using Microsoft.Extensions.DependencyInjection;

namespace CryptoPlatform.Authentication;

public static class AuthenticationServiceCollectionExtensions
{
    public static IServiceCollection AddAuthenticationServices(this IServiceCollection services, JwtSettings? jwtSettings = null)
    {
        // JWT 配置
        var settings = jwtSettings ?? new JwtSettings();
        services.AddSingleton(settings);

        // 令牌生成器
        services.AddSingleton<ITokenGenerator, JwtTokenGenerator>();

        // 管理员 Token 服务
        services.AddScoped<IAdminTokenService, AdminTokenService>();

        // 应用凭据认证服务
        services.AddScoped<IAppAuthenticationService, AppAuthenticationService>();

        return services;
    }
}
