using CryptoPlatform.Crypto.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CryptoPlatform.Worker;

/// <summary>
/// Provider 健康检查 Worker。每 30 秒检测已注册的密码运算 Provider 是否可用。
/// </summary>
public sealed class HealthCheckWorker : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<HealthCheckWorker> _logger;

    public HealthCheckWorker(IServiceProvider services, ILogger<HealthCheckWorker> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = _services.CreateScope();
                var router = scope.ServiceProvider.GetRequiredService<ICryptoProviderRouter>();

                // 检查默认 Provider 是否可用
                try
                {
                    var provider = router.GetDefaultProvider();
                    _logger.LogDebug("Provider 健康检查通过: {ProviderType}", provider.GetType().Name);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Provider 健康检查失败：无法获取默认 Provider");
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "HealthCheck Worker 异常");
            }
        }
    }
}
