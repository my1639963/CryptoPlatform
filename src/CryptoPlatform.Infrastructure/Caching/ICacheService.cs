namespace CryptoPlatform.Infrastructure.Caching;

/// <summary>
/// 缓存服务抽象接口。
/// 封装分布式缓存的通用操作，使调用方不直接依赖 IDistributedCache。
/// 底层实现通过 DI 注册决定（内存缓存 / Redis）。
/// </summary>
public interface ICacheService
{
    /// <summary>获取字符串值。不存在返回 null。</summary>
    Task<string?> GetStringAsync(string key, CancellationToken ct = default);

    /// <summary>设置字符串值（相对过期时间）。</summary>
    Task SetStringAsync(string key, string value, TimeSpan expiration, CancellationToken ct = default);

    /// <summary>设置字符串值（绝对过期时间）。</summary>
    Task SetStringAbsoluteAsync(string key, string value, DateTimeOffset absoluteExpiration, CancellationToken ct = default);

    /// <summary>设置字符串值（滑动过期时间）。</summary>
    Task SetStringSlidingAsync(string key, string value, TimeSpan slidingExpiration, CancellationToken ct = default);

    /// <summary>删除缓存键。</summary>
    Task RemoveAsync(string key, CancellationToken ct = default);

    /// <summary>检查键是否存在。</summary>
    Task<bool> ExistsAsync(string key, CancellationToken ct = default);

    /// <summary>
    /// 如果键不存在则设置值（原子操作）。
    /// 返回 true 表示设置成功（键之前不存在），false 表示键已存在。
    /// 用于分布式锁、Nonce 防重放等场景。
    /// </summary>
    Task<bool> SetIfNotExistsAsync(string key, string value, TimeSpan expiration, CancellationToken ct = default);
}
