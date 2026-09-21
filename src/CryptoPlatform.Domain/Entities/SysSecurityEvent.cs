namespace CryptoPlatform.Domain.Entities;

/// <summary>
/// 安全事件。生命周期：发现 → 告警 → 确认 → 处置 → 恢复 → 关闭。
/// </summary>
public sealed class SysSecurityEvent
{
    public long Id { get; set; }
    public string EventId { get; set; } = null!;
    public string EventType { get; set; } = null!;
    public string Severity { get; set; } = null!;
    public string? AppId { get; set; }
    public string? OperatorId { get; set; }
    public string? SourceIp { get; set; }
    public string? RelatedKeyId { get; set; }
    public string? RequestId { get; set; }
    public string? Description { get; set; }
    public string Status { get; set; } = null!;
    public DateTime DetectedAt { get; set; }
    public DateTime? HandledAt { get; set; }
    public string? Handler { get; set; }
    public string? HandlingResult { get; set; }
    public DateTime CreatedAt { get; set; }
}
