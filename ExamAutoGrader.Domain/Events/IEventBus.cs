using System;

namespace ExamAutoGrader.Domain.Events;

/// <summary>
/// 事件总线接口
/// 模仿ABP的IEventBus
/// </summary>
public interface IEventBus
{
    /// <summary>
    /// 发布事件到所有注册的处理器
    /// </summary>
    Task PublishAsync<TEventData>(TEventData eventData, CancellationToken cancellationToken = default)
        where TEventData : IEventData;

    /// <summary>
    /// 发布事件到指定类型处理器
    /// </summary>
    Task PublishAsync(Type eventType, IEventData eventData, CancellationToken cancellationToken = default);

    /// <summary>
    /// 注册事件处理器
    /// </summary>
    IDisposable Register<TEventData>(IEventHandler<TEventData> handler, CancellationToken cancellationToken = default)
        where TEventData : IEventData;
}