using ExamAutoGrader.Domain.Entities;
using ExamAutoGrader.Domain.Interfaces;
using ExamAutoGrader.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MySqlConnector;

namespace ExamAutoGrader.Infrastructure.Persistence;

public class ExamAutoGraderDbContext : DbContext
{
    /// <summary>
    /// 实例唯一标识（用于验证上下文一致性）
    /// </summary>
    public Guid InstanceId { get; } = Guid.NewGuid();

    public DbSet<FeedbackRecord> FeedbackRecords { get; set; }

    private readonly ILogger<ExamAutoGraderDbContext> _logger;

    public ExamAutoGraderDbContext(DbContextOptions<ExamAutoGraderDbContext> options, ILogger<ExamAutoGraderDbContext> logger)
        : base(options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        try
        {
            // 检查数据库是否可以连接
            Database.EnsureCreated();
            _logger.LogInformation("数据库连接成功，确保已创建");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "数据库连接失败");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 配置FeedbackRecord实体
        modelBuilder.Entity<FeedbackRecord>(entity =>
        {
            entity.ToTable("ai_feedback_record");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasMaxLength(36);

            entity.Property(e => e.QuestionId).HasMaxLength(36);

            entity.Property(e => e.Subject).HasMaxLength(50);

            entity.Property(e => e.QuestionType).HasConversion<int?>();

            entity.Property(e => e.Stem).HasColumnType("longtext");

            entity.Property(e => e.StudentAnswer).HasColumnType("longtext");

            entity.Property(e => e.ExpectedScore);

            entity.Property(e => e.FeedbackReason).HasColumnType("longtext");

            entity.Property(e => e.StandardAnswer);

            entity.Property(e => e.SemanticFingerprint);

            // 为常用查询字段添加索引
            entity.HasIndex(e => e.Subject);
            entity.HasIndex(e => e.QuestionType);

            entity.Property(e => e.CreatedAt);

            entity.Property(e => e.UpdatedAt);
        });

        modelBuilder.Entity<QuestionFingerprint>().HasKey(q => new { q.Stem, q.Subject });

        _logger.LogInformation("数据库模型配置完成");
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var contextId = GetHashCode(); // 每个实例唯一
        _logger.LogDebug("EF Core SaveChangesAsync 被调用，DbContext HashCode: {ContextId}", contextId);

        foreach (var entry in ChangeTracker.Entries())
        {
            _logger.LogDebug("实体: {EntityType}, 状态: {State}, 来自上下文: {ContextId}",entry.Entity.GetType().Name, entry.State, contextId);
        }

        try
        {
            await ProcessDomainEventsAsync();
            var result = await base.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("保存成功，影响 {Count} 条记录，上下文: {ContextId}", result, contextId);
            return result;
        }
        catch (DbUpdateException ex)
        {
            LogDatabaseException(ex, "数据库更新失败");
            throw new ApplicationException("保存数据时发生数据库错误", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存数据库更改时发生未知异常");
            throw new ApplicationException("保存数据时发生未知错误", ex);
        }
    }

    public override int SaveChanges()
    {
        _logger.LogDebug("EF Core SaveChanges被调用");

        try
        {
            // 在保存前处理领域事件（如果需要的话）
            ProcessDomainEventsAsync().GetAwaiter().GetResult();

            var result = base.SaveChanges();

            _logger.LogInformation("数据库更改保存成功，影响 {Count} 条记录", result);
            return result;
        }
        catch (DbUpdateException ex)
        {
            LogDatabaseException(ex, "数据库更新失败");
            throw new ApplicationException("保存数据时发生数据库错误", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存数据库更改时发生未知异常");
            throw new ApplicationException("保存数据时发生未知错误", ex);
        }
    }

    private async Task ProcessDomainEventsAsync()
    {
        var domainEntities = ChangeTracker.Entries<IAggregateRoot<Guid>>()
            .Where(x => x.Entity.DomainEvents.Any())
            .ToList();

        var domainEvents = domainEntities
            .SelectMany(x => x.Entity.DomainEvents)
            .ToList();

        // 清空领域事件，避免重复发布
        domainEntities.ForEach(entity => entity.Entity.ClearDomainEvents());

        // 记录领域事件（实际项目中这里会发布到事件总线）
        foreach (var domainEvent in domainEvents)
        {
            _logger.LogInformation("发现领域事件：{EventType}，发生时间：{OccurredOn}",
                domainEvent.GetType().Name, domainEvent.EventTime);
        }

        await Task.CompletedTask;
    }

    private void LogDatabaseException(DbUpdateException ex, string message)
    {
        // 输出完整异常信息
        Console.WriteLine($"完整异常: {ex}");
        Console.WriteLine($"内部异常: {ex.InnerException}");
        Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");

        // 记录到日志
        _logger.LogError(ex, message);

        // 如果是 MySQL 异常，获取详细信息
        if (ex.InnerException is MySqlException mysqlEx)
        {
            Console.WriteLine($"MySQL 错误代码: {mysqlEx.ErrorCode}");
            Console.WriteLine($"MySQL 错误号: {mysqlEx.Number}");
            Console.WriteLine($"MySQL 错误消息: {mysqlEx.Message}");
        }
    }
}