namespace ExamAutoGrader.Domain.Events;

/// <summary>
/// 事件数据基类
/// 模仿ABP的EventData
/// </summary>
[Serializable]
public abstract class EventData : IEventData
{
    public DateTime EventTime { get; set; }
    public object EventSource { get; set; }

    protected EventData()
    {
        EventTime = DateTime.Now;
    }
}

/// <summary>
/// 事件数据接口
/// 模仿ABP的IEventData
/// </summary>
public interface IEventData
{
    DateTime EventTime { get; set; }
    object EventSource { get; set; }
}