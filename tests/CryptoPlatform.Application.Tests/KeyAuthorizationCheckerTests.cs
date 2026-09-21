using CryptoPlatform.Authorization;
using CryptoPlatform.Domain;
using CryptoPlatform.Domain.Entities;
using CryptoPlatform.Persistence;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace CryptoPlatform.Application.Tests;

/// <summary>
/// KeyAuthorizationChecker 单元测试。
/// 覆盖 Owner 放行、显式授权、过期校验、权限校验。
/// </summary>
public class KeyAuthorizationCheckerTests : IDisposable
{
    private readonly CryptoPlatformDbContext _db;
    private readonly KeyAuthorizationChecker _checker;

    public KeyAuthorizationCheckerTests()
    {
        _db = TestDbContextFactory.Create();
        _checker = new KeyAuthorizationChecker(_db, Mock.Of<ILogger<KeyAuthorizationChecker>>());
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    [Fact]
    public async Task Check_OwnerAppId_ShouldPass()
    {
        var key = TestDataFactory.CreateKey(ownerAppId: "APP-OWNER");
        await _db.Keys.AddAsync(key);
        await _db.SaveChangesAsync();

        var act = () => _checker.CheckAsync(key.KeyId, "APP-OWNER", "ENCRYPT", CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Check_AuthorizedNonOwner_WithCorrectPermission_ShouldPass()
    {
        var key = TestDataFactory.CreateKey(ownerAppId: "APP-OWNER");
        var auth = TestDataFactory.CreateAuthorization(appId: "APP-CALLER", permissions: "ENCRYPT,DECRYPT");
        await _db.Keys.AddAsync(key);
        await _db.KeyAuthorizations.AddAsync(auth);
        await _db.SaveChangesAsync();

        var act = () => _checker.CheckAsync(key.KeyId, "APP-CALLER", "ENCRYPT", CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Check_NonOwnerWithoutAuthorization_ShouldThrow()
    {
        var key = TestDataFactory.CreateKey(ownerAppId: "APP-OWNER");
        await _db.Keys.AddAsync(key);
        await _db.SaveChangesAsync();

        var act = () => _checker.CheckAsync(key.KeyId, "APP-STRANGER", "ENCRYPT", CancellationToken.None);

        await act.Should().ThrowAsync<BusinessException>()
            .Where(e => e.Code == "KEY_PERMISSION_DENIED");
    }

    [Fact]
    public async Task Check_ExpiredAuthorization_ShouldThrow()
    {
        var key = TestDataFactory.CreateKey(ownerAppId: "APP-OWNER");
        var auth = TestDataFactory.CreateAuthorization(appId: "APP-CALLER");
        auth.ExpiresAt = DateTime.UtcNow.AddHours(-1); // 已过期
        await _db.Keys.AddAsync(key);
        await _db.KeyAuthorizations.AddAsync(auth);
        await _db.SaveChangesAsync();

        var act = () => _checker.CheckAsync(key.KeyId, "APP-CALLER", "ENCRYPT", CancellationToken.None);

        await act.Should().ThrowAsync<BusinessException>()
            .Where(e => e.Code == "KEY_AUTH_EXPIRED");
    }

    [Fact]
    public async Task Check_WrongPermission_ShouldThrow()
    {
        var key = TestDataFactory.CreateKey(ownerAppId: "APP-OWNER");
        var auth = TestDataFactory.CreateAuthorization(appId: "APP-CALLER", permissions: "DECRYPT");
        await _db.Keys.AddAsync(key);
        await _db.KeyAuthorizations.AddAsync(auth);
        await _db.SaveChangesAsync();

        var act = () => _checker.CheckAsync(key.KeyId, "APP-CALLER", "ENCRYPT", CancellationToken.None);

        await act.Should().ThrowAsync<BusinessException>()
            .Where(e => e.Code == "KEY_PERMISSION_DENIED");
    }

    [Fact]
    public async Task Check_NonExistentKey_ShouldThrow()
    {
        var act = () => _checker.CheckAsync("KEY-NOT-EXIST", "APP-001", "ENCRYPT", CancellationToken.None);

        await act.Should().ThrowAsync<BusinessException>()
            .Where(e => e.Code == "KEY_NOT_FOUND");
    }

    [Fact]
    public async Task Check_InactiveAuthorization_ShouldThrow()
    {
        var key = TestDataFactory.CreateKey(ownerAppId: "APP-OWNER");
        var auth = TestDataFactory.CreateAuthorization(appId: "APP-CALLER", status: "REVOKED");
        await _db.Keys.AddAsync(key);
        await _db.KeyAuthorizations.AddAsync(auth);
        await _db.SaveChangesAsync();

        var act = () => _checker.CheckAsync(key.KeyId, "APP-CALLER", "ENCRYPT", CancellationToken.None);

        await act.Should().ThrowAsync<BusinessException>()
            .Where(e => e.Code == "KEY_PERMISSION_DENIED");
    }

    [Fact]
    public async Task Check_NonExpiredAuthorization_ShouldPass()
    {
        var key = TestDataFactory.CreateKey(ownerAppId: "APP-OWNER");
        var auth = TestDataFactory.CreateAuthorization(appId: "APP-CALLER", permissions: "ENCRYPT");
        auth.ExpiresAt = DateTime.UtcNow.AddHours(24); // 未过期
        await _db.Keys.AddAsync(key);
        await _db.KeyAuthorizations.AddAsync(auth);
        await _db.SaveChangesAsync();

        var act = () => _checker.CheckAsync(key.KeyId, "APP-CALLER", "ENCRYPT", CancellationToken.None);

        await act.Should().NotThrowAsync();
    }
}
