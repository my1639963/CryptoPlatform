using CryptoPlatform.Domain.Entities;
using CryptoPlatform.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace CryptoPlatform.Audit;

/// <summary>
/// 审计日志服务实现。
/// 自动从 HTTP 上下文填充操作者信息，并通过 SM3 哈希链确保日志不可篡改。
/// </summary>
public sealed class AuditService : IAuditService
{
    private readonly CryptoPlatformDbContext _db;
    private readonly IHttpContextAccessor _http;
    private readonly IHashChainService _hashChain;
    private readonly ILogger<AuditService> _logger;

    public AuditService(
        CryptoPlatformDbContext db,
        IHttpContextAccessor http,
        IHashChainService hashChain,
        ILogger<AuditService> logger)
    {
        _db = db;
        _http = http;
        _hashChain = hashChain;
        _logger = logger;
    }

    public async Task LogAsync(string operation, string? keyId, int? keyVersion, string resultCode, CancellationToken ct)
    {
        var ctx = _http.HttpContext;
        var entry = new AuditLogEntry(
            AuditId: Guid.NewGuid().ToString("N"),
            RequestId: ctx?.TraceIdentifier,
            TraceId: ctx?.TraceIdentifier,
            Timestamp: DateTime.UtcNow,
            OperatorType: ctx?.Items["OperatorType"]?.ToString() ?? "SYSTEM",
            OperatorId: ctx?.Items["OperatorId"]?.ToString() ?? ctx?.Items["AppId"]?.ToString(),
            OperatorName: null,
            AppId: ctx?.Items["AppId"]?.ToString(),
            SourceIp: ctx?.Connection.RemoteIpAddress?.ToString(),
            Operation: operation,
            KeyId: keyId,
            KeyVersion: keyVersion,
            ResultCode: resultCode,
            DurationMs: null);

        await LogAsync(entry, ct);
    }

    public async Task LogAsync(AuditLogEntry entry, CancellationToken ct)
    {
        // 计算哈希链
        var previousHash = await _hashChain.GetLatestHashAsync(ct);
        var currentHash = _hashChain.ComputeHash(entry, previousHash);

        var log = new SysAuditLog
        {
            AuditId = entry.AuditId,
            RequestId = entry.RequestId,
            TraceId = entry.TraceId,
            Timestamp = entry.Timestamp,
            OperatorType = entry.OperatorType,
            OperatorId = entry.OperatorId,
            OperatorName = entry.OperatorName,
            AppId = entry.AppId,
            SourceIp = entry.SourceIp,
            Operation = entry.Operation,
            KeyId = entry.KeyId,
            KeyVersion = entry.KeyVersion,
            ResultCode = entry.ResultCode,
            DurationMs = entry.DurationMs,
            PreviousHash = previousHash,
            CurrentHash = currentHash,
            CreatedAt = DateTime.UtcNow
        };

        _db.AuditLogs.Add(log);
        await _db.SaveChangesAsync(ct);

        // 更新缓存中的最新哈希
        await _hashChain.UpdateLatestHashAsync(currentHash, ct);

        _logger.LogDebug("审计日志 {AuditId}: {Operation} [{ResultCode}]", entry.AuditId, entry.Operation, entry.ResultCode);
    }
}
