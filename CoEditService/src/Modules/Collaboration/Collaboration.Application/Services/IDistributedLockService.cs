namespace Collaboration.Application.Services;

public interface IDistributedLockService
{
    Task<IAsyncDisposable?> AcquireLockAsync(string key, TimeSpan expiry);
}