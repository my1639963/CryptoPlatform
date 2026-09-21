using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace CryptoPlatform.Infrastructure.Locking;

/// <summary>
/// 基于内存信号量的分布式锁实现（开发/测试环境用）。
/// 仅在单实例内有效，不支持多实例互斥。
/// </summary>
public sealed class InMemoryDistributedLock : IDistributedLock
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();
    private readonly ILogger<InMemoryDistributedLock> _logger;

    public InMemoryDistributedLock(ILogger<InMemoryDistributedLock> logger)
    {
        _logger = logger;
    }

    public async Task<IAsyncDisposable?> TryAcquireAsync(string key, TimeSpan expiry, TimeSpan waitTimeout, CancellationToken ct = default)
    {
        var semaphore = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        var owner = Guid.NewGuid().ToString("N");

        var acquired = waitTimeout == TimeSpan.Zero
            ? semaphore.Wait(0)
            : await semaphore.WaitAsync(waitTimeout, ct);

        if (!acquired)
        {
            _logger.LogDebug("内存锁获取超时: {Key}", key);
            return null;
        }

        _logger.LogDebug("内存锁获取成功: {Key}, Owner: {Owner}", key, owner);
        return new InMemoryLockHandle(key, owner, semaphore, _logger);
    }

    private sealed class InMemoryLockHandle : ILockHandle
    {
        private readonly SemaphoreSlim _semaphore;
        private readonly ILogger _logger;
        private int _disposed;

        public string Key { get; }
        public string Owner { get; }

        public InMemoryLockHandle(string key, string owner, SemaphoreSlim semaphore, ILogger logger)
        {
            Key = key;
            Owner = owner;
            _semaphore = semaphore;
            _logger = logger;
        }

        public ValueTask DisposeAsync()
        {
            if (Interlocked.CompareExchange(ref _disposed, 1, 0) != 0)
                return ValueTask.CompletedTask;

            _logger.LogDebug("内存锁释放: {Key}", Key);
            _semaphore.Release();
            return ValueTask.CompletedTask;
        }
    }
}
