using ExamAutoGrader.Domain.Events;

namespace ExamAutoGrader.Domain.Interfaces;

public interface IDomainEvent : IEventData
{
    Guid EventId { get; }
}

/// <summary>
/// 领域事件基类
/// </summary>
public abstract class DomainEvent : EventData, IDomainEvent
{
    public Guid EventId { get; }

    protected DomainEvent()
    {
        EventId = Guid.NewGuid();
        EventTime = DateTime.UtcNow;
    }

    protected DomainEvent(object eventSource) : this()
    {
        EventSource = eventSource;
    }
}