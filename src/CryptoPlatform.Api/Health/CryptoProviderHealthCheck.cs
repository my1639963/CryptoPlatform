using CryptoPlatform.Crypto.Abstractions;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CryptoPlatform.Api.Health;

/// <summary>
/// Provider 健康检查。
/// 调用所有已注册 Provider 的 CheckHealthAsync。
/// </summary>
public sealed class CryptoProviderHealthCheck : IHealthCheck
{
    private readonly ICryptoProviderRouter _router;

    public CryptoProviderHealthCheck(ICryptoProviderRouter router)
    {
        _router = router;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var providers = _router.GetAllProviders();
        if (providers.Count == 0)
            return HealthCheckResult.Unhealthy("没有已注册的密码运算 Provider");

        var data = new Dictionary<string, object>();
        var allHealthy = true;

        foreach (var provider in providers)
        {
            try
            {
                var result = await provider.CheckHealthAsync(cancellationToken);
                data[$"{provider.ProviderName}({provider.ProviderType})"] = new
                {
                    result.Status,
                    LatencyMs = result.Latency.TotalMilliseconds,
                    result.Message
                };

                if (result.Status != "HEALTHY")
                    allHealthy = false;
            }
            catch (Exception ex)
            {
                data[$"{provider.ProviderName}({provider.ProviderType})"] = new
                {
                    Status = "ERROR",
                    ex.Message
                };
                allHealthy = false;
            }
        }

        return allHealthy
            ? HealthCheckResult.Healthy("所有 Provider 正常", data)
            : HealthCheckResult.Degraded("部分 Provider 异常", null, data);
    }
}
