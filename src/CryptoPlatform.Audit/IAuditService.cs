namespace CryptoPlatform.Audit;

/// <summary>
/// 审计日志服务接口。
/// 提供审计日志写入能力，配合 SM3 哈希链确保日志不可篡改。
/// </summary>
public interface IAuditService
{
    /// <summary>
    /// 快捷记录审计日志（自动从 HTTP 上下文填充操作者信息）。
    /// </summary>
    Task LogAsync(string operation, string? keyId, int? keyVersion, string resultCode, CancellationToken ct);

    /// <summary>
    /// 记录完整审计日志条目。
    /// </summary>
    Task LogAsync(AuditLogEntry entry, CancellationToken ct);
}

/// <summary>
/// 审计日志条目（不可变记录）。
/// </summary>
public sealed record AuditLogEntry(
    string AuditId,
    string? RequestId,
    string? TraceId,
    DateTime Timestamp,
    string OperatorType,
    string? OperatorId,
    string? OperatorName,
    string? AppId,
    string? SourceIp,
    string Operation,
    string? KeyId,
    int? KeyVersion,
    string ResultCode,
    int? DurationMs);
