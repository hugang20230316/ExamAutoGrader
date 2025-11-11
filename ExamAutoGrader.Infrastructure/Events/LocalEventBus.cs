using ExamAutoGrader.Domain.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ExamAutoGrader.Infrastructure.Events;

/// <summary>
/// 本地事件总线
/// 模仿ABP的LocalEventBus
/// </summary>
public class LocalEventBus : EventBus
{
    public LocalEventBus(
        ILogger<LocalEventBus> logger,
        IServiceProvider serviceProvider)
        : base(logger, serviceProvider)
    {
    }

    protected override List<Type> GetHandlers(Type eventType)
    {
        var handlers = base.GetHandlers(eventType);

        // 如果没有显式注册的处理器，尝试从DI容器中获取所有实现ILocalEventHandler<>的处理器
        if (!handlers.Any())
        {
            var handlerType = typeof(ILocalEventHandler<>).MakeGenericType(eventType);
            var registeredHandlers = _serviceProvider.GetServices(handlerType);

            foreach (var handler in registeredHandlers)
            {
                handlers.Add(handler.GetType());
            }
        }

        return handlers;
    }
}