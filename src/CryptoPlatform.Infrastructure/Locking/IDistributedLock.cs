namespace CryptoPlatform.Infrastructure.Locking;

/// <summary>
/// 分布式锁抽象。
/// 用于密钥轮换、销毁等需要跨实例互斥的操作。
/// </summary>
public interface IDistributedLock
{
    /// <summary>
    /// 尝试获取分布式锁。
    /// </summary>
    /// <param name="key">锁的唯一标识（如 lock:key-rotate:{keyId}）</param>
    /// <param name="expiry">锁的自动过期时间（防止死锁）</param>
    /// <param name="waitTimeout">等待获取锁的超时时间。TimeSpan.Zero 表示不等待。</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>锁句柄。如果获取失败返回 null。</returns>
    Task<IAsyncDisposable?> TryAcquireAsync(string key, TimeSpan expiry, TimeSpan waitTimeout, CancellationToken ct = default);
}

/// <summary>
/// 分布式锁句柄。释放时自动解锁。
/// </summary>
public interface ILockHandle : IAsyncDisposable
{
    /// <summary>锁的唯一标识</summary>
    string Key { get; }

    /// <summary>锁的持有者标识（用于安全释放）</summary>
    string Owner { get; }
}
