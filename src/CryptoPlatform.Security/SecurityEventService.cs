using CryptoPlatform.Domain;
using CryptoPlatform.Domain.Entities;
using CryptoPlatform.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CryptoPlatform.Security;

/// <summary>
/// 安全事件服务实现。
/// 负责安全事件的检测、记录、严重等级判定和告警触发。
/// </summary>
public sealed class SecurityEventService : ISecurityEventService
{
    private readonly CryptoPlatformDbContext _db;
    private readonly ILogger<SecurityEventService> _logger;

    public SecurityEventService(
        CryptoPlatformDbContext db,
        ILogger<SecurityEventService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task RaiseAsync(string eventType, string? relatedKeyId, string description, CancellationToken ct)
    {
        await RaiseAsync(eventType, null, relatedKeyId, description, ct);
    }

    public async Task RaiseAsync(string eventType, string? appId, string? keyId, string? description, CancellationToken ct)
    {
        var severity = DetermineSeverity(eventType);
        var evt = new SysSecurityEvent
        {
            EventId = $"EVT-{Guid.NewGuid():N}",
            EventType = eventType,
            Severity = severity,
            AppId = appId,
            RelatedKeyId = keyId,
            Description = description,
            Status = "OPEN",
            DetectedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _db.SecurityEvents.Add(evt);
        await _db.SaveChangesAsync(ct);

        _logger.LogWarning("安全事件 [{Severity}] {Type}: {Desc}", severity, eventType, description);

        // CRITICAL 级别触发即时告警
        if (severity == "CRITICAL")
            await SendAlertAsync(evt, ct);
    }

    public async Task<IReadOnlyList<SysSecurityEvent>> GetOpenEventsAsync(int limit, CancellationToken ct)
    {
        return await _db.SecurityEvents
            .Where(e => e.Status == "OPEN" || e.Status == "ACKNOWLEDGED")
            .OrderByDescending(e => e.DetectedAt)
            .Take(limit)
            .ToListAsync(ct);
    }

    public async Task CloseAsync(string eventId, string handler, string result, CancellationToken ct)
    {
        var evt = await _db.SecurityEvents.FirstOrDefaultAsync(e => e.EventId == eventId, ct)
            ?? throw new BusinessException("EVENT_NOT_FOUND", "安全事件不存在");

        evt.Status = "CLOSED";
        evt.HandledAt = DateTime.UtcNow;
        evt.Handler = handler;
        evt.HandlingResult = result;
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("安全事件 {EventId} 已关闭，处置人={Handler}，结果={Result}", eventId, handler, result);
    }

    /// <summary>
    /// 根据事件类型判定严重等级。
    /// </summary>
    private static string DetermineSeverity(string eventType) => eventType switch
    {
        // 严重
        "HSM_OFFLINE" or "AUDIT_INTEGRITY_FAILURE" or "KEY_CREATE_FAILED"
            => "CRITICAL",

        // 高
        "APP_SECRET_BRUTE_FORCE" or "TOKEN_REPLAY" or "AUTH_SIGNATURE_INVALID"
            => "HIGH",

        // 中
        "AUTH_FAILURE" or "IP_POLICY_VIOLATION" or "NONCE_REPLAYED"
            => "MEDIUM",

        // 低
        "KEY_ROTATION_REQUIRED" or "KEY_EXPIRING_SOON"
            => "LOW",

        _ => "INFO"
    };

    private Task SendAlertAsync(SysSecurityEvent evt, CancellationToken ct)
    {
        // TODO: 接入告警通道（邮件/短信/企微/钉钉）
        _logger.LogCritical("ALERT: 安全事件 {EventId} - {Type} [{Severity}]",
            evt.EventId, evt.EventType, evt.Severity);
        return Task.CompletedTask;
    }
}
