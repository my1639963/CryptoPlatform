using CryptoPlatform.Authentication;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace CryptoPlatform.Phase4.Tests;

/// <summary>
/// JwtTokenGenerator 单元测试。
/// </summary>
public class JwtTokenGeneratorTests
{
    private readonly JwtTokenGenerator _sut;
    private readonly JwtSettings _settings;

    public JwtTokenGeneratorTests()
    {
        _settings = new JwtSettings
        {
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            ExpiresInSeconds = 3600,
            SigningKey = "TestSigningKey12345678901234567890" // 至少 32 字节
        };
        var logger = new Mock<ILogger<JwtTokenGenerator>>();
        _sut = new JwtTokenGenerator(_settings, logger.Object);
    }

    [Fact]
    public void Generate_ShouldReturnValidJwt()
    {
        var payload = CreateTestPayload();
        var token = _sut.Generate(payload);

        token.Should().NotBeNullOrEmpty();
        // JWT 由三部分组成，用 . 分隔
        token.Split('.').Should().HaveCount(3);
    }

    [Fact]
    public void Validate_ValidToken_ShouldReturnPayload()
    {
        var payload = CreateTestPayload();
        var token = _sut.Generate(payload);

        var result = _sut.Validate(token);

        result.Should().NotBeNull();
        result!.Subject.Should().Be(payload.Subject);
        result.OperatorId.Should().Be(payload.OperatorId);
        result.Role.Should().Be(payload.Role);
        result.Scopes.Should().Contain("key:manage");
    }

    [Fact]
    public void Validate_ExpiredToken_ShouldReturnNull()
    {
        // 使用 2 分钟前过期的令牌（超过 ClockSkew 1 分钟容差）
        var payload = CreateTestPayload(expires: DateTime.UtcNow.AddMinutes(-2));
        var token = _sut.Generate(payload);

        var result = _sut.Validate(token);

        result.Should().BeNull();
    }

    [Fact]
    public void Validate_TamperedToken_ShouldReturnNull()
    {
        var payload = CreateTestPayload();
        var token = _sut.Generate(payload);
        var tampered = token + "x";

        var result = _sut.Validate(tampered);

        result.Should().BeNull();
    }

    [Fact]
    public void Validate_WrongSigningKey_ShouldReturnNull()
    {
        var payload = CreateTestPayload();
        var token = _sut.Generate(payload);

        // 使用不同密钥创建新的验证器
        var otherSettings = new JwtSettings
        {
            Issuer = _settings.Issuer,
            Audience = _settings.Audience,
            ExpiresInSeconds = _settings.ExpiresInSeconds,
            SigningKey = "DifferentSigningKey1234567890123456"
        };
        var otherGenerator = new JwtTokenGenerator(otherSettings, new Mock<ILogger<JwtTokenGenerator>>().Object);

        var result = otherGenerator.Validate(token);
        result.Should().BeNull();
    }

    [Fact]
    public void Validate_WrongIssuer_ShouldReturnNull()
    {
        var payload = CreateTestPayload(issuer: "WrongIssuer");
        var token = _sut.Generate(payload);

        var result = _sut.Validate(token);
        result.Should().BeNull();
    }

    [Fact]
    public void Generate_DifferentPayloads_ShouldProduceDifferentTokens()
    {
        var payload1 = CreateTestPayload(subject: "user1");
        var payload2 = CreateTestPayload(subject: "user2");

        var token1 = _sut.Generate(payload1);
        var token2 = _sut.Generate(payload2);

        token1.Should().NotBe(token2);
    }

    private TokenPayload CreateTestPayload(
        string? subject = null,
        string? issuer = null,
        DateTime? expires = null)
    {
        return new TokenPayload(
            Jti: Guid.NewGuid().ToString("N"),
            Issuer: issuer ?? _settings.Issuer,
            Subject: subject ?? "12345",
            Audience: _settings.Audience,
            OperatorId: subject ?? "12345",
            Role: "SYSTEM_ADMIN",
            Scopes: new[] { "key:manage", "app:manage" },
            Expires: expires ?? DateTime.UtcNow.AddSeconds(_settings.ExpiresInSeconds));
    }
}
