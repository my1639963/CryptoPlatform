using CryptoPlatform.Authentication;
using CryptoPlatform.Domain;
using CryptoPlatform.Infrastructure.Caching;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace CryptoPlatform.Phase4.Tests;

/// <summary>
/// AdminTokenService 单元测试。
/// </summary>
public class AdminTokenServiceTests
{
    private readonly AdminTokenService _sut;
    private readonly CryptoPlatform.Persistence.CryptoPlatformDbContext _db;
    private readonly JwtSettings _jwtSettings;
    private readonly ICacheService _cache;

    public AdminTokenServiceTests()
    {
        _db = TestDbContextFactory.Create();
        _cache = TestCacheFactory.Create();
        _jwtSettings = new JwtSettings
        {
            Issuer = "TestIssuer",
            Audience = "management-api",
            ExpiresInSeconds = 3600,
            SigningKey = "TestSigningKey12345678901234567890"
        };

        var tokenGen = new JwtTokenGenerator(_jwtSettings, new Mock<ILogger<JwtTokenGenerator>>().Object);
        var logger = new Mock<ILogger<AdminTokenService>>();
        _sut = new AdminTokenService(_db, tokenGen, _cache, _jwtSettings, logger.Object);
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ShouldReturnToken()
    {
        // 准备测试用户
        var user = TestDataFactory.CreateUser("admin", AdminTokenService.ComputeSM3Hash("password123"));
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var result = await _sut.LoginAsync("admin", "password123", CancellationToken.None);

        result.Should().NotBeNull();
        result.AccessToken.Should().NotBeNullOrEmpty();
        result.TokenType.Should().Be("Bearer");
        result.ExpiresIn.Should().Be(_jwtSettings.ExpiresInSeconds);
        result.Role.Should().Be("SYSTEM_ADMIN");
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ShouldThrowAndIncrementFailCount()
    {
        var user = TestDataFactory.CreateUser("admin", AdminTokenService.ComputeSM3Hash("password123"));
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var act = () => _sut.LoginAsync("admin", "wrongpassword", CancellationToken.None);

        await act.Should().ThrowAsync<BusinessException>()
            .Where(e => e.Code == "AUTH_LOGIN_FAILED");

        var updatedUser = await _db.Users.FirstAsync(u => u.Username == "admin");
        updatedUser.LoginFailCount.Should().Be(1);
    }

    [Fact]
    public async Task LoginAsync_LockedAccount_ShouldThrow()
    {
        var user = TestDataFactory.CreateUser("admin", AdminTokenService.ComputeSM3Hash("password123"));
        user.LockedUntil = DateTime.UtcNow.AddMinutes(10);
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var act = () => _sut.LoginAsync("admin", "password123", CancellationToken.None);

        await act.Should().ThrowAsync<BusinessException>()
            .Where(e => e.Code == "AUTH_ACCOUNT_LOCKED");
    }

    [Fact]
    public async Task LoginAsync_DisabledAccount_ShouldThrow()
    {
        var user = TestDataFactory.CreateUser("admin", AdminTokenService.ComputeSM3Hash("password123"));
        user.Status = 2; // 禁用
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var act = () => _sut.LoginAsync("admin", "password123", CancellationToken.None);

        await act.Should().ThrowAsync<BusinessException>()
            .Where(e => e.Code == "AUTH_ACCOUNT_DISABLED");
    }

    [Fact]
    public async Task LoginAsync_NonExistentUser_ShouldThrow()
    {
        var act = () => _sut.LoginAsync("nonexistent", "password", CancellationToken.None);

        await act.Should().ThrowAsync<BusinessException>()
            .Where(e => e.Code == "AUTH_LOGIN_FAILED");
    }

    [Fact]
    public async Task ValidateAsync_ValidToken_ShouldReturnPrincipal()
    {
        var user = TestDataFactory.CreateUser("admin", AdminTokenService.ComputeSM3Hash("password123"));
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var loginResult = await _sut.LoginAsync("admin", "password123", CancellationToken.None);
        var principal = await _sut.ValidateAsync(loginResult.AccessToken);

        principal.Should().NotBeNull();
        principal!.Role.Should().Be("SYSTEM_ADMIN");
    }

    [Fact]
    public async Task ValidateAsync_RevokedToken_ShouldReturnNull()
    {
        var user = TestDataFactory.CreateUser("admin", AdminTokenService.ComputeSM3Hash("password123"));
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var loginResult = await _sut.LoginAsync("admin", "password123", CancellationToken.None);

        // 提取 JTI 并撤销
        var tokenGen = new JwtTokenGenerator(_jwtSettings, new Mock<ILogger<JwtTokenGenerator>>().Object);
        var payload = tokenGen.Validate(loginResult.AccessToken);
        await _sut.RevokeAsync(payload!.Jti, CancellationToken.None);

        var principal = await _sut.ValidateAsync(loginResult.AccessToken);
        principal.Should().BeNull();
    }

    [Fact]
    public async Task LoginAsync_Success_ShouldResetFailCount()
    {
        var user = TestDataFactory.CreateUser("admin", AdminTokenService.ComputeSM3Hash("password123"));
        user.LoginFailCount = 3;
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        await _sut.LoginAsync("admin", "password123", CancellationToken.None);

        var updatedUser = await _db.Users.FirstAsync(u => u.Username == "admin");
        updatedUser.LoginFailCount.Should().Be(0);
        updatedUser.LockedUntil.Should().BeNull();
    }
}
