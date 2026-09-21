using CryptoPlatform.Crypto.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace CryptoPlatform.Crypto.Abstractions;

/// <summary>
/// Crypto.Abstractions 层 DI 注册。
/// </summary>
public static class CryptoAbstractionsServiceCollectionExtensions
{
    public static IServiceCollection AddCryptoAbstractions(this IServiceCollection services)
    {
        // 接口和模型定义在此项目中，具体 Provider 由实现项目注册
        return services;
    }
}
