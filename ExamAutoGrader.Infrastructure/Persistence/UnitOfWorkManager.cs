using ExamAutoGrader.Domain.Interfaces;

namespace ExamAutoGrader.Infrastructure.Persistence;

public class UnitOfWorkManager : IUnitOfWorkManager, IDisposable
{
    private IUnitOfWork? _unitOfWork;
    private bool _disposed = false;

    public UnitOfWorkManager(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public IUnitOfWork Begin()
    {
        return _unitOfWork;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _unitOfWork?.Dispose();
            _disposed = true;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            if (_unitOfWork is not null)
            {
                await _unitOfWork.DisposeAsync().ConfigureAwait(false);
            }
            _disposed = true;
        }
    }
}