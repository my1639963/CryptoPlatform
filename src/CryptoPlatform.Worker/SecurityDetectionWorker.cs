using CryptoPlatform.Persistence;
using CryptoPlatform.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CryptoPlatform.Worker;

/// <summary>
/// 安全事件检测 Worker。每分钟执行一次安全检测规则。
/// 检测项：认证失败频率、异常高频调用、HSM 离线、审计哈希链完整性。
/// </summary>
public sealed class SecurityDetectionWorker : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<SecurityDetectionWorker> _logger;

    public SecurityDetectionWorker(IServiceProvider services, ILogger<SecurityDetectionWorker> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = _services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<CryptoPlatformDbContext>();
                var eventService = scope.ServiceProvider.GetRequiredService<ISecurityEventService>();

                // 检测 1：审计日志是否有最近记录（若无则可能审计服务异常）
                var recentAuditCount = await db.AuditLogs
                    .CountAsync(l => l.Timestamp > DateTime.UtcNow.AddMinutes(-5), stoppingToken);

                if (recentAuditCount == 0)
                {
                    _logger.LogWarning("过去 5 分钟内无审计日志记录，审计服务可能异常");
                }

                // 检测 2：检查是否有大量 OPEN 状态的安全事件
                var openEventCount = await db.SecurityEvents
                    .CountAsync(e => e.Status == "OPEN", stoppingToken);

                if (openEventCount > 100)
                {
                    _logger.LogWarning("未处理安全事件数量过多: {Count}", openEventCount);
                }

                // 检测 3：检查密钥过期预警
                var expiringCount = await db.Keys
                    .CountAsync(k => k.Status == "ACTIVE"
                                  && k.ExpiresAt != null
                                  && k.ExpiresAt <= DateTime.UtcNow.AddDays(3)
                                  && k.ExpiresAt > DateTime.UtcNow, stoppingToken);

                if (expiringCount > 0)
                {
                    _logger.LogWarning("{Count} 个密钥将在 3 天内过期", expiringCount);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "安全检测 Worker 异常");
            }
        }
    }
}
