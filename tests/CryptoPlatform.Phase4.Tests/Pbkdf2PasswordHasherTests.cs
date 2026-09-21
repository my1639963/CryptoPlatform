using CryptoPlatform.Authentication.PasswordHashing;
using FluentAssertions;

namespace CryptoPlatform.Phase4.Tests;

/// <summary>
/// Pbkdf2PasswordHasher 单元测试。
/// </summary>
public class Pbkdf2PasswordHasherTests
{
    private readonly Pbkdf2PasswordHasher _hasher = new();

    [Fact]
    public void HashPassword_ShouldReturnCorrectFormat()
    {
        var result = _hasher.HashPassword("TestPassword123");

        // 格式：PBKDF2-SHA256$1${base64salt}${iterations}${base64hash}
        var parts = result.Split('$');
        parts.Should().HaveCount(5);
        parts[0].Should().Be("PBKDF2-SHA256");
        parts[1].Should().Be("1");
        // salt 为 16 字节 Base64
        Convert.FromBase64String(parts[2]).Should().HaveCount(16);
        int.Parse(parts[3]).Should().Be(600_000);
        // hash 为 32 字节 Base64
        Convert.FromBase64String(parts[4]).Should().HaveCount(32);
    }

    [Fact]
    public void HashPassword_TwoCalls_ShouldProduceDifferentHashes()
    {
        var hash1 = _hasher.HashPassword("SamePassword");
        var hash2 = _hasher.HashPassword("SamePassword");

        hash1.Should().NotBe(hash2); // 不同 Salt，结果不同
    }

    [Fact]
    public void VerifyPassword_CorrectPassword_ShouldReturnTrue()
    {
        var password = "MySecretPassword!@#";
        var hash = _hasher.HashPassword(password);

        var result = _hasher.VerifyPassword(password, hash);

        result.Should().BeTrue();
    }

    [Fact]
    public void VerifyPassword_WrongPassword_ShouldReturnFalse()
    {
        var hash = _hasher.HashPassword("CorrectPassword");

        var result = _hasher.VerifyPassword("WrongPassword", hash);

        result.Should().BeFalse();
    }

    [Fact]
    public void VerifyPassword_InvalidFormat_ShouldReturnFalse()
    {
        var result = _hasher.VerifyPassword("password", "invalid-format");

        result.Should().BeFalse();
    }

    [Fact]
    public void VerifyPassword_DifferentAlgorithm_ShouldReturnFalse()
    {
        // 构造一个算法标识不同的哈希字符串
        var fakeHash = "PBKDF2-SHA512$1$YWJjZGVmZ2hpamtsbW5vcA$600000$YWJjZGVmZ2hpamtsbW5vcHFyc3R1dnd4eXoxMjM0NTY=";

        var result = _hasher.VerifyPassword("password", fakeHash);

        result.Should().BeFalse();
    }

    [Fact]
    public void NeedsUpgrade_CurrentVersion_ShouldReturnFalse()
    {
        var hash = _hasher.HashPassword("password");

        var result = _hasher.NeedsUpgrade(hash);

        result.Should().BeFalse();
    }

    [Fact]
    public void NeedsUpgrade_OldAlgorithm_ShouldReturnTrue()
    {
        var oldFormatHash = "LEGACY-SM3$1$YWJjZGVmZ2hpamtsbW5vcA$100000$YWJjZGVmZ2hpamtsbW5vcHFyc3R1dnd4eXoxMjM0NTY=";

        var result = _hasher.NeedsUpgrade(oldFormatHash);

        result.Should().BeTrue();
    }

    [Fact]
    public void NeedsUpgrade_InvalidFormat_ShouldReturnTrue()
    {
        var result = _hasher.NeedsUpgrade("garbage");

        result.Should().BeTrue();
    }

    [Fact]
    public void NeedsUpgrade_LowerIterations_ShouldReturnTrue()
    {
        // 使用较低迭代次数（100000 < 600000）
        var hashWithLowIterations = "PBKDF2-SHA256$1$YWJjZGVmZ2hpamtsbW5vcA$100000$YWJjZGVmZ2hpamtsbW5vcHFyc3R1dnd4eXoxMjM0NTY=";

        var result = _hasher.NeedsUpgrade(hashWithLowIterations);

        result.Should().BeTrue();
    }

    [Fact]
    public void Algorithm_ShouldReturnPBKDF2SHA256()
    {
        _hasher.Algorithm.Should().Be("PBKDF2-SHA256");
    }

    [Fact]
    public void Version_ShouldReturn1()
    {
        _hasher.Version.Should().Be(1);
    }
}
