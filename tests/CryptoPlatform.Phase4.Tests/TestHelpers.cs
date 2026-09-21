using System.Threading;
using CryptoPlatform.Audit;
using CryptoPlatform.Authentication;
using CryptoPlatform.Domain.Entities;
using CryptoPlatform.Infrastructure.Caching;
using CryptoPlatform.Persistence;
using CryptoPlatform.Persistence.IdGeneration;
using CryptoPlatform.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace CryptoPlatform.Phase4.Tests;

/// <summary>
/// 测试用 InMemory DbContext 工厂。
/// 配置了 SnowflakeIdInterceptor 以自动分配唯一 ID。
/// </summary>
internal static class TestDbContextFactory
{
    private static int _dbCounter;

    public static CryptoPlatformDbContext Create()
    {
        var dbName = $"Phase4TestDb_{Interlocked.Increment(ref _dbCounter)}";
        var idGenerator = new SnowflakeIdGenerator(1);
        var interceptor = new SnowflakeIdInterceptor(idGenerator);

        var options = new DbContextOptionsBuilder<CryptoPlatformDbContext>()
            .UseInMemoryDatabase(dbName)
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .AddInterceptors(interceptor)
            .Options;

        var ctx = new CryptoPlatformDbContext(options);
        ctx.Database.EnsureCreated();
        return ctx;
    }
}

/// <summary>
/// 创建测试用的 ICacheService（基于内存分布式缓存）。
/// </summary>
internal static class TestCacheFactory
{
    public static ICacheService Create()
    {
        var options = Options.Create(new MemoryDistributedCacheOptions());
        var distributedCache = new MemoryDistributedCache(options);
        return new DistributedCacheService(distributedCache);
    }
}

/// <summary>
/// 测试数据工厂。
/// </summary>
internal static class TestDataFactory
{
    private static long _nextId = 10_000;
    private static long NextId() => Interlocked.Increment(ref _nextId);

    public static SysUser CreateUser(string username = "admin", string passwordHash = "")
    {
        return new SysUser
        {
            Id = NextId(),
            Username = username,
            PasswordHash = string.IsNullOrEmpty(passwordHash) ? AdminTokenService.ComputeSM3Hash("password123") : passwordHash,
            DisplayName = "Test Admin",
            Status = 0,
            LoginFailCount = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public static SysApplication CreateApplication(string appId = "TEST_APP")
    {
        return new SysApplication
        {
            Id = NextId(),
            AppId = appId,
            AppName = "Test Application",
            Status = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public static SysApplicationSecret CreateSecret(long applicationId, string secretHash = "test-secret-hash")
    {
        return new SysApplicationSecret
        {
            Id = NextId(),
            ApplicationId = applicationId,
            SecretHash = secretHash,
            SecretVersion = 1,
            Status = 1,
            CreatedAt = DateTime.UtcNow
        };
    }

    public static AuditLogEntry CreateAuditEntry(string operation = "TEST_OP", string? keyId = null)
    {
        return new AuditLogEntry(
            AuditId: Guid.NewGuid().ToString("N"),
            RequestId: "req-" + Guid.NewGuid().ToString("N")[..8],
            TraceId: "trace-" + Guid.NewGuid().ToString("N")[..8],
            Timestamp: DateTime.UtcNow,
            OperatorType: "SYSTEM",
            OperatorId: "test-operator",
            OperatorName: "Test Operator",
            AppId: "TEST_APP",
            SourceIp: "127.0.0.1",
            Operation: operation,
            KeyId: keyId,
            KeyVersion: 1,
            ResultCode: "SUCCESS",
            DurationMs: 42);
    }
}
