namespace CryptoPlatform.Domain.Entities;

/// <summary>
/// 审计日志（不可篡改，追加写入）。
/// 包含 SM3 哈希链字段：PreviousHash / CurrentHash。
/// </summary>
public sealed class SysAuditLog
{
    public long Id { get; set; }
    public string AuditId { get; set; } = null!;
    public string? RequestId { get; set; }
    public string? TraceId { get; set; }
    public DateTime Timestamp { get; set; }
    public string OperatorType { get; set; } = null!;
    public string? OperatorId { get; set; }
    public string? OperatorName { get; set; }
    public string? AppId { get; set; }
    public string? SourceIp { get; set; }
    public string Operation { get; set; } = null!;
    public string? KeyId { get; set; }
    public int? KeyVersion { get; set; }
    public string ResultCode { get; set; } = null!;
    public int? DurationMs { get; set; }
    public string? PreviousHash { get; set; }
    public string CurrentHash { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}
