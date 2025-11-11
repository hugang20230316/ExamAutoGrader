using ExamAutoGrader.Domain.Events;
using ExamAutoGrader.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ExamAutoGrader.Infrastructure.Persistence;

public class UnitOfWork(ExamAutoGraderDbContext dbContext, ILogger<UnitOfWorkManager> logger) : IUnitOfWork
{
    private readonly IEventBus _eventBus;
    private readonly ExamAutoGraderDbContext _context = dbContext;
    private readonly ILogger<UnitOfWorkManager> _logger = logger;
    private bool _disposed = false;

    public async Task CompleteAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(UnitOfWorkManager));
        _logger.LogDebug("【UnitOfWork】正在 SaveChangesAsync，DbContext Hash: {hash}", _context.GetHashCode());
        await _context.SaveChangesAsync();

        // ✅ 发布领域事件
        await PublishDomainEventsAsync(cancellationToken);
    }

    public Task RollbackAsync()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(UnitOfWork));

        var transaction = _context.Database.CurrentTransaction;
        if (transaction != null)
        {
            _logger.LogDebug("【UnitOfWork】回滚事务");
            return transaction.RollbackAsync();
        }
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _context.Database.CurrentTransaction?.Dispose();
            _disposed = true;
        }
    }

    public async Task DisposeAsync()
    {
        if (!_disposed)
        {
            if (_context.Database.CurrentTransaction != null)
            {
                await _context.Database.CurrentTransaction.DisposeAsync().ConfigureAwait(false);
            }
            _disposed = true;
        }
    }

    public object GetDbContext() => _context;

    private async Task PublishDomainEventsAsync(CancellationToken cancellationToken)
    {
        if (_context == null) return;

        // 获取所有有领域事件的实体
        var domainEntities = _context.ChangeTracker.Entries<IAggregateRoot>()
            .Where(x => x.Entity.DomainEvents != null && x.Entity.DomainEvents.Any())
            .ToList();

        var domainEvents = domainEntities.SelectMany(x => x.Entity.DomainEvents).ToList();

        // 清空领域事件，避免重复发布
        domainEntities.ForEach(entity => entity.Entity.ClearDomainEvents());

        // 发布所有领域事件
        foreach (var domainEvent in domainEvents)
        {
            _logger.LogDebug("发布领域事件: {EventType}", domainEvent.GetType().Name);
            await _eventBus.PublishAsync(domainEvent, cancellationToken);
        }
    }
}
