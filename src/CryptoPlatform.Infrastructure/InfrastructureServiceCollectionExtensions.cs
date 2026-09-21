using CryptoPlatform.Infrastructure.Caching;
using CryptoPlatform.Infrastructure.Locking;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace CryptoPlatform.Infrastructure;

/// <summary>
/// 基础设施层 DI 注册扩展方法。
/// </summary>
public static class InfrastructureServiceCollectionExtensions
{
    /// <summary>
    /// 注册缓存抽象服务（ICacheService）。
    /// 必须先注册 IDistributedCache（内存或 Redis）。
    /// </summary>
    public static IServiceCollection AddCacheService(this IServiceCollection services)
    {
        services.AddSingleton<ICacheService, DistributedCacheService>();
        return services;
    }

    /// <summary>
    /// 注册内存缓存（开发环境）。
    /// 同时注册 ICacheService 和 IDistributedLock（内存版）。
    /// </summary>
    public static IServiceCollection AddMemoryCacheInfrastructure(this IServiceCollection services)
    {
        services.AddDistributedMemoryCache();
        services.AddCacheService();
        services.AddSingleton<IDistributedLock, InMemoryDistributedLock>();
        return services;
    }

    /// <summary>
    /// 注册 Redis 缓存（生产环境）。
    /// 同时注册 IConnectionMultiplexer、ICacheService 和 RedisDistributedLock。
    /// </summary>
    public static IServiceCollection AddRedisCacheInfrastructure(
        this IServiceCollection services,
        string redisConnectionString)
    {
        // Redis 分布式缓存
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnectionString;
            options.InstanceName = "CryptoPlatform:";
        });

        // Redis 连接复用器（用于分布式锁等需要原生 Redis 操作的场景）
        services.AddSingleton<IConnectionMultiplexer>(
            ConnectionMultiplexer.Connect(redisConnectionString));

        // 缓存抽象
        services.AddCacheService();

        // Redis 分布式锁
        services.AddSingleton<IDistributedLock, RedisDistributedLock>();

        return services;
    }
}
