using System.Text;
using CryptoPlatform.Authentication;
using CryptoPlatform.Infrastructure.Caching;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Macs;
using Org.BouncyCastle.Crypto.Parameters;

namespace CryptoPlatform.Phase4.Tests;

/// <summary>
/// AppAuthenticationService 单元测试。
/// </summary>
public class AppAuthenticationServiceTests
{
    private readonly AppAuthenticationService _sut;
    private readonly CryptoPlatform.Persistence.CryptoPlatformDbContext _db;
    private readonly ICacheService _cache;

    public AppAuthenticationServiceTests()
    {
        _db = TestDbContextFactory.Create();
        _cache = TestCacheFactory.Create();
        var logger = new Mock<ILogger<AppAuthenticationService>>();
        _sut = new AppAuthenticationService(_db, _cache, logger.Object);
    }

    [Fact]
    public async Task AuthenticateAsync_ValidSignature_ShouldSucceed()
    {
        // 准备测试数据
        var app = TestDataFactory.CreateApplication("TEST_APP");
        _db.Applications.Add(app);
        await _db.SaveChangesAsync();

        var secretHash = "my-secret-hash-key";
        var secret = TestDataFactory.CreateSecret(app.Id, secretHash);
        _db.ApplicationSecrets.Add(secret);
        await _db.SaveChangesAsync();

        // 构造签名参数
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var nonce = Guid.NewGuid().ToString("N");
        var httpMethod = "POST";
        var path = "/api/v1/keys";
        var body = "{\"name\":\"test\"}";
        var bodyHash = ComputeSM3(body);
        var stringToSign = $"TEST_APP|{timestamp}|{nonce}|{httpMethod}|{path}|{bodyHash}";
        var signature = ComputeHmacSm3(secretHash, stringToSign);

        var result = await _sut.AuthenticateAsync(
            "TEST_APP", timestamp, nonce, signature, httpMethod, path, body, CancellationToken.None);

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task AuthenticateAsync_InvalidSignature_ShouldFail()
    {
        var app = TestDataFactory.CreateApplication("TEST_APP");
        _db.Applications.Add(app);
        await _db.SaveChangesAsync();

        var secret = TestDataFactory.CreateSecret(app.Id, "my-secret-hash");
        _db.ApplicationSecrets.Add(secret);
        await _db.SaveChangesAsync();

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var nonce = Guid.NewGuid().ToString("N");

        var result = await _sut.AuthenticateAsync(
            "TEST_APP", timestamp, nonce, "INVALID_SIGNATURE", "GET", "/api/v1/keys", null, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("AUTH_SIGNATURE_INVALID");
    }

    [Fact]
    public async Task AuthenticateAsync_ExpiredTimestamp_ShouldFail()
    {
        var app = TestDataFactory.CreateApplication("TEST_APP");
        _db.Applications.Add(app);
        await _db.SaveChangesAsync();

        var secret = TestDataFactory.CreateSecret(app.Id, "my-secret-hash");
        _db.ApplicationSecrets.Add(secret);
        await _db.SaveChangesAsync();

        // 使用 10 分钟前的时间戳（超过 5 分钟限制）
        var timestamp = DateTimeOffset.UtcNow.AddSeconds(-600).ToUnixTimeSeconds().ToString();
        var nonce = Guid.NewGuid().ToString("N");

        var result = await _sut.AuthenticateAsync(
            "TEST_APP", timestamp, nonce, "any-signature", "GET", "/api/v1/keys", null, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("AUTH_TIMESTAMP_EXPIRED");
    }

    [Fact]
    public async Task AuthenticateAsync_NonceReplay_ShouldFail()
    {
        var app = TestDataFactory.CreateApplication("TEST_APP");
        _db.Applications.Add(app);
        await _db.SaveChangesAsync();

        var secretHash = "my-secret-hash";
        var secret = TestDataFactory.CreateSecret(app.Id, secretHash);
        _db.ApplicationSecrets.Add(secret);
        await _db.SaveChangesAsync();

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var nonce = Guid.NewGuid().ToString("N");
        var path = "/api/v1/keys";
        var stringToSign = $"TEST_APP|{timestamp}|{nonce}|GET|{path}|";
        var signature = ComputeHmacSm3(secretHash, stringToSign);

        // 第一次认证应成功
        var result1 = await _sut.AuthenticateAsync(
            "TEST_APP", timestamp, nonce, signature, "GET", path, null, CancellationToken.None);
        result1.Success.Should().BeTrue();

        // 第二次使用相同 Nonce 应失败（重放）
        var result2 = await _sut.AuthenticateAsync(
            "TEST_APP", timestamp, nonce, signature, "GET", path, null, CancellationToken.None);
        result2.Success.Should().BeFalse();
        result2.ErrorCode.Should().Be("NONCE_REPLAYED");
    }

    [Fact]
    public async Task AuthenticateAsync_AppNotFound_ShouldFail()
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var result = await _sut.AuthenticateAsync(
            "NON_EXISTENT_APP", timestamp, "nonce", "sig", "GET", "/api", null, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("AUTH_APP_NOT_FOUND");
    }

    [Fact]
    public async Task AuthenticateAsync_NoValidSecret_ShouldFail()
    {
        var app = TestDataFactory.CreateApplication("TEST_APP");
        _db.Applications.Add(app);
        await _db.SaveChangesAsync();

        // 添加一个已撤销的凭据
        var secret = TestDataFactory.CreateSecret(app.Id, "hash");
        secret.RevokedAt = DateTime.UtcNow.AddMinutes(-1);
        _db.ApplicationSecrets.Add(secret);
        await _db.SaveChangesAsync();

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var nonce = Guid.NewGuid().ToString("N");

        var result = await _sut.AuthenticateAsync(
            "TEST_APP", timestamp, nonce, "sig", "GET", "/api", null, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("AUTH_NO_VALID_SECRET");
    }

    [Fact]
    public async Task AuthenticateAsync_InvalidTimestampFormat_ShouldFail()
    {
        var result = await _sut.AuthenticateAsync(
            "APP", "not-a-number", "nonce", "sig", "GET", "/api", null, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("AUTH_INVALID_TIMESTAMP");
    }

    private static string ComputeSM3(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var digest = new SM3Digest();
        digest.BlockUpdate(bytes, 0, bytes.Length);
        var hash = new byte[digest.GetDigestSize()];
        digest.DoFinal(hash, 0);
        return Convert.ToHexString(hash);
    }

    private static string ComputeHmacSm3(string key, string message)
    {
        var keyBytes = Encoding.UTF8.GetBytes(key);
        var msgBytes = Encoding.UTF8.GetBytes(message);
        var hmac = new HMac(new SM3Digest());
        hmac.Init(new KeyParameter(keyBytes));
        hmac.BlockUpdate(msgBytes, 0, msgBytes.Length);
        var result = new byte[hmac.GetMacSize()];
        hmac.DoFinal(result, 0);
        return Convert.ToHexString(result);
    }
}
