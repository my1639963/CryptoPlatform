using System.Collections;
using System.Linq.Expressions;
using CryptoPlatform.Domain.Entities;
using CryptoPlatform.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata;

namespace CryptoPlatform.Application.Tests;

/// <summary>
/// 测试辅助：创建使用 InMemory 数据库的 CryptoPlatformDbContext。
/// 每个测试使用独立的数据库名，确保测试隔离。
/// </summary>
public static class TestDbContextFactory
{
    public static CryptoPlatformDbContext Create(string? dbName = null)
    {
        var options = new DbContextOptionsBuilder<CryptoPlatformDbContext>()
            .UseInMemoryDatabase(databaseName: dbName ?? Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var ctx = new CryptoPlatformDbContext(options);
        ctx.Database.EnsureCreated();
        return ctx;
    }

    /// <summary>
    /// 向 DbContext 中插入测试数据（Keys）
    /// </summary>
    public static async Task SeedKeyAsync(CryptoPlatformDbContext db, SysKey key)
    {
        db.Keys.Add(key);
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// 向 DbContext 中插入测试数据（KeyVersion）
    /// </summary>
    public static async Task SeedKeyVersionAsync(CryptoPlatformDbContext db, SysKeyVersion version)
    {
        db.KeyVersions.Add(version);
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// 向 DbContext 中插入测试数据（KeyAuthorization）
    /// </summary>
    public static async Task SeedKeyAuthorizationAsync(CryptoPlatformDbContext db, SysKeyAuthorization auth)
    {
        db.KeyAuthorizations.Add(auth);
        await db.SaveChangesAsync();
    }
}

/// <summary>
/// 默认测试数据工厂
/// </summary>
public static class TestDataFactory
{
    private static long _nextId = 1000;
    private static long NextId() => Interlocked.Increment(ref _nextId);

    public static SysKey CreateKey(
        string keyId = "KEY-test001",
        string ownerAppId = "APP-001",
        string keyType = "SM4",
        string keyUsage = "ENCRYPT",
        string status = "ACTIVE",
        int? currentVersion = 1)
    {
        return new SysKey
        {
            Id = NextId(),
            KeyId = keyId,
            OwnerAppId = ownerAppId,
            KeyType = keyType,
            KeyUsage = keyUsage,
            Name = $"Test Key {keyId}",
            Status = status,
            CurrentVersion = currentVersion,
            CreatedAt = DateTime.UtcNow,
            ConcurrencyStamp = Guid.NewGuid().ToString("N")
        };
    }

    public static SysKeyVersion CreateVersion(
        string keyId = "KEY-test001",
        int versionNo = 1,
        string status = "ACTIVE",
        string providerKeyRef = "SOFTWARE:test:1")
    {
        return new SysKeyVersion
        {
            Id = NextId(),
            KeyId = keyId,
            VersionNo = versionNo,
            ProviderType = "SOFTWARE",
            ProviderKeyRef = providerKeyRef,
            Fingerprint = "TEST-FINGERPRINT",
            Status = status,
            CreatedAt = DateTime.UtcNow,
            ConcurrencyStamp = Guid.NewGuid().ToString("N")
        };
    }

    public static SysKeyAuthorization CreateAuthorization(
        string keyId = "KEY-test001",
        string appId = "APP-002",
        string permissions = "ENCRYPT,DECRYPT",
        string status = "ACTIVE")
    {
        return new SysKeyAuthorization
        {
            Id = NextId(),
            KeyId = keyId,
            AppId = appId,
            Permissions = permissions,
            Status = status,
            CreatedAt = DateTime.UtcNow
        };
    }
}
