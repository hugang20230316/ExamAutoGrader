using ExamAutoGrader.Domain.Entities;
using ExamAutoGrader.Domain.Interfaces;
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

    public DbSet<GradingRecord> GradingRecords { get; set; }

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

        // 配置LearningRecord实体
        modelBuilder.Entity<FeedbackRecord>(entity =>
        {
            entity.ToTable("ai_feedback_record");

            entity.HasKey(e => e.Id);

            // 主键：记录唯一标识（GUID 字符串）
            entity.Property(e => e.Id)
                  .HasMaxLength(36)
                  .HasComment("反馈记录唯一ID，全局唯一标识符（GUID）");

            // 关联题目ID
            entity.Property(e => e.QuestionId)
                  .HasMaxLength(36)
                  .HasComment("关联的题目唯一ID，可为空");

            // 学科名称
            entity.Property(e => e.Subject)
                  .HasMaxLength(50)
                  .HasComment("学科名称，如：数学、物理、语文等");

            // 题目类型（枚举）
            entity.Property(e => e.QuestionType)
                  .HasConversion<int?>()
                  .HasComment("题目类型：1=选择题，2=填空题，3=解答题，4=判断题等");

            // 题干内容
            entity.Property(e => e.Stem)
                  .HasColumnType("longtext")
                  .HasComment("题目题干内容，支持长文本");

            // 学生作答
            entity.Property(e => e.StudentAnswer)
                  .HasColumnType("longtext")
                  .HasComment("学生提交的答案内容");

            // 题目总分
            entity.Property(e => e.Score).HasComment("题目总分");

            // 期望得分（用于训练的目标分数）
            entity.Property(e => e.ExpectedScore)
                  .HasComment("期望得分（教师评分或历史标准分），用于AI模型训练的目标值");

            // AI反馈理由
            entity.Property(e => e.FeedbackReason)
                  .HasColumnType("longtext")
                  .HasComment("AI生成的评分理由或反馈评语");

            // 标准答案
            entity.Property(e => e.StandardAnswer)
                  .HasComment("题目标准参考答案");

            // 原始得分（可能来自其他系统）
            entity.Property(e => e.OriginalScore)
                  .HasComment("原始得分（如人工初评分数），可用于对比分析");

            // 语义指纹（用于相似题匹配）
            entity.Property(e => e.SemanticFingerprint)
                  .HasComment("语义指纹，用于题目去重或相似题聚类（如哈希值或特征编码）");

            // 创建时间
            entity.Property(e => e.CreatedAt)
                  .HasComment("记录创建时间");

            // 更新时间
            entity.Property(e => e.UpdatedAt)
                  .HasComment("记录最后更新时间");

            // 索引：提升按学科查询性能
            entity.HasIndex(e => e.Subject)
                  .HasDatabaseName("idx_subject");

            // 索引：提升按题型查询性能
            entity.HasIndex(e => e.QuestionType)
                  .HasDatabaseName("idx_question_type");

        });

        //生成GradingRecord实体的配置
        modelBuilder.Entity<GradingRecord>(entity =>
        {
            entity.ToTable("ai_grading_record");

            entity.HasKey(e => e.Id);

            // 主键：记录唯一标识（GUID 字符串）
            entity.Property(e => e.Id)
                  .HasMaxLength(36)
                  .HasComment("评分记录唯一ID，全局唯一标识符（GUID）");

            // 关联题目ID
            entity.Property(e => e.QuestionId)
                  .HasMaxLength(36)
                  .HasComment("关联的题目唯一ID，可为空");

            // 学科名称
            entity.Property(e => e.Subject)
                  .HasMaxLength(50)
                  .HasComment("学科名称，如：数学、物理、语文等");

            // 题目类型（枚举）
            entity.Property(e => e.QuestionType)
                  .HasConversion<int?>()
                  .HasComment("题目类型：1=选择题，2=填空题，3=解答题，4=判断题等");

            // 题干内容
            entity.Property(e => e.Stem)
                  .HasColumnType("longtext")
                  .HasComment("题目题干内容，支持长文本");

            // 学生作答
            entity.Property(e => e.StudentAnswer)
                  .HasColumnType("longtext")
                  .HasComment("学生提交的答案内容");

            // 标准答案
            entity.Property(e => e.StandardAnswer)
                  .HasColumnType("longtext")
                  .HasComment("题目标准参考答案");

            // 原始得分（AI 给分或外部来源）
            entity.Property(e => e.OriginalScore)
                  .HasComment("AI 给分或外部系统提供的原始得分");

            // 评分原因/评语（AI 生成或人工补充）
            entity.Property(e => e.GradingReason)
                  .HasColumnType("longtext")
                  .HasComment("评分理由或评语");

            // 索引：按学科查询
            entity.HasIndex(e => e.Subject)
                  .HasDatabaseName("idx_grading_subject");

            // 索引：按题型查询
            entity.HasIndex(e => e.QuestionType)
                  .HasDatabaseName("idx_grading_question_type");

            // 可根据需要添加更多约束/默认值，例如创建时间、评分来源等（仅当实体包含这些属性时）
        });

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