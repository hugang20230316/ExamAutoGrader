using ExamAutoGrader.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ExamAutoGrader.Infrastructure.Persistence.Repositories;

/// <summary>
/// 基于 EF Core 的泛型仓储实现
/// 支持聚合根、规约模式和领域事件
/// </summary>
/// <typeparam name="TAggregate">聚合根类型</typeparam>
/// <typeparam name="TKey">主键类型</typeparam>
public class EfCoreRepository<TAggregate, TKey> : IRepository<TAggregate, TKey> where TAggregate : class, IAggregateRoot<TKey>
{
    protected readonly ExamAutoGraderDbContext Context;
    protected readonly ILogger Logger;
    protected readonly DbSet<TAggregate> DbSet;

    public EfCoreRepository(
        ExamAutoGraderDbContext context,
        ILogger<EfCoreRepository<TAggregate, TKey>> logger)
    {
        Context = context;
        Logger = logger;
        DbSet = context.Set<TAggregate>();
    }

    public virtual IQueryable<TAggregate> Query() => DbSet;

    public virtual async Task<TAggregate?> GetByIdAsync(TKey id, CancellationToken ct = default)
    {
        var entity = await DbSet.FindAsync(new object[] { id! }, ct);
        if (entity != null)
        {
            Logger.LogDebug("获取聚合根 {Type} ID: {Id}", typeof(TAggregate).Name, id);
        }
        return entity;
    }

    public virtual async Task<IEnumerable<TAggregate>> GetByIdsAsync(IEnumerable<TKey> ids, CancellationToken ct = default)
    {
        var idList = ids.ToList();
        var entities = await DbSet.Where(e => idList.Contains(e.Id)).ToListAsync(ct);
        Logger.LogDebug("批量获取聚合根 {Type} 数量: {Count}", typeof(TAggregate).Name, entities.Count);
        return entities;
    }

    public virtual async Task AddAsync(TAggregate aggregate, CancellationToken ct = default)
    {
        await DbSet.AddAsync(aggregate, ct);
        Logger.LogDebug("添加聚合根 {Type} ID: {Id}", typeof(TAggregate).Name, aggregate.Id);
    }

    public virtual void Update(TAggregate aggregate, CancellationToken ct = default)
    {
        DbSet.Update(aggregate);
        Logger.LogDebug("更新聚合根 {Type} ID: {Id}", typeof(TAggregate).Name, aggregate.Id);
    }

    public virtual void Delete(TAggregate aggregate, CancellationToken ct = default)
    {
        DbSet.Remove(aggregate);
        Logger.LogDebug("删除聚合根 {Type} ID: {Id}", typeof(TAggregate).Name, aggregate.Id);
    }
}