using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Distributed;

namespace CryptoPlatform.Infrastructure.Caching;

/// <summary>
/// 基于 IDistributedCache 的缓存服务实现。
/// 适用于内存缓存（开发）和 Redis 缓存（生产）两种场景。
/// </summary>
public sealed class DistributedCacheService : ICacheService
{
    private readonly IDistributedCache _cache;

    // 用于 SetIfNotExistsAsync 的本地并发控制（单实例内有效）
    // 分布式环境下，Redis 实现应使用 Lua 脚本保证原子性
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> _keyLocks = new();

    public DistributedCacheService(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task<string?> GetStringAsync(string key, CancellationToken ct = default)
    {
        return await _cache.GetStringAsync(key, ct);
    }

    public async Task SetStringAsync(string key, string value, TimeSpan expiration, CancellationToken ct = default)
    {
        await _cache.SetStringAsync(key, value, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration
        }, ct);
    }

    public async Task SetStringAbsoluteAsync(string key, string value, DateTimeOffset absoluteExpiration, CancellationToken ct = default)
    {
        await _cache.SetStringAsync(key, value, new DistributedCacheEntryOptions
        {
            AbsoluteExpiration = absoluteExpiration
        }, ct);
    }

    public async Task SetStringSlidingAsync(string key, string value, TimeSpan slidingExpiration, CancellationToken ct = default)
    {
        await _cache.SetStringAsync(key, value, new DistributedCacheEntryOptions
        {
            SlidingExpiration = slidingExpiration
        }, ct);
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        await _cache.RemoveAsync(key, ct);
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken ct = default)
    {
        var value = await _cache.GetStringAsync(key, ct);
        return value is not null;
    }

    /// <summary>
    /// 基于本地信号量 + Get-Then-Set 的近似原子操作。
    /// 注意：在分布式（Redis）环境下，此实现不能保证跨实例原子性。
    /// 真正的分布式原子操作应使用 RedisDistributedLock 中的 Lua 脚本。
    /// </summary>
    public async Task<bool> SetIfNotExistsAsync(string key, string value, TimeSpan expiration, CancellationToken ct = default)
    {
        var keyLock = _keyLocks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await keyLock.WaitAsync(ct);
        try
        {
            var existing = await _cache.GetStringAsync(key, ct);
            if (existing is not null)
                return false;

            await _cache.SetStringAsync(key, value, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration
            }, ct);
            return true;
        }
        finally
        {
            keyLock.Release();
            _keyLocks.TryRemove(key, out _);
        }
    }
}
