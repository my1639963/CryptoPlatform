using CryptoPlatform.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CryptoPlatform.Worker;

/// <summary>
/// 密钥过期检测 Worker。每分钟扫描到期密钥并更新状态为 EXPIRED。
/// </summary>
public sealed class KeyExpirationWorker : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<KeyExpirationWorker> _logger;

    public KeyExpirationWorker(IServiceProvider services, ILogger<KeyExpirationWorker> logger)
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

                var now = DateTime.UtcNow;
                var expired = await db.Keys
                    .Where(k => k.Status == "ACTIVE" && k.ExpiresAt != null && k.ExpiresAt < now)
                    .ToListAsync(stoppingToken);

                foreach (var key in expired)
                {
                    key.Status = "EXPIRED";
                    _logger.LogInformation("密钥 {KeyId} 已过期，状态更新为 EXPIRED", key.KeyId);
                }

                if (expired.Count > 0)
                    await db.SaveChangesAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "密钥过期检测异常");
            }
        }
    }
}
