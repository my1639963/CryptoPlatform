using CryptoPlatform.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace CryptoPlatform.Application;

/// <summary>
/// Application 层 DI 注册。
/// 注册 IKeyService、ICryptoService 等核心业务服务。
/// </summary>
/// <remarks>
/// 注意：AddHttpContextAccessor() 和 AddPlatformAuthorization() 需在宿主程序中分别调用。
/// </remarks>
public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // 注册操作上下文（依赖 IHttpContextAccessor，需在宿主中先调用 AddHttpContextAccessor）
        services.AddScoped<IOperationContext, HttpOperationContext>();

        // 注册核心业务服务
        services.AddScoped<Keys.IKeyService, Keys.KeyService>();
        services.AddScoped<Crypto.ICryptoService, Crypto.CryptoService>();

        return services;
    }
}
