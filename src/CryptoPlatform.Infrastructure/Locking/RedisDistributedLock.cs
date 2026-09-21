using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace CryptoPlatform.Infrastructure.Locking;

/// <summary>
/// 基于 Redis 的分布式锁实现。
/// 使用 SET NX EX 获取锁，Lua 脚本安全释放锁（仅持有者可释放）。
/// </summary>
public sealed class RedisDistributedLock : IDistributedLock
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisDistributedLock> _logger;

    // Lua 脚本：仅当锁的值匹配 owner 时才删除（安全释放）
    private const string ReleaseLuaScript = @"
        if redis.call('get', KEYS[1]) == ARGV[1] then
            return redis.call('del', KEYS[1])
        else
            return 0
        end";

    public RedisDistributedLock(IConnectionMultiplexer redis, ILogger<RedisDistributedLock> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task<IAsyncDisposable?> TryAcquireAsync(string key, TimeSpan expiry, TimeSpan waitTimeout, CancellationToken ct = default)
    {
        var owner = Guid.NewGuid().ToString("N");
        var db = _redis.GetDatabase();

        var deadline = DateTime.UtcNow + waitTimeout;

        do
        {
            ct.ThrowIfCancellationRequested();

            // SET key owner NX EX expirySeconds
            var acquired = await db.StringSetAsync(
                key, owner, expiry, When.NotExists, CommandFlags.None);

            if (acquired)
            {
                _logger.LogDebug("分布式锁获取成功: {Key}, Owner: {Owner}", key, owner);
                return new RedisLockHandle(key, owner, _redis, _logger);
            }

            // 未获取到锁，等待后重试
            if (waitTimeout > TimeSpan.Zero)
            {
                var delay = TimeSpan.FromMilliseconds(
                    Math.Min(50, (deadline - DateTime.UtcNow).TotalMilliseconds));
                if (delay > TimeSpan.Zero)
                    await Task.Delay(delay, ct);
            }
        }
        while (DateTime.UtcNow < deadline);

        _logger.LogDebug("分布式锁获取超时: {Key}", key);
        return null;
    }

    /// <summary>
    /// Redis 分布式锁句柄。
    /// 释放时通过 Lua 脚本校验 owner 身份，确保只有持有者能释放锁。
    /// </summary>
    private sealed class RedisLockHandle : ILockHandle
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly ILogger _logger;
        private int _disposed;

        public string Key { get; }
        public string Owner { get; }

        public RedisLockHandle(string key, string owner, IConnectionMultiplexer redis, ILogger logger)
        {
            Key = key;
            Owner = owner;
            _redis = redis;
            _logger = logger;
        }

        public async ValueTask DisposeAsync()
        {
            if (Interlocked.CompareExchange(ref _disposed, 1, 0) != 0)
                return;

            try
            {
                var db = _redis.GetDatabase();
                var result = await db.ScriptEvaluateAsync(
                    ReleaseLuaScript,
                    [(RedisKey)Key],
                    [(RedisValue)Owner]);

                if ((long)result == 1)
                    _logger.LogDebug("分布式锁释放成功: {Key}", Key);
                else
                    _logger.LogWarning("分布式锁释放失败（锁已过期或被他人持有）: {Key}", Key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分布式锁释放异常: {Key}", Key);
            }
        }
    }
}
