namespace CryptoPlatform.Crypto.Abstractions;

/// <summary>
/// Provider 路由抽象。根据密钥绑定关系或系统配置选择对应的 Provider。
/// </summary>
public interface ICryptoProviderRouter
{
    /// <summary>获取系统默认 Provider</summary>
    ICryptoProvider GetDefaultProvider();

    /// <summary>根据已有密钥版本的 providerType 路由到对应 Provider</summary>
    ICryptoProvider ResolveProvider(string providerType);

    /// <summary>根据设备 ID 路由到指定 Provider</summary>
    ICryptoProvider ResolveByDevice(string deviceId);

    /// <summary>获取所有已注册 Provider</summary>
    IReadOnlyList<ICryptoProvider> GetAllProviders();
}
