using System.Security.Cryptography;
using CryptoPlatform.Audit;
using CryptoPlatform.Authorization;
using CryptoPlatform.Crypto.Abstractions;
using CryptoPlatform.Application.Crypto;
using CryptoPlatform.Domain;
using CryptoPlatform.Domain.Entities;
using CryptoPlatform.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace CryptoPlatform.Application.Tests;

/// <summary>
/// CryptoService 单元测试。
/// 覆盖密码运算全流程：SM4/SM2/SM3/HMAC/Random + 授权校验 + 版本状态校验。
/// </summary>
public class CryptoServiceTests : IDisposable
{
    private readonly CryptoPlatformDbContext _db;
    private readonly Mock<ICryptoProviderRouter> _routerMock;
    private readonly Mock<ICryptoProvider> _providerMock;
    private readonly Mock<IKeyAuthorizationChecker> _authCheckerMock;
    private readonly Mock<IAuditService> _auditMock;
    private readonly Mock<IOperationContext> _operationContextMock;
    private readonly CryptoService _cryptoService;

    public CryptoServiceTests()
    {
        _db = TestDbContextFactory.Create();
        _routerMock = new Mock<ICryptoProviderRouter>();
        _providerMock = new Mock<ICryptoProvider>();
        _authCheckerMock = new Mock<IKeyAuthorizationChecker>();
        _auditMock = new Mock<IAuditService>();
        _operationContextMock = new Mock<IOperationContext>();

        _routerMock.Setup(r => r.GetDefaultProvider()).Returns(_providerMock.Object);
        _routerMock.Setup(r => r.ResolveProvider(It.IsAny<string>())).Returns(_providerMock.Object);
        _providerMock.Setup(p => p.ProviderType).Returns("SOFTWARE");

        _operationContextMock.Setup(o => o.AppId).Returns("APP-001");

        _authCheckerMock.Setup(a => a.CheckAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _auditMock.Setup(a => a.LogAsync(
            It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<int?>(),
            It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _cryptoService = new CryptoService(
            _db, _routerMock.Object, _authCheckerMock.Object,
            _auditMock.Object, _operationContextMock.Object,
            Mock.Of<ILogger<CryptoService>>());
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    private async Task SeedActiveKeyAsync(string keyId = "KEY-001", string keyType = "SM4", string keyUsage = "ENCRYPT")
    {
        var key = TestDataFactory.CreateKey(keyId: keyId, keyType: keyType, keyUsage: keyUsage, status: "ACTIVE");
        var version = TestDataFactory.CreateVersion(keyId: keyId, status: "ACTIVE");
        await _db.Keys.AddAsync(key);
        await _db.KeyVersions.AddAsync(version);
        await _db.SaveChangesAsync();
    }

    // ────────────────── SM4 ──────────────────

    [Fact]
    public async Task Sm4Encrypt_ShouldSucceed()
    {
        await SeedActiveKeyAsync();

        var plaintext = "Hello SM4"u8.ToArray();
        var ciphertext = new byte[32];
        Random.Shared.NextBytes(ciphertext);

        _providerMock.Setup(p => p.EncryptAsync(
            It.IsAny<string>(), It.IsAny<ReadOnlyMemory<byte>>(),
            It.IsAny<CryptoParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CryptoResult { Ciphertext = ciphertext, Nonce = new byte[16], Tag = new byte[16] });

        var req = new Sm4EncryptRequest("KEY-001", null, Convert.ToBase64String(plaintext), "GCM", "BASE64", null, null);
        var result = await _cryptoService.Sm4EncryptAsync(req, CancellationToken.None);

        result.Should().NotBeNull();
        result.KeyId.Should().Be("KEY-001");
        result.KeyVersion.Should().Be(1);
        result.Algorithm.Should().Be("SM4-GCM");
        result.Encoding.Should().Be("BASE64");
    }

    [Fact]
    public async Task Sm4Decrypt_ShouldSucceed()
    {
        await SeedActiveKeyAsync();

        var plaintext = "Decrypted text"u8.ToArray();
        _providerMock.Setup(p => p.DecryptAsync(
            It.IsAny<string>(), It.IsAny<ReadOnlyMemory<byte>>(),
            It.IsAny<CryptoParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CryptoResult { Plaintext = plaintext });

        var req = new Sm4DecryptRequest("KEY-001", null, "AAAA", "GCM", "BASE64", "AAAA", "AAAA", null);
        var result = await _cryptoService.Sm4DecryptAsync(req, CancellationToken.None);

        result.Should().NotBeNull();
        result.Plaintext.Should().Be(Convert.ToBase64String(plaintext));
    }

    // ────────────────── SM2 ──────────────────

    [Fact]
    public async Task Sm2Encrypt_ShouldSucceed()
    {
        await SeedActiveKeyAsync(keyType: "SM2", keyUsage: "ENCRYPT");

        var ciphertext = new byte[96];
        Random.Shared.NextBytes(ciphertext);
        _providerMock.Setup(p => p.EncryptAsync(
            It.IsAny<string>(), It.IsAny<ReadOnlyMemory<byte>>(),
            It.IsAny<CryptoParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CryptoResult { Ciphertext = ciphertext });

        var req = new Sm2EncryptRequest("KEY-001", null, Convert.ToBase64String("Hello SM2"u8.ToArray()), "BASE64");
        var result = await _cryptoService.Sm2EncryptAsync(req, CancellationToken.None);

        result.KeyId.Should().Be("KEY-001");
        result.Ciphertext.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Sm2SignAndVerify_ShouldSucceed()
    {
        await SeedActiveKeyAsync(keyType: "SM2", keyUsage: "SIGN");

        var signature = new byte[64];
        Random.Shared.NextBytes(signature);
        _providerMock.Setup(p => p.SignAsync(
            It.IsAny<string>(), It.IsAny<ReadOnlyMemory<byte>>(),
            It.IsAny<CryptoParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SignResult { Signature = signature, Format = "RAW" });

        var signReq = new Sm2SignRequest("KEY-001", null, Convert.ToBase64String("data"u8.ToArray()), "BASE64");
        var signResult = await _cryptoService.Sm2SignAsync(signReq, CancellationToken.None);

        signResult.Signature.Should().NotBeEmpty();

        // Verify
        _providerMock.Setup(p => p.VerifyAsync(
            It.IsAny<string>(), It.IsAny<ReadOnlyMemory<byte>>(),
            It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CryptoParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var verifyReq = new Sm2VerifyRequest("KEY-001", null,
            Convert.ToBase64String("data"u8.ToArray()), signResult.Signature, "BASE64");
        var verifyResult = await _cryptoService.Sm2VerifyAsync(verifyReq, CancellationToken.None);

        verifyResult.Valid.Should().BeTrue();
    }

    // ────────────────── SM3 ──────────────────

    [Fact]
    public async Task Sm3Hash_ShouldSucceed()
    {
        var hashBytes = new byte[32];
        Array.Fill(hashBytes, (byte)0xAB);
        _providerMock.Setup(p => p.HashAsync("SM3", It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(hashBytes);

        var req = new Sm3HashRequest(Convert.ToBase64String("abc"u8.ToArray()), "BASE64", "HEX");
        var result = await _cryptoService.Sm3HashAsync(req, CancellationToken.None);

        result.Hash.Should().NotBeEmpty();
        result.Encoding.Should().Be("HEX");
    }

    // ────────────────── HMAC-SM3 ──────────────────

    [Fact]
    public async Task HmacGenerate_ShouldSucceed()
    {
        await SeedActiveKeyAsync(keyType: "HMAC", keyUsage: "MAC");

        var hmacBytes = new byte[32];
        Random.Shared.NextBytes(hmacBytes);
        _providerMock.Setup(p => p.HmacAsync(
            It.IsAny<string>(), It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(hmacBytes);

        var req = new HmacGenerateRequest("KEY-001", null, Convert.ToBase64String("data"u8.ToArray()), "BASE64");
        var result = await _cryptoService.HmacGenerateAsync(req, CancellationToken.None);

        result.Hmac.Should().NotBeEmpty();
        result.KeyVersion.Should().Be(1);
    }

    [Fact]
    public async Task HmacVerify_CorrectMatch_ShouldReturnTrue()
    {
        await SeedActiveKeyAsync(keyType: "HMAC", keyUsage: "MAC");

        var hmacBytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
        _providerMock.Setup(p => p.HmacAsync(
            It.IsAny<string>(), It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(hmacBytes);

        var req = new HmacVerifyRequest("KEY-001", null,
            Convert.ToBase64String("data"u8.ToArray()), Convert.ToBase64String(hmacBytes), "BASE64");
        var result = await _cryptoService.HmacVerifyAsync(req, CancellationToken.None);

        result.Valid.Should().BeTrue();
    }

    [Fact]
    public async Task HmacVerify_WrongMatch_ShouldReturnFalse()
    {
        await SeedActiveKeyAsync(keyType: "HMAC", keyUsage: "MAC");

        var hmacBytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
        var wrongBytes = new byte[] { 8, 7, 6, 5, 4, 3, 2, 1 };
        _providerMock.Setup(p => p.HmacAsync(
            It.IsAny<string>(), It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(hmacBytes);

        var req = new HmacVerifyRequest("KEY-001", null,
            Convert.ToBase64String("data"u8.ToArray()), Convert.ToBase64String(wrongBytes), "BASE64");
        var result = await _cryptoService.HmacVerifyAsync(req, CancellationToken.None);

        result.Valid.Should().BeFalse();
    }

    // ────────────────── Random ──────────────────

    [Fact]
    public async Task GenerateRandom_ShouldSucceed()
    {
        var randomBytes = new byte[32];
        Random.Shared.NextBytes(randomBytes);
        _providerMock.Setup(p => p.GenerateRandomAsync(32, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RandomResult { Bytes = randomBytes });

        var req = new RandomRequest(32, "BASE64");
        var result = await _cryptoService.GenerateRandomAsync(req, CancellationToken.None);

        result.Data.Should().NotBeEmpty();
        result.Encoding.Should().Be("BASE64");
    }

    // ────────────────── Authorization & Version Status ──────────────────

    [Fact]
    public async Task Encrypt_KeyNotFound_ShouldThrow()
    {
        var req = new Sm4EncryptRequest("KEY-NOT-EXIST", null, "AAAA", "GCM", "BASE64", null, null);

        var act = () => _cryptoService.Sm4EncryptAsync(req, CancellationToken.None);

        await act.Should().ThrowAsync<BusinessException>()
            .Where(e => e.Code == "KEY_NOT_FOUND");
    }

    [Fact]
    public async Task Encrypt_AuthorizationDenied_ShouldThrow()
    {
        await SeedActiveKeyAsync();

        _authCheckerMock.Setup(a => a.CheckAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new BusinessException("KEY_PERMISSION_DENIED", "无权访问此密钥"));

        var req = new Sm4EncryptRequest("KEY-001", null, "AAAA", "GCM", "BASE64", null, null);

        var act = () => _cryptoService.Sm4EncryptAsync(req, CancellationToken.None);

        await act.Should().ThrowAsync<BusinessException>()
            .Where(e => e.Code == "KEY_PERMISSION_DENIED");
    }

    [Fact]
    public async Task Encrypt_RotatedVersion_ShouldThrow()
    {
        var key = TestDataFactory.CreateKey(keyId: "KEY-ROT", status: "ACTIVE", currentVersion: 1);
        var version = TestDataFactory.CreateVersion(keyId: "KEY-ROT", status: "ROTATED");
        await _db.Keys.AddAsync(key);
        await _db.KeyVersions.AddAsync(version);
        await _db.SaveChangesAsync();

        var req = new Sm4EncryptRequest("KEY-ROT", null, "AAAA", "GCM", "BASE64", null, null);

        var act = () => _cryptoService.Sm4EncryptAsync(req, CancellationToken.None);

        await act.Should().ThrowAsync<BusinessException>()
            .Where(e => e.Code == "KEY_VERSION_NOT_USABLE");
    }

    [Fact]
    public async Task Decrypt_RotatedVersion_ShouldSucceed()
    {
        var key = TestDataFactory.CreateKey(keyId: "KEY-ROTD", status: "ACTIVE", currentVersion: 1);
        var version = TestDataFactory.CreateVersion(keyId: "KEY-ROTD", status: "ROTATED");
        await _db.Keys.AddAsync(key);
        await _db.KeyVersions.AddAsync(version);
        await _db.SaveChangesAsync();

        var plaintext = "old data"u8.ToArray();
        _providerMock.Setup(p => p.DecryptAsync(
            It.IsAny<string>(), It.IsAny<ReadOnlyMemory<byte>>(),
            It.IsAny<CryptoParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CryptoResult { Plaintext = plaintext });

        var req = new Sm4DecryptRequest("KEY-ROTD", 1, "AAAA", "GCM", "BASE64", "AAAA", "AAAA", null);
        var result = await _cryptoService.Sm4DecryptAsync(req, CancellationToken.None);

        result.Should().NotBeNull();
    }

    // ────────────────── Encoding ──────────────────

    [Fact]
    public async Task Sm3Hash_InvalidEncoding_ShouldThrow()
    {
        var req = new Sm3HashRequest("AAAA", "INVALID_ENC", "HEX");

        var act = () => _cryptoService.Sm3HashAsync(req, CancellationToken.None);

        await act.Should().ThrowAsync<BusinessException>()
            .Where(e => e.Code == "INVALID_ENCODING");
    }
}
