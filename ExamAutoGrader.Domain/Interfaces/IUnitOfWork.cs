namespace ExamAutoGrader.Domain.Interfaces;

/// <summary>
/// 工作单元接口（由领域定义）
/// 所有数据库操作最终通过它完成提交
/// </summary>
public interface IUnitOfWork : IDisposable
{
    Task CompleteAsync(CancellationToken cancellationToken = default);
    Task DisposeAsync();
    Task RollbackAsync(CancellationToken cancellationToken = default);

    object GetDbContext();
}


public interface IUnitOfWorkManager
{
    IUnitOfWork Begin();

    ValueTask DisposeAsync();
}