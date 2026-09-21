using CryptoPlatform.Crypto.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace CryptoPlatform.Crypto.Software;

/// <summary>
/// Software Crypto Provider 层 DI 注册。
/// 注册 SoftwareCryptoProvider 和 CryptoProviderRouter。
/// </summary>
public static class CryptoSoftwareServiceCollectionExtensions
{
    public static IServiceCollection AddSoftwareCryptoProvider(this IServiceCollection services)
    {
        services.AddSingleton<ICryptoProvider, SoftwareCryptoProvider>();
        services.AddSingleton<ICryptoProviderRouter, CryptoProviderRouter>();
        return services;
    }
}
