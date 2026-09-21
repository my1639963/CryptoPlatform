using CryptoPlatform.Crypto.Abstractions;
using CryptoPlatform.Crypto.Software;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;

namespace CryptoPlatform.Crypto.Software.Tests;

public class SoftwareCryptoProviderTests
{
    private readonly SoftwareCryptoProvider _provider = new(NullLogger<SoftwareCryptoProvider>.Instance);

    // ══════════════════════════════════════════════
    // SM3 杂凑 — 国密标准测试向量 (GB/T 32905-2016)
    // ══════════════════════════════════════════════

    [Fact]
    public async Task SM3_StandardTestVector_abc()
    {
        var data = "abc"u8.ToArray();
        var hash = await _provider.HashAsync("SM3", data, default);

        Convert.ToHexString(hash).ToLowerInvariant()
            .Should().Be("66c7f0f462eeedd9d1f2d46bdc10e4e24167c4875cf2f7a2297da02b8f4ba8e0");
    }

    [Fact]
    public async Task SM3_StandardTestVector_64Bytes()
    {
        // GB/T 32905-2016: "abcd" × 16 = 64 bytes
        var input = System.Text.Encoding.ASCII.GetBytes("abcdabcdabcdabcdabcdabcdabcdabcdabcdabcdabcdabcdabcdabcdabcdabcd");
        input.Should().HaveCount(64);

        var hash = await _provider.HashAsync("SM3", input, default);

        Convert.ToHexString(hash).ToLowerInvariant()
            .Should().Be("debe9ff92275b8a138604889c18e5a4d6fdb70e5387e5765293dcba39c0c5732");
    }

    [Fact]
    public async Task SM3_EmptyData_ProducesHash()
    {
        var hash = await _provider.HashAsync("SM3", Array.Empty<byte>(), default);
        hash.Should().HaveCount(32); // SM3 输出 256 位 = 32 字节
    }

    [Fact]
    public async Task SM3_LargeData_Over1MB()
    {
        var data = new byte[1024 * 1024 + 7]; // 1MB + 7 bytes
        new Random(42).NextBytes(data);

        var hash = await _provider.HashAsync("SM3", data, default);
        hash.Should().HaveCount(32);
    }

    [Fact]
    public async Task SM3_UnsupportedAlgorithm_Throws()
    {
        var act = () => _provider.HashAsync("SHA256", Array.Empty<byte>(), default);
        await act.Should().ThrowAsync<CryptoProviderException>()
            .Where(e => e.ErrorCode == ProviderErrorCodes.PROVIDER_CAPABILITY_NOT_SUPPORTED);
    }

    // ══════════════════════════════════════════════
    // SM4 对称加密 — 国密标准测试向量 (GB/T 32907-2016)
    // ══════════════════════════════════════════════

    [Fact]
    public void SM4_ECB_Standard测试向量_BouncyCastle直接验证()
    {
        // GB/T 32907-2016 标准测试向量
        var key = Convert.FromHexString("0123456789ABCDEFFEDCBA9876543210");
        var plaintext = Convert.FromHexString("0123456789ABCDEFFEDCBA9876543210");
        var expected = "681EDF34D206965E86B3E94F536E4246";

        var engine = new SM4Engine();
        engine.Init(true, new KeyParameter(key));
        var output = new byte[engine.GetBlockSize()];
        engine.ProcessBlock(plaintext, 0, output, 0);

        Convert.ToHexString(output).ToUpperInvariant()
            .Should().Be(expected);
    }

    [Fact]
    public async Task SM4_ECB_Roundtrip_WithPadding()
    {
        var keyRef = await GenerateSm4Key();
        var plaintext = "Hello, 国密 SM4!"u8.ToArray();

        var enc = await _provider.EncryptAsync(keyRef, plaintext,
            new CryptoParameters { Mode = "ECB", Padding = "PKCS7" }, default);

        var dec = await _provider.DecryptAsync(keyRef, enc.Ciphertext,
            new CryptoParameters { Mode = "ECB", Padding = "PKCS7" }, default);

        dec.Plaintext.Should().Equal(plaintext);
    }

    [Fact]
    public async Task SM4_CBC_Roundtrip()
    {
        var keyRef = await GenerateSm4Key();
        var plaintext = "CBC mode test data, 1234567890!"u8.ToArray();

        var enc = await _provider.EncryptAsync(keyRef, plaintext,
            new CryptoParameters { Mode = "CBC", Padding = "PKCS7" }, default);

        enc.Nonce.Should().NotBeNull().And.HaveCount(16);

        var dec = await _provider.DecryptAsync(keyRef, enc.Ciphertext,
            new CryptoParameters { Mode = "CBC", Nonce = enc.Nonce, Padding = "PKCS7" }, default);

        dec.Plaintext.Should().Equal(plaintext);
    }

    [Fact]
    public async Task SM4_CTR_Roundtrip()
    {
        var keyRef = await GenerateSm4Key();
        var plaintext = "CTR mode test - variable length!"u8.ToArray();

        var enc = await _provider.EncryptAsync(keyRef, plaintext,
            new CryptoParameters { Mode = "CTR" }, default);

        enc.Nonce.Should().NotBeNull().And.HaveCount(16);

        var dec = await _provider.DecryptAsync(keyRef, enc.Ciphertext,
            new CryptoParameters { Mode = "CTR", Nonce = enc.Nonce }, default);

        dec.Plaintext.Should().Equal(plaintext);
    }

    [Fact]
    public async Task SM4_GCM_Roundtrip()
    {
        var keyRef = await GenerateSm4Key();
        var plaintext = "GCM authenticated encryption!"u8.ToArray();
        var aad = "additional-data"u8.ToArray();

        var enc = await _provider.EncryptAsync(keyRef, plaintext,
            new CryptoParameters { Mode = "GCM", Aad = aad }, default);

        enc.Nonce.Should().NotBeNull().And.HaveCount(12);
        enc.Tag.Should().NotBeNull().And.HaveCount(16);

        var dec = await _provider.DecryptAsync(keyRef, enc.Ciphertext,
            new CryptoParameters { Mode = "GCM", Nonce = enc.Nonce, Aad = aad, Tag = enc.Tag }, default);

        dec.Plaintext.Should().Equal(plaintext);
    }

    [Fact]
    public async Task SM4_GCM_TamperedTag_Throws()
    {
        var keyRef = await GenerateSm4Key();
        var plaintext = "sensitive data"u8.ToArray();

        var enc = await _provider.EncryptAsync(keyRef, plaintext,
            new CryptoParameters { Mode = "GCM" }, default);

        // 篡改 Tag
        var badTag = (byte[])enc.Tag!.Clone();
        badTag[0] ^= 0xFF;

        var act = () => _provider.DecryptAsync(keyRef, enc.Ciphertext,
            new CryptoParameters { Mode = "GCM", Nonce = enc.Nonce, Tag = badTag }, default);

        await act.Should().ThrowAsync<Exception>();
    }

    // ══════════════════════════════════════════════
    // SM2 非对称加密
    // ══════════════════════════════════════════════

    [Fact]
    public async Task SM2_EncryptDecrypt_Roundtrip_C1C2C3()
    {
        var kp = await _provider.GenerateKeyPairAsync(KeyPairAlgorithm.SM2, default);
        var plaintext = "SM2 encryption test"u8.ToArray();

        var enc = await _provider.EncryptAsync(kp.ProviderKeyRef, plaintext,
            new CryptoParameters { CipherFormat = "C1C2C3" }, default);

        var dec = await _provider.DecryptAsync(kp.ProviderKeyRef, enc.Ciphertext,
            new CryptoParameters { CipherFormat = "C1C2C3" }, default);

        dec.Plaintext.Should().Equal(plaintext);
    }

    [Fact]
    public async Task SM2_EncryptDecrypt_Roundtrip_C1C3C2()
    {
        var kp = await _provider.GenerateKeyPairAsync(KeyPairAlgorithm.SM2, default);
        var plaintext = "SM2 C1C3C2 format test"u8.ToArray();

        var enc = await _provider.EncryptAsync(kp.ProviderKeyRef, plaintext,
            new CryptoParameters { CipherFormat = "C1C3C2" }, default);

        var dec = await _provider.DecryptAsync(kp.ProviderKeyRef, enc.Ciphertext,
            new CryptoParameters { CipherFormat = "C1C3C2" }, default);

        dec.Plaintext.Should().Equal(plaintext);
    }

    // ══════════════════════════════════════════════
    // SM2 签名/验签
    // ══════════════════════════════════════════════

    [Fact]
    public async Task SM2_SignVerify_DER()
    {
        var kp = await _provider.GenerateKeyPairAsync(KeyPairAlgorithm.SM2, default);
        var data = "Sign this message"u8.ToArray();

        var sig = await _provider.SignAsync(kp.ProviderKeyRef, data,
            new CryptoParameters { SignatureFormat = "DER" }, default);

        sig.Format.Should().Be("DER");
        sig.Signature.Should().NotBeEmpty();

        var valid = await _provider.VerifyAsync(kp.ProviderKeyRef, data, sig.Signature,
            new CryptoParameters { SignatureFormat = "DER" }, default);

        valid.Should().BeTrue();
    }

    [Fact]
    public async Task SM2_SignVerify_RAW()
    {
        var kp = await _provider.GenerateKeyPairAsync(KeyPairAlgorithm.SM2, default);
        var data = "RAW signature test"u8.ToArray();

        var sig = await _provider.SignAsync(kp.ProviderKeyRef, data,
            new CryptoParameters { SignatureFormat = "RAW" }, default);

        sig.Format.Should().Be("RAW");
        sig.Signature.Should().HaveCount(64); // R(32) + S(32)

        var valid = await _provider.VerifyAsync(kp.ProviderKeyRef, data, sig.Signature,
            new CryptoParameters { SignatureFormat = "RAW" }, default);

        valid.Should().BeTrue();
    }

    [Fact]
    public async Task SM2_Verify_TamperedData_ReturnsFalse()
    {
        var kp = await _provider.GenerateKeyPairAsync(KeyPairAlgorithm.SM2, default);
        var data = "original message"u8.ToArray();

        var sig = await _provider.SignAsync(kp.ProviderKeyRef, data,
            new CryptoParameters { SignatureFormat = "DER" }, default);

        var tampered = "tampered message"u8.ToArray();
        var valid = await _provider.VerifyAsync(kp.ProviderKeyRef, tampered, sig.Signature,
            new CryptoParameters { SignatureFormat = "DER" }, default);

        valid.Should().BeFalse();
    }

    // ══════════════════════════════════════════════
    // HMAC-SM3
    // ══════════════════════════════════════════════

    [Fact]
    public async Task HMAC_SM3_Roundtrip()
    {
        var keyRef = await GenerateHmacKey();
        var data = "HMAC-SM3 test data"u8.ToArray();

        var mac1 = await _provider.HmacAsync(keyRef, data, default);
        var mac2 = await _provider.HmacAsync(keyRef, data, default);

        mac1.Should().HaveCount(32); // SM3 输出 32 字节
        mac1.Should().Equal(mac2);   // 相同输入 → 相同 MAC
    }

    [Fact]
    public async Task HMAC_SM3_DifferentKeys_DifferentMacs()
    {
        var key1 = await GenerateHmacKey();
        var key2 = await GenerateHmacKey();
        var data = "same data"u8.ToArray();

        var mac1 = await _provider.HmacAsync(key1, data, default);
        var mac2 = await _provider.HmacAsync(key2, data, default);

        mac1.Should().NotEqual(mac2);
    }

    // ══════════════════════════════════════════════
    // 随机数
    // ══════════════════════════════════════════════

    [Fact]
    public async Task Random_CorrectLength()
    {
        var result = await _provider.GenerateRandomAsync(32, default);
        result.Bytes.Should().HaveCount(32);
    }

    [Fact]
    public async Task Random_TwoCalls_Different()
    {
        var r1 = await _provider.GenerateRandomAsync(32, default);
        var r2 = await _provider.GenerateRandomAsync(32, default);
        r1.Bytes.Should().NotEqual(r2.Bytes);
    }

    // ══════════════════════════════════════════════
    // 密钥管理
    // ══════════════════════════════════════════════

    [Fact]
    public async Task GenerateKey_SM4_ReturnsValidResult()
    {
        var result = await _provider.GenerateKeyAsync(KeyAlgorithm.SM4_128, default);

        result.ProviderKeyRef.Should().StartWith("SOFTWARE:");
        result.Fingerprint.Should().NotBeEmpty();
        result.EncryptedKeyMaterial.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GenerateKeyPair_SM2_ReturnsValidResult()
    {
        var result = await _provider.GenerateKeyPairAsync(KeyPairAlgorithm.SM2, default);

        result.ProviderKeyRef.Should().StartWith("SOFTWARE:");
        result.PublicKeyMaterial.Should().NotBeEmpty();
        result.Fingerprint.Should().NotBeEmpty();
        result.EncryptedKeyMaterial.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetKeyInfo_ExistingKey_ReturnsExists()
    {
        var key = await GenerateSm4Key();
        var info = await _provider.GetKeyInfoAsync(key, default);

        info.Exists.Should().BeTrue();
        info.Algorithm.Should().Be("SM4_128");
    }

    [Fact]
    public async Task GetKeyInfo_NonExistingKey_ReturnsNotExists()
    {
        var info = await _provider.GetKeyInfoAsync("SOFTWARE:nonexistent:1", default);
        info.Exists.Should().BeFalse();
    }

    [Fact]
    public async Task DestroyKey_RemovesKey()
    {
        var keyRef = await GenerateSm4Key();
        await _provider.DestroyKeyAsync(keyRef, default);

        var info = await _provider.GetKeyInfoAsync(keyRef, default);
        info.Exists.Should().BeFalse();
    }

    [Fact]
    public async Task Encrypt_WithNonExistentKey_Throws()
    {
        var act = () => _provider.EncryptAsync("SOFTWARE:missing:1",
            Array.Empty<byte>(), new CryptoParameters { Mode = "ECB" }, default);

        await act.Should().ThrowAsync<CryptoProviderException>()
            .Where(e => e.ErrorCode == ProviderErrorCodes.PROVIDER_KEY_NOT_FOUND);
    }

    // ══════════════════════════════════════════════
    // 能力声明 & 健康检查
    // ══════════════════════════════════════════════

    [Fact]
    public void GetCapabilities_ContainsExpectedAlgorithms()
    {
        var caps = _provider.GetCapabilities();

        caps.Should().Contain("SM2");
        caps.Should().Contain("SM4_ECB");
        caps.Should().Contain("SM4_CBC");
        caps.Should().Contain("SM4_CTR");
        caps.Should().Contain("SM4_GCM");
        caps.Should().Contain("SM3");
        caps.Should().Contain("HMAC-SM3");
    }

    [Fact]
    public async Task CheckHealth_ReturnsHealthy()
    {
        var health = await _provider.CheckHealthAsync(default);

        health.Status.Should().Be("HEALTHY");
        health.Message.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void ProviderType_IsSoftware()
    {
        _provider.ProviderType.Should().Be("SOFTWARE");
    }

    // ══════════════════════════════════════════════
    // 辅助方法
    // ══════════════════════════════════════════════

    private async Task<string> GenerateSm4Key()
    {
        var result = await _provider.GenerateKeyAsync(KeyAlgorithm.SM4_128, default);
        return result.ProviderKeyRef;
    }

    private async Task<string> GenerateHmacKey()
    {
        var result = await _provider.GenerateKeyAsync(KeyAlgorithm.HMAC_SM3, default);
        return result.ProviderKeyRef;
    }


}
