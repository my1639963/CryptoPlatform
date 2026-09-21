using CryptoPlatform.Authentication.PasswordHashing;
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

        // ── 密码哈希器注册 ──
        // 默认 Hasher：PBKDF2-SHA256（同时注册为 IPasswordHasher 默认实现）
        var defaultHasher = new Pbkdf2PasswordHasher();
        services.AddSingleton<IPasswordHasher>(defaultHasher);

        // 所有可用 Hasher（供 Factory 枚举）
        services.AddSingleton<IPasswordHasher>(new Sm3PasswordHasher());

        // 密码哈希器工厂
        services.AddSingleton<IPasswordHasherFactory>(sp =>
        {
            var hashers = sp.GetServices<IPasswordHasher>();
            return new PasswordHasherFactory(hashers, defaultHasher);
        });

        // 管理员 Token 服务
        services.AddScoped<IAdminTokenService, AdminTokenService>();

        // 应用凭据认证服务
        services.AddScoped<IAppAuthenticationService, AppAuthenticationService>();

        return services;
    }
}
