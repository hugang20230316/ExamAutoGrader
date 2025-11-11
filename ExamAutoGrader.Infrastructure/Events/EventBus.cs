using ExamAutoGrader.Domain.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace ExamAutoGrader.Infrastructure.Events;

/// <summary>
/// 事件总线实现
/// 模仿ABP的EventBus
/// </summary>
public class EventBus : IEventBus
{
    private readonly ILogger<EventBus> _logger;
    protected readonly IServiceProvider _serviceProvider;

    // 事件类型与处理器类型的映射
    private readonly ConcurrentDictionary<Type, List<Type>> _eventHandlers;

    public EventBus(
        ILogger<EventBus> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _eventHandlers = new ConcurrentDictionary<Type, List<Type>>();
    }

    public virtual async Task PublishAsync<TEventData>(TEventData eventData, CancellationToken cancellationToken)
        where TEventData : IEventData
    {
        await PublishAsync(typeof(TEventData), eventData, cancellationToken);
    }

    public virtual async Task PublishAsync(Type eventType, IEventData eventData, CancellationToken cancellationToken)
    {
        if (eventData == null)
            throw new ArgumentNullException(nameof(eventData));

        var handlers = GetHandlers(eventType);

        _logger.LogDebug("发布事件: {EventType} - {EventTime}", eventType.Name, eventData.EventTime);

        foreach (var handler in handlers)
        {
            await TriggerHandlerAsync(eventType, handler, eventData, cancellationToken);
        }
    }

    public virtual IDisposable Register<TEventData>(IEventHandler<TEventData> handler, CancellationToken cancellationToken)
        where TEventData : IEventData
    {
        var eventType = typeof(TEventData);
        var handlerType = handler.GetType();

        _eventHandlers.AddOrUpdate(
            eventType,
            new List<Type> { handlerType },
            (_, existingHandlers) =>
            {
                if (!existingHandlers.Contains(handlerType))
                {
                    existingHandlers.Add(handlerType);
                }
                return existingHandlers;
            });

        _logger.LogDebug("注册事件处理器: {EventType} -> {HandlerType}", eventType.Name, handlerType.Name);

        return new EventHandlerDisposable(() => Unregister<TEventData>(handler));
    }

    protected virtual void Unregister<TEventData>(IEventHandler<TEventData> handler)
        where TEventData : IEventData
    {
        var eventType = typeof(TEventData);
        var handlerType = handler.GetType();

        if (_eventHandlers.TryGetValue(eventType, out var handlers))
        {
            handlers.Remove(handlerType);
            if (!handlers.Any())
            {
                _eventHandlers.TryRemove(eventType, out _);
            }
        }

        _logger.LogDebug("取消注册事件处理器: {EventType} -> {HandlerType}", eventType.Name, handlerType.Name);
    }

    protected virtual List<Type> GetHandlers(Type eventType)
    {
        if (_eventHandlers.TryGetValue(eventType, out var handlers))
        {
            return handlers.ToList();
        }

        return new List<Type>();
    }

    protected virtual async Task TriggerHandlerAsync(Type eventType, Type handlerType, IEventData eventData, CancellationToken cancellationToken)
    {
        using (var scope = _serviceProvider.CreateScope())
        {
            var handler = scope.ServiceProvider.GetService(handlerType) as IEventHandler;
            if (handler == null)
            {
                _logger.LogWarning("无法解析事件处理器: {HandlerType}", handlerType.Name);
                return;
            }

            try
            {
                _logger.LogDebug("执行事件处理器: {HandlerType}", handlerType.Name);

                // 通过反射调用HandleEventAsync方法
                var handleMethod = handlerType.GetMethod("HandleEventAsync");
                if (handleMethod != null)
                {
                    var task = (Task)handleMethod.Invoke(handler, new object[] { eventData });
                    if (task != null)
                    {
                        await task;
                    }
                }

                _logger.LogDebug("事件处理器执行完成: {HandlerType}", handlerType.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "事件处理器执行失败: {HandlerType}", handlerType.Name);
                throw new EventHandleException($"处理事件 {eventType.Name} 时发生错误", ex);
            }
        }
    }

    private class EventHandlerDisposable : IDisposable
    {
        private readonly Action _disposeAction;

        public EventHandlerDisposable(Action disposeAction)
        {
            _disposeAction = disposeAction;
        }

        public void Dispose()
        {
            _disposeAction();
        }
    }
}

/// <summary>
/// 事件处理异常
/// 模仿ABP的EventHandleException
/// </summary>
public class EventHandleException : Exception
{
    public EventHandleException(string message) : base(message)
    {
    }

    public EventHandleException(string message, Exception innerException) : base(message, innerException)
    {
    }
}