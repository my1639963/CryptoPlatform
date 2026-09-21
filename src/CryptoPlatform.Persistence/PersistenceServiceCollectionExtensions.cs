using CryptoPlatform.Persistence.IdGeneration;
using CryptoPlatform.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CryptoPlatform.Persistence;

public static class PersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── 雪花 ID 生成器（单例） ──
        var workerId = long.TryParse(configuration["Snowflake:WorkerId"], out var id) ? id : 0L;
        services.AddSingleton(new SnowflakeIdGenerator(workerId));

        // ── 拦截器（单例） ──
        services.AddSingleton<UpdatedAtInterceptor>();
        services.AddSingleton<SnowflakeIdInterceptor>();

        services.AddDbContext<CryptoPlatformDbContext>((sp, options) =>
        {
            var connectionString = configuration.GetConnectionString("Default")
                ?? throw new InvalidOperationException("缺少连接字符串配置: ConnectionStrings:Default");
            options.UseMySQL(connectionString);

            // 注入拦截器
            options.AddInterceptors(
                sp.GetRequiredService<UpdatedAtInterceptor>(),
                sp.GetRequiredService<SnowflakeIdInterceptor>());
        });

        return services;
    }
}
