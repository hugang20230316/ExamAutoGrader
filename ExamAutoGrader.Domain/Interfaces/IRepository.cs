namespace ExamAutoGrader.Domain.Interfaces;

/// <summary>
/// 通用仓储接口（模拟 ABP IRepository）
/// </summary>
/// <typeparam name="TEntity">实体类型</typeparam>
public interface IRepository<TAggregate, TKey> where TAggregate : IAggregateRoot<TKey>
{
    /// <summary>
    /// 根据ID获取实体
    /// </summary>
    Task<TAggregate?> GetByIdAsync(TKey id, CancellationToken ct);

    /// <summary>
    /// 根据ID列表获取实体
    /// </summary>
    Task<IEnumerable<TAggregate>> GetByIdsAsync(IEnumerable<TKey> ids, CancellationToken ct = default);

    /// <summary>
    /// 插入实体
    /// </summary>
    Task AddAsync(TAggregate entity, CancellationToken ct = default);

    /// <summary>
    /// 更新实体
    /// </summary>
    void Update(TAggregate entity, CancellationToken ct = default);

    /// <summary>
    /// 删除实体
    /// </summary>
    void Delete(TAggregate entity, CancellationToken ct = default);
}
