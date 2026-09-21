using CryptoPlatform.Domain.Entities;

namespace CryptoPlatform.Security;

/// <summary>
/// 安全事件服务接口。
/// 提供安全事件的检测、记录、查询和关闭能力。
/// </summary>
public interface ISecurityEventService
{
    /// <summary>上报安全事件（Phase 3 兼容签名）</summary>
    Task RaiseAsync(string eventType, string? relatedKeyId, string description, CancellationToken ct);

    /// <summary>上报安全事件（含应用 ID）</summary>
    Task RaiseAsync(string eventType, string? appId, string? keyId, string? description, CancellationToken ct);

    /// <summary>获取未关闭的安全事件列表</summary>
    Task<IReadOnlyList<SysSecurityEvent>> GetOpenEventsAsync(int limit, CancellationToken ct);

    /// <summary>关闭安全事件</summary>
    Task CloseAsync(string eventId, string handler, string result, CancellationToken ct);
}
