using ExamAutoGrader.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ExamAutoGrader.Infrastructure.ExtenalServices;

public class StartupService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<StartupService> _logger;

    public StartupService(IServiceProvider serviceProvider, ILogger<StartupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("=== StartupService 开始执行 ===");

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ExamAutoGraderDbContext>();

            _logger.LogInformation("获取DbContext成功，开始检查数据库...");

            // 方法1：先检查数据库连接
            var canConnect = await context.Database.CanConnectAsync();
            _logger.LogInformation("数据库连接状态: {CanConnect}", canConnect);

            // 方法2：检查是否有待应用的迁移
            var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
            _logger.LogInformation("待应用的迁移: {Count}", pendingMigrations.Count());

            if (pendingMigrations.Any())
            {
                _logger.LogInformation("待应用迁移: {Migrations}", string.Join(", ", pendingMigrations));
            }

            // 方法3：应用迁移
            _logger.LogInformation("开始应用数据库迁移...");
            await context.Database.MigrateAsync();
            _logger.LogInformation("数据库迁移完成");

            // 方法4：验证表是否存在
            try
            {
                var tableExists = await context.FeedbackRecords.AnyAsync();
                _logger.LogInformation("ai_feedback_record 表存在: {Exists}", true);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("ai_feedback_record 表不存在: {Message}", ex.Message);

                // 方法5：如果迁移失败，尝试直接创建表
                _logger.LogInformation("尝试直接创建表...");
                var created = await context.Database.EnsureCreatedAsync();
                _logger.LogInformation("直接创建表结果: {Created}", created);
            }

            _logger.LogInformation("=== StartupService 执行完成 ===");
        }
        catch (Exception ex)
        {
        _logger.LogError(ex, "=== StartupService 执行失败 ===");
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("StartupService 停止");
        return Task.CompletedTask;
    }
}