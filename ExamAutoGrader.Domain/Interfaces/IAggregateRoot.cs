
namespace ExamAutoGrader.Domain.Interfaces;

/// 聚合根接口
/// 模仿ABP的IAggregateRoot
/// </summary>
public interface IAggregateRoot
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }
    void AddDomainEvent(IDomainEvent eventItem);
    void RemoveDomainEvent(IDomainEvent eventItem);
    void ClearDomainEvents();
}

public interface IAggregateRoot<TKey> : IAggregateRoot
{
    TKey Id { get; }
}

public interface IAuditable
{
    DateTime CreatedAt { get; set; }
    DateTime UpdatedAt { get; set; }
}

// 实体接口
public interface IEntity<TKey>
{
    TKey Id { get; }
}

// 基础实体
public abstract class Entity<TKey> : IEntity<TKey>, IAuditable
{
    public TKey Id { get; protected set; }

    protected Entity(TKey id)
    {
        Id = id;
    }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 最后修改时间
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 软删除标记
    /// </summary>
    public bool IsDeleted { get; private set; }

    // 相等性比较
    public override bool Equals(object obj)
    {
        if (obj is not Entity<TKey> other)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        if (GetType() != other.GetType())
            return false;

        return Id.Equals(other.Id);
    }

    public override int GetHashCode()
    {
        return (GetType().ToString() + Id).GetHashCode();
    }
}
