using Microsoft.Extensions.DependencyInjection;

namespace CryptoPlatform.Authorization;

/// <summary>
/// Authorization 层 DI 注册。
/// 注册密钥授权检查器。
/// </summary>
public static class AuthorizationServiceCollectionExtensions
{
    public static IServiceCollection AddPlatformAuthorization(this IServiceCollection services)
    {
        services.AddScoped<IKeyAuthorizationChecker, KeyAuthorizationChecker>();
        return services;
    }
}
