using CryptoPlatform.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CryptoPlatform.Worker;

/// <summary>
/// 密钥自动轮换 Worker。提前 N 天检测即将过期的 ACTIVE 密钥并触发轮换。
/// 配置项：KeyRotation:AdvanceDays（默认 7 天）
/// </summary>
public sealed class KeyRotationWorker : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<KeyRotationWorker> _logger;
    private readonly int _advanceDays;

    public KeyRotationWorker(
        IServiceProvider services,
        ILogger<KeyRotationWorker> logger,
        IConfiguration configuration)
    {
        _services = services;
        _logger = logger;
        _advanceDays = configuration.GetValue("KeyRotation:AdvanceDays", 7);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = _services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<CryptoPlatformDbContext>();

                var threshold = DateTime.UtcNow.AddDays(_advanceDays);
                var expiringKeys = await db.Keys
                    .Where(k => k.Status == "ACTIVE"
                             && k.ExpiresAt != null
                             && k.ExpiresAt <= threshold
                             && k.ExpiresAt > DateTime.UtcNow)
                    .ToListAsync(stoppingToken);

                foreach (var key in expiringKeys)
                {
                    _logger.LogWarning(
                        "密钥 {KeyId} 将在 {Days} 天后过期（{ExpiresAt}），建议轮换",
                        key.KeyId,
                        (key.ExpiresAt!.Value - DateTime.UtcNow).Days,
                        key.ExpiresAt);

                    // TODO: 自动触发轮换逻辑（需要调用 IKeyService.RotateAsync）
                    // 当前仅记录告警日志，生产环境应集成自动轮换
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "密钥轮换检测异常");
            }
        }
    }
}
