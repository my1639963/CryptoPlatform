using CryptoPlatform.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CryptoPlatform.Worker;

/// <summary>
/// Saga 补偿 Worker。定期扫描处于中间状态的密钥记录并尝试补偿。
/// 检测 REVOKED 状态但仍有未创建版本的密钥（Provider 成功但 DB 写入失败的情况）。
/// </summary>
public sealed class CompensationWorker : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<CompensationWorker> _logger;

    public CompensationWorker(IServiceProvider services, ILogger<CompensationWorker> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = _services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<CryptoPlatformDbContext>();

                // 检测：CREATED 状态超过 10 分钟但未激活的密钥（可能创建过程中断）
                var staleCreated = await db.Keys
                    .Where(k => k.Status == "CREATED"
                             && k.CreatedAt < DateTime.UtcNow.AddMinutes(-10))
                    .ToListAsync(stoppingToken);

                foreach (var key in staleCreated)
                {
                    _logger.LogWarning(
                        "检测到停滞的 CREATED 密钥: {KeyId}，创建于 {CreatedAt}，尝试补偿",
                        key.KeyId, key.CreatedAt);

                    // 补偿策略：将停滞的 CREATED 密钥标记为 REVOKED
                    key.Status = "REVOKED";
                }

                if (staleCreated.Count > 0)
                {
                    await db.SaveChangesAsync(stoppingToken);
                    _logger.LogInformation("Saga 补偿完成，处理了 {Count} 条停滞记录", staleCreated.Count);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Saga 补偿 Worker 异常");
            }
        }
    }
}
