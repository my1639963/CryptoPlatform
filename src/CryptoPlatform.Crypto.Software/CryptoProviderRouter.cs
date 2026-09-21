using CryptoPlatform.Crypto.Abstractions;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace CryptoPlatform.Crypto.Software;

/// <summary>
/// Provider 路由实现。
/// 支持多 Provider 注册、按类型/设备路由、健康状态跟踪。
/// 当前 Phase 6 仅注册 SOFTWARE Provider；HSM 接入后自动参与路由。
/// </summary>
public sealed class CryptoProviderRouter : ICryptoProviderRouter
{
    private readonly ICryptoProvider[] _providers;
    private readonly ILogger<CryptoProviderRouter> _logger;

    // 设备健康状态跟踪：连续失败计数
    private readonly ConcurrentDictionary<string, ProviderHealthState> _healthStates = new();

    /// <summary>连续失败 N 次 → DEGRADED</summary>
    private const int DegradedThreshold = 3;
    /// <summary>连续失败 N 次 → OFFLINE</summary>
    private const int OfflineThreshold = 5;

    public CryptoProviderRouter(
        IEnumerable<ICryptoProvider> providers,
        ILogger<CryptoProviderRouter> logger)
    {
        _providers = providers.ToArray();
        _logger = logger;

        // 初始化健康状态
        foreach (var provider in _providers)
        {
            _healthStates[provider.ProviderType] = new ProviderHealthState();
        }
    }

    public ICryptoProvider GetDefaultProvider()
    {
        // 优先选择 HEALTHY 的 Provider
        var healthy = _providers.FirstOrDefault(p => GetHealthStatus(p.ProviderType) != "OFFLINE");
        return healthy ?? _providers.FirstOrDefault()
            ?? throw new CryptoProviderException(
                ProviderErrorCodes.PROVIDER_UNAVAILABLE,
                "没有已注册的密码运算 Provider");
    }

    public ICryptoProvider ResolveProvider(string providerType)
    {
        var provider = _providers.FirstOrDefault(p =>
            string.Equals(p.ProviderType, providerType, StringComparison.OrdinalIgnoreCase));

        if (provider is null)
        {
            _logger.LogError("Provider {ProviderType} 未注册，已注册: {RegisteredTypes}",
                providerType, string.Join(", ", _providers.Select(p => p.ProviderType)));

            throw new CryptoProviderException(
                ProviderErrorCodes.PROVIDER_UNAVAILABLE,
                $"Provider {providerType} 未注册");
        }

        // 检查健康状态
        var status = GetHealthStatus(providerType);
        if (status == "OFFLINE")
        {
            _logger.LogWarning("Provider {ProviderType} 当前状态为 OFFLINE", providerType);
        }

        return provider;
    }

    public ICryptoProvider ResolveByDevice(string deviceId)
    {
        // 当前无 HSM 设备注册，返回默认 Provider
        // HSM 接入后：查询 sys_crypto_device 表，按 deviceId 解析对应 Provider
        _logger.LogDebug("ResolveByDevice({DeviceId}) → 返回默认 Provider", deviceId);
        return GetDefaultProvider();
    }

    public IReadOnlyList<ICryptoProvider> GetAllProviders() => _providers;

    /// <summary>
    /// 报告 Provider 健康检查结果。
    /// 由 HealthCheckWorker 定期调用。
    /// </summary>
    public void ReportHealth(string providerType, bool healthy)
    {
        var state = _healthStates.GetOrAdd(providerType, _ => new ProviderHealthState());

        if (healthy)
        {
            state.ConsecutiveFailures = 0;
            if (state.Status != "HEALTHY")
            {
                _logger.LogInformation("Provider {ProviderType} 恢复健康", providerType);
                state.Status = "HEALTHY";
            }
        }
        else
        {
            var failures = Interlocked.Increment(ref state._consecutiveFailures);
            if (failures >= OfflineThreshold && state.Status != "OFFLINE")
            {
                _logger.LogError("Provider {ProviderType} 连续 {Failures} 次健康检查失败 → OFFLINE",
                    providerType, failures);
                state.Status = "OFFLINE";
            }
            else if (failures >= DegradedThreshold && state.Status == "HEALTHY")
            {
                _logger.LogWarning("Provider {ProviderType} 连续 {Failures} 次健康检查失败 → DEGRADED",
                    providerType, failures);
                state.Status = "DEGRADED";
            }
        }
    }

    /// <summary>获取 Provider 当前健康状态</summary>
    public string GetHealthStatus(string providerType)
    {
        return _healthStates.TryGetValue(providerType, out var state)
            ? state.Status
            : "UNKNOWN";
    }

    /// <summary>
    /// Provider 健康状态跟踪。
    /// </summary>
    private sealed class ProviderHealthState
    {
        public string Status { get; set; } = "HEALTHY";
        internal int _consecutiveFailures;

        public int ConsecutiveFailures
        {
            get => _consecutiveFailures;
            set => _consecutiveFailures = value;
        }
    }
}
