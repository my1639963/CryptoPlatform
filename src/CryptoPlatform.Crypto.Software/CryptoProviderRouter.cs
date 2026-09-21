using CryptoPlatform.Crypto.Abstractions;
using Microsoft.Extensions.Logging;

namespace CryptoPlatform.Crypto.Software;

/// <summary>
/// 默认 Provider 路由实现。
/// Phase 2 仅支持单 Provider（SOFTWARE），后续 HSM 接入后扩展路由逻辑。
/// </summary>
public sealed class CryptoProviderRouter : ICryptoProviderRouter
{
    private readonly ICryptoProvider[] _providers;
    private readonly ILogger<CryptoProviderRouter> _logger;

    public CryptoProviderRouter(
        IEnumerable<ICryptoProvider> providers,
        ILogger<CryptoProviderRouter> logger)
    {
        _providers = providers.ToArray();
        _logger = logger;
    }

    public ICryptoProvider GetDefaultProvider()
    {
        return _providers.FirstOrDefault()
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

        return provider;
    }

    public ICryptoProvider ResolveByDevice(string deviceId)
    {
        // Phase 2: 无 HSM 设备，仅返回默认 Provider
        // Phase 3+: 查询 sys_crypto_device 表，按 deviceId 解析 Provider
        _logger.LogWarning("ResolveByDevice({DeviceId}) 当前仅返回默认 Provider（Phase 2 无 HSM 设备）", deviceId);
        return GetDefaultProvider();
    }

    public IReadOnlyList<ICryptoProvider> GetAllProviders() => _providers;
}
