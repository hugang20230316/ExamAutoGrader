namespace ExamAutoGrader.Domain.Events;

/// <summary>
/// 空接口标记事件处理器
/// 模仿ABP的IEventHandler
/// </summary>
public interface IEventHandler
{
}

/// <summary>
/// 泛型事件处理器
/// 模仿ABP的IEventHandler<TEventData>
/// </summary>
public interface IEventHandler<in TEventData> : IEventHandler
    where TEventData : IEventData
{
    Task HandleEventAsync(TEventData eventData);
}

/// <summary>
/// 本地事件处理器
/// 模仿ABP的ILocalEventHandler<TEventData>
/// </summary>
public interface ILocalEventHandler<in TEventData> : IEventHandler<TEventData>
    where TEventData : IEventData
{
}

/// <summary>
/// 事件处理器包装器基类
/// 模仿ABP的EventHandlerWrapperBase
/// </summary>
public abstract class EventHandlerWrapperBase
{
    public abstract Task HandleAsync(IEventData eventData);
}

/// <summary>
/// 事件处理器包装器
/// 模仿ABP的EventHandlerWrapperImpl<TEventData>
/// </summary>
public class EventHandlerWrapper<TEventData> : EventHandlerWrapperBase
    where TEventData : IEventData
{
    private readonly IEventHandler<TEventData> _eventHandler;

    public EventHandlerWrapper(IEventHandler<TEventData> eventHandler)
    {
        _eventHandler = eventHandler;
    }

    public override Task HandleAsync(IEventData eventData)
    {
        return _eventHandler.HandleEventAsync((TEventData)eventData);
    }
}