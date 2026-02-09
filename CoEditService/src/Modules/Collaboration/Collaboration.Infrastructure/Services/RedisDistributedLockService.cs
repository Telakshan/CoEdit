using Collaboration.Application.Services;
using StackExchange.Redis;

namespace Collaboration.Infrastructure.Services;

public class RedisDistributedLockService(IConnectionMultiplexer redis): IDistributedLockService
{
    public async Task<IAsyncDisposable?> AcquireLockAsync(string key, TimeSpan expiry)
    {
        var db = redis.GetDatabase();
        var token = Guid.NewGuid().ToString();
        var lockKey = $"lock:{key}";

        if (await db.LockTakeAsync(lockKey, token, expiry))
        {
            return new RedisLockRelease(db, lockKey, token);
        }

        return null;
    }
    
    private class RedisLockRelease(IDatabase db, string key, string token) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            await db.LockReleaseAsync(key, token);
        }
    }
}