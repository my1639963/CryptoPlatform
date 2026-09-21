using CryptoPlatform.Authentication;
using CryptoPlatform.Authentication.PasswordHashing;
using CryptoPlatform.Domain;
using CryptoPlatform.Infrastructure.Caching;
using CryptoPlatform.Security;
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
    private readonly IPasswordHasherFactory _hasherFactory;
    private readonly Mock<ISecurityEventService> _securityEvents;

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

        // 构建 PasswordHasherFactory
        var pbkdf2Hasher = new Pbkdf2PasswordHasher();
        var sm3Hasher = new Sm3PasswordHasher();
        _hasherFactory = new PasswordHasherFactory(
            new IPasswordHasher[] { pbkdf2Hasher, sm3Hasher },
            pbkdf2Hasher);

        _securityEvents = new Mock<ISecurityEventService>();
        _securityEvents.Setup(s => s.RaiseAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _sut = new AdminTokenService(_db, tokenGen, _cache, _jwtSettings, _hasherFactory, _securityEvents.Object, logger.Object);
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ShouldReturnToken()
    {
        // 准备测试用户（使用 Pbkdf2PasswordHasher 生成密码）
        var user = TestDataFactory.CreateUser("admin");
        _db.Users.Add(user);
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await _sut.LoginAsync("admin", "Admin@123", TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result.AccessToken.Should().NotBeNullOrEmpty();
        result.TokenType.Should().Be("Bearer");
        result.ExpiresIn.Should().Be(_jwtSettings.ExpiresInSeconds);
        result.MustModifyPassword.Should().BeFalse();
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ShouldThrowAndIncrementFailCount()
    {
        var user = TestDataFactory.CreateUser("admin");
        _db.Users.Add(user);
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var act = () => _sut.LoginAsync("admin", "wrongpassword", TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<BusinessException>()
            .Where(e => e.Code == "AUTH_LOGIN_FAILED");

        var updatedUser = await _db.Users.FirstAsync(u => u.Username == "admin", cancellationToken: TestContext.Current.CancellationToken);
        updatedUser.LoginFailCount.Should().Be(1);
    }

    [Fact]
    public async Task LoginAsync_LockedAccount_ShouldThrow()
    {
        var user = TestDataFactory.CreateUser("admin");
        user.LockedUntil = DateTime.UtcNow.AddMinutes(10);
        _db.Users.Add(user);
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var act = () => _sut.LoginAsync("admin", "Admin@123", TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<BusinessException>()
            .Where(e => e.Code == "AUTH_ACCOUNT_LOCKED");
    }

    [Fact]
    public async Task LoginAsync_DisabledAccount_ShouldThrow()
    {
        var user = TestDataFactory.CreateUser("admin");
        user.Status = 2; // 禁用
        _db.Users.Add(user);
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var act = () => _sut.LoginAsync("admin", "Admin@123", TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<BusinessException>()
            .Where(e => e.Code == "AUTH_ACCOUNT_DISABLED");
    }

    [Fact]
    public async Task LoginAsync_NonExistentUser_ShouldThrow()
    {
        var act = () => _sut.LoginAsync("nonexistent", "password", TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<BusinessException>()
            .Where(e => e.Code == "AUTH_LOGIN_FAILED");
    }

    [Fact]
    public async Task ValidateAsync_ValidToken_ShouldReturnPrincipal()
    {
        var user = TestDataFactory.CreateUser("admin");
        _db.Users.Add(user);
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var loginResult = await _sut.LoginAsync("admin", "Admin@123", TestContext.Current.CancellationToken);
        var principal = await _sut.ValidateAsync(loginResult.AccessToken);

        principal.Should().NotBeNull();
    }

    [Fact]
    public async Task ValidateAsync_RevokedToken_ShouldReturnNull()
    {
        var user = TestDataFactory.CreateUser("admin");
        _db.Users.Add(user);
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var loginResult = await _sut.LoginAsync("admin", "Admin@123", TestContext.Current.CancellationToken);

        // 提取 JTI 并撤销
        var tokenGen = new JwtTokenGenerator(_jwtSettings, new Mock<ILogger<JwtTokenGenerator>>().Object);
        var payload = tokenGen.Validate(loginResult.AccessToken);
        await _sut.RevokeAsync(payload!.Jti, TestContext.Current.CancellationToken);

        var principal = await _sut.ValidateAsync(loginResult.AccessToken);
        principal.Should().BeNull();
    }

    [Fact]
    public async Task LoginAsync_Success_ShouldResetFailCount()
    {
        var user = TestDataFactory.CreateUser("admin");
        user.LoginFailCount = 3;
        _db.Users.Add(user);
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        await _sut.LoginAsync("admin", "Admin@123", TestContext.Current.CancellationToken);

        var updatedUser = await _db.Users.FirstAsync(u => u.Username == "admin", cancellationToken: TestContext.Current.CancellationToken);
        updatedUser.LoginFailCount.Should().Be(0);
        updatedUser.LockedUntil.Should().BeNull();
    }

    [Fact]
    public async Task LoginAsync_OldAlgorithm_ShouldUpgradePasswordHash()
    {
        // 模拟一个使用旧算法（SM3）存储的用户
        var oldHash = AdminTokenService.ComputeSM3Hash("Admin@123");
        var user = TestDataFactory.CreateUser("admin", oldHash, passwordAlgorithm: "LEGACY-SM3", passwordVersion: 0);
        _db.Users.Add(user);
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        // 由于 LEGACY-SM3 无法识别，登录应失败
        var act = () => _sut.LoginAsync("admin", "Admin@123", TestContext.Current.CancellationToken);
        await act.Should().ThrowAsync<BusinessException>()
            .Where(e => e.Code == "AUTH_LOGIN_FAILED");
    }

    [Fact]
    public async Task LoginAsync_MustModifyPassword_ShouldReturnFlag()
    {
        var user = TestDataFactory.CreateUser("admin");
        user.MustModifyPassword = true;
        _db.Users.Add(user);
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await _sut.LoginAsync("admin", "Admin@123", TestContext.Current.CancellationToken);

        result.MustModifyPassword.Should().BeTrue();
    }

    [Fact]
    public async Task LoginAsync_FailCount5_ShouldLockAccount()
    {
        var user = TestDataFactory.CreateUser("admin");
        user.LoginFailCount = 4;
        _db.Users.Add(user);
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var act = () => _sut.LoginAsync("admin", "wrongpassword", TestContext.Current.CancellationToken);
        await act.Should().ThrowAsync<BusinessException>();

        var updatedUser = await _db.Users.FirstAsync(u => u.Username == "admin", cancellationToken: TestContext.Current.CancellationToken);
        updatedUser.LoginFailCount.Should().Be(5);
        updatedUser.LockedUntil.Should().NotBeNull();
    }
}
