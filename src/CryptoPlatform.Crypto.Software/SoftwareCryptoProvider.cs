using System.Collections.Concurrent;
using System.Text;
using CryptoPlatform.Crypto.Abstractions;
using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Macs;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Paddings;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;

namespace CryptoPlatform.Crypto.Software;

/// <summary>
/// 基于 BouncyCastle 的软件密码运算 Provider。
/// 支持 SM2（加解密/签名验签）、SM4（ECB/CBC/CTR/GCM）、SM3（杂凑）、HMAC-SM3。
/// </summary>
public sealed class SoftwareCryptoProvider : ICryptoProvider
{
    private static readonly X9ECParameters Sm2Curve = ECNamedCurveTable.GetByName("sm2p256v1");
    private static readonly ECDomainParameters Sm2Domain = new(Sm2Curve.Curve, Sm2Curve.G, Sm2Curve.N, Sm2Curve.H);

    private readonly ConcurrentDictionary<string, StoredKey> _keyStore = new();
    private readonly SecureRandom _random = new();
    private readonly ILogger<SoftwareCryptoProvider> _logger;
    private readonly byte[] _masterKek;

    private static readonly HashSet<string> Capabilities =
    [
        "SM2", "SM4_ECB", "SM4_CBC", "SM4_CTR", "SM4_GCM", "SM3", "HMAC-SM3"
    ];

    public string ProviderType => "SOFTWARE";
    public string ProviderName => "SoftwareCryptoProvider";

    public SoftwareCryptoProvider(ILogger<SoftwareCryptoProvider> logger, byte[]? masterKek = null)
    {
        _logger = logger;
        _masterKek = masterKek ?? Encoding.UTF8.GetBytes("CryptoPlatform.DefaultKEK!2026");
    }

    // ══════════════════════════════════════════════
    // 密钥生成
    // ══════════════════════════════════════════════

    public Task<ProviderKeyResult> GenerateKeyAsync(KeyAlgorithm algorithm, CancellationToken ct)
    {
        byte[] rawKey;
        string algoName;

        switch (algorithm)
        {
            case KeyAlgorithm.SM4_128:
                rawKey = new byte[16];
                _random.NextBytes(rawKey);
                algoName = "SM4_128";
                break;
            case KeyAlgorithm.HMAC_SM3:
                rawKey = new byte[16];
                _random.NextBytes(rawKey);
                algoName = "HMAC_SM3";
                break;
            default:
                throw new CryptoProviderException(
                    ProviderErrorCodes.PROVIDER_CAPABILITY_NOT_SUPPORTED,
                    $"不支持的对称算法: {algorithm}");
        }

        var keyId = GenerateKeyId();
        var keyRef = $"SOFTWARE:{keyId}:1";
        var fingerprint = ComputeFingerprint(rawKey);
        var encrypted = WrapKeyMaterial(rawKey);

        _keyStore[keyRef] = new StoredKey(algoName, rawKey, null, DateTime.UtcNow);

        return Task.FromResult(new ProviderKeyResult
        {
            ProviderKeyRef = keyRef,
            Fingerprint = fingerprint,
            EncryptedKeyMaterial = Convert.ToHexString(encrypted)
        });
    }

    public Task<ProviderKeyPairResult> GenerateKeyPairAsync(KeyPairAlgorithm algorithm, CancellationToken ct)
    {
        if (algorithm != KeyPairAlgorithm.SM2)
            throw new CryptoProviderException(
                ProviderErrorCodes.PROVIDER_CAPABILITY_NOT_SUPPORTED,
                $"不支持的非对称算法: {algorithm}");

        var generator = new ECKeyPairGenerator();
        generator.Init(new ECKeyGenerationParameters(Sm2Domain, _random));
        var keyPair = generator.GenerateKeyPair();

        var privateKey = (ECPrivateKeyParameters)keyPair.Private;
        var publicKey = (ECPublicKeyParameters)keyPair.Public;

        var pubPoint = publicKey.Q.Normalize();
        var pubBytes = pubPoint.GetEncoded(false); // 未压缩格式 65 字节
        var pubHex = Convert.ToHexString(pubBytes);
        var fingerprint = ComputeFingerprint(pubBytes);

        var keyId = GenerateKeyId();
        var keyRef = $"SOFTWARE:{keyId}:1";

        // 私钥 DER 编码存储
        var privInfo = PrivateKeyInfoFactory.CreatePrivateKeyInfo(privateKey);
        var privDer = privInfo.GetDerEncoded();
        var encrypted = WrapKeyMaterial(privDer);

        _keyStore[keyRef] = new StoredKey("SM2", privDer, pubHex, DateTime.UtcNow);

        return Task.FromResult(new ProviderKeyPairResult
        {
            ProviderKeyRef = keyRef,
            PublicKeyMaterial = pubHex,
            Fingerprint = fingerprint,
            EncryptedKeyMaterial = Convert.ToHexString(encrypted)
        });
    }

    // ══════════════════════════════════════════════
    // SM2 加解密
    // ══════════════════════════════════════════════

    public Task<CryptoResult> EncryptAsync(
        string providerKeyRef, ReadOnlyMemory<byte> plaintext,
        CryptoParameters parameters, CancellationToken ct)
    {
        var key = GetKey(providerKeyRef);

        if (key.Algorithm == "SM2")
        {
            var result = Sm2Encrypt(key, plaintext, parameters);
            return Task.FromResult(result);
        }

        if (key.Algorithm is "SM4_128")
        {
            var result = Sm4Encrypt(key.RawKey, plaintext, parameters);
            return Task.FromResult(result);
        }

        throw new CryptoProviderException(
            ProviderErrorCodes.PROVIDER_OPERATION_FAILED,
            $"算法 {key.Algorithm} 不支持加密操作");
    }

    public Task<CryptoResult> DecryptAsync(
        string providerKeyRef, ReadOnlyMemory<byte> ciphertext,
        CryptoParameters parameters, CancellationToken ct)
    {
        var key = GetKey(providerKeyRef);

        if (key.Algorithm == "SM2")
        {
            var result = Sm2Decrypt(key, ciphertext, parameters);
            return Task.FromResult(result);
        }

        if (key.Algorithm is "SM4_128")
        {
            var result = Sm4Decrypt(key.RawKey, ciphertext, parameters);
            return Task.FromResult(result);
        }

        throw new CryptoProviderException(
            ProviderErrorCodes.PROVIDER_OPERATION_FAILED,
            $"算法 {key.Algorithm} 不支持解密操作");
    }

    private static CryptoResult Sm2Encrypt(StoredKey key, ReadOnlyMemory<byte> plaintext, CryptoParameters parameters)
    {
        var privInfo = (AsymmetricKeyParameter)PrivateKeyFactory.CreateKey(key.RawKey);
        var pubPoint = Sm2Curve.Curve.DecodePoint(Convert.FromHexString(key.PublicKeyHex!));
        var pubParams = new ECPublicKeyParameters(pubPoint, Sm2Domain);

        // SM2Engine 默认输出 C1C2C3 格式
        var engine = new SM2Engine();
        engine.Init(true, new ParametersWithRandom(pubParams, new SecureRandom()));
        var c1c2c3 = engine.ProcessBlock(plaintext.ToArray(), 0, plaintext.Length);

        byte[] output;
        if (string.Equals(parameters.CipherFormat, "C1C3C2", StringComparison.OrdinalIgnoreCase))
        {
            output = ConvertC1C2C3ToC1C3C2(c1c2c3);
        }
        else
        {
            output = c1c2c3; // 默认 C1C2C3
        }

        return new CryptoResult { Ciphertext = output };
    }

    private static CryptoResult Sm2Decrypt(StoredKey key, ReadOnlyMemory<byte> ciphertext, CryptoParameters parameters)
    {
        var privParams = (ECPrivateKeyParameters)PrivateKeyFactory.CreateKey(key.RawKey);

        var inputData = ciphertext.ToArray();
        if (string.Equals(parameters.CipherFormat, "C1C3C2", StringComparison.OrdinalIgnoreCase))
        {
            inputData = ConvertC1C3C2ToC1C2C3(inputData);
        }

        var engine = new SM2Engine();
        engine.Init(false, privParams);
        var plaintext = engine.ProcessBlock(inputData, 0, inputData.Length);

        return new CryptoResult { Plaintext = plaintext };
    }

    // ══════════════════════════════════════════════
    // SM4 加解密
    // ══════════════════════════════════════════════

    private static CryptoResult Sm4Encrypt(byte[] keyBytes, ReadOnlyMemory<byte> plaintext, CryptoParameters parameters)
    {
        var mode = (parameters.Mode ?? "ECB").ToUpperInvariant();
        return mode switch
        {
            "ECB" => Sm4EcbEncrypt(keyBytes, plaintext, parameters.Padding),
            "CBC" => Sm4CbcEncrypt(keyBytes, plaintext, parameters),
            "CTR" => Sm4CtrEncrypt(keyBytes, plaintext, parameters),
            "GCM" => Sm4GcmEncrypt(keyBytes, plaintext, parameters),
            _ => throw new CryptoProviderException(
                ProviderErrorCodes.PROVIDER_CAPABILITY_NOT_SUPPORTED,
                $"不支持的 SM4 模式: {mode}")
        };
    }

    private static CryptoResult Sm4Decrypt(byte[] keyBytes, ReadOnlyMemory<byte> ciphertext, CryptoParameters parameters)
    {
        var mode = (parameters.Mode ?? "ECB").ToUpperInvariant();
        return mode switch
        {
            "ECB" => Sm4EcbDecrypt(keyBytes, ciphertext, parameters.Padding),
            "CBC" => Sm4CbcDecrypt(keyBytes, ciphertext, parameters),
            "CTR" => Sm4CtrDecrypt(keyBytes, ciphertext, parameters),
            "GCM" => Sm4GcmDecrypt(keyBytes, ciphertext, parameters),
            _ => throw new CryptoProviderException(
                ProviderErrorCodes.PROVIDER_CAPABILITY_NOT_SUPPORTED,
                $"不支持的 SM4 模式: {mode}")
        };
    }

    // ── ECB ──

    private static CryptoResult Sm4EcbEncrypt(byte[] key, ReadOnlyMemory<byte> plaintext, string? padding)
    {
        var engine = new SM4Engine();
        engine.Init(true, new KeyParameter(key));

        var input = plaintext.ToArray();
        var usePadding = !string.Equals(padding, "NONE", StringComparison.OrdinalIgnoreCase);
        if (usePadding)
            input = AddPkcs7Padding(input, engine.GetBlockSize());

        var output = new byte[input.Length];
        for (int i = 0; i < input.Length; i += engine.GetBlockSize())
            engine.ProcessBlock(input, i, output, i);

        return new CryptoResult { Ciphertext = output };
    }

    private static CryptoResult Sm4EcbDecrypt(byte[] key, ReadOnlyMemory<byte> ciphertext, string? padding)
    {
        var engine = new SM4Engine();
        engine.Init(false, new KeyParameter(key));

        var input = ciphertext.ToArray();
        var output = new byte[input.Length];
        for (int i = 0; i < input.Length; i += engine.GetBlockSize())
            engine.ProcessBlock(input, i, output, i);

        var usePadding = !string.Equals(padding, "NONE", StringComparison.OrdinalIgnoreCase);
        if (usePadding)
            output = RemovePkcs7Padding(output);

        return new CryptoResult { Plaintext = output };
    }

    private static byte[] AddPkcs7Padding(byte[] data, int blockSize)
    {
        var padLen = blockSize - (data.Length % blockSize);
        var padded = new byte[data.Length + padLen];
        Buffer.BlockCopy(data, 0, padded, 0, data.Length);
        for (int i = data.Length; i < padded.Length; i++)
            padded[i] = (byte)padLen;
        return padded;
    }

    private static byte[] RemovePkcs7Padding(byte[] data)
    {
        if (data.Length == 0) return data;
        var padLen = data[^1];
        if (padLen < 1 || padLen > 16) return data;
        return data[..^padLen];
    }

    // ── CBC ──

    private static CryptoResult Sm4CbcEncrypt(byte[] key, ReadOnlyMemory<byte> plaintext, CryptoParameters parameters)
    {
        var iv = parameters.Nonce ?? GenerateIv();
        var usePadding = !string.Equals(parameters.Padding, "NONE", StringComparison.OrdinalIgnoreCase);

        var engine = new SM4Engine();
        engine.Init(true, new KeyParameter(key));

        var input = plaintext.ToArray();
        if (usePadding)
            input = AddPkcs7Padding(input, engine.GetBlockSize());

        var output = new byte[input.Length];
        var prev = (byte[])iv.Clone();

        for (int i = 0; i < input.Length; i += engine.GetBlockSize())
        {
            // XOR with previous ciphertext block (or IV)
            var block = new byte[16];
            for (int j = 0; j < 16; j++)
                block[j] = (byte)(input[i + j] ^ prev[j]);
            engine.ProcessBlock(block, 0, output, i);
            Buffer.BlockCopy(output, i, prev, 0, 16);
        }

        return new CryptoResult { Ciphertext = output, Nonce = iv };
    }

    private static CryptoResult Sm4CbcDecrypt(byte[] key, ReadOnlyMemory<byte> ciphertext, CryptoParameters parameters)
    {
        var iv = parameters.Nonce ?? throw new CryptoProviderException(
            ProviderErrorCodes.PROVIDER_OPERATION_FAILED, "CBC 模式解密需要提供 Nonce(IV)");
        var usePadding = !string.Equals(parameters.Padding, "NONE", StringComparison.OrdinalIgnoreCase);

        var engine = new SM4Engine();
        engine.Init(false, new KeyParameter(key));

        var input = ciphertext.ToArray();
        var output = new byte[input.Length];
        var prev = (byte[])iv.Clone();

        for (int i = 0; i < input.Length; i += engine.GetBlockSize())
        {
            var decrypted = new byte[16];
            engine.ProcessBlock(input, i, decrypted, 0);
            for (int j = 0; j < 16; j++)
                output[i + j] = (byte)(decrypted[j] ^ prev[j]);
            Buffer.BlockCopy(input, i, prev, 0, 16);
        }

        if (usePadding)
            output = RemovePkcs7Padding(output);

        return new CryptoResult { Plaintext = output };
    }

    // ── CTR ──

    private static CryptoResult Sm4CtrEncrypt(byte[] key, ReadOnlyMemory<byte> plaintext, CryptoParameters parameters)
    {
        var iv = parameters.Nonce ?? GenerateIv();

        var ctrMode = new SicBlockCipher(new SM4Engine());
        var ctr = new BufferedBlockCipher(ctrMode);
        ctr.Init(true, new ParametersWithIV(new KeyParameter(key), iv));

        var output = ctr.ProcessBytes(plaintext.ToArray());
        var final = ctr.DoFinal();

        return new CryptoResult
        {
            Ciphertext = Combine(output ?? [], final ?? []),
            Nonce = iv
        };
    }

    private static CryptoResult Sm4CtrDecrypt(byte[] key, ReadOnlyMemory<byte> ciphertext, CryptoParameters parameters)
    {
        var iv = parameters.Nonce ?? throw new CryptoProviderException(
            ProviderErrorCodes.PROVIDER_OPERATION_FAILED, "CTR 模式解密需要提供 Nonce(IV)");

        var ctrMode = new SicBlockCipher(new SM4Engine());
        var ctr = new BufferedBlockCipher(ctrMode);
        ctr.Init(false, new ParametersWithIV(new KeyParameter(key), iv));

        var output = ctr.ProcessBytes(ciphertext.ToArray());
        var final = ctr.DoFinal();

        return new CryptoResult { Plaintext = Combine(output ?? [], final ?? []) };
    }

    // ── GCM ──

    private static CryptoResult Sm4GcmEncrypt(byte[] key, ReadOnlyMemory<byte> plaintext, CryptoParameters parameters)
    {
        var nonce = parameters.Nonce ?? GenerateGcmNonce();
        var aeadParams = new AeadParameters(new KeyParameter(key), 128, nonce, parameters.Aad);

        var gcm = new GcmBlockCipher(new SM4Engine());
        gcm.Init(true, aeadParams);

        var ptBytes = plaintext.ToArray();
        var outBuf = new byte[gcm.GetOutputSize(ptBytes.Length)];
        var len = gcm.ProcessBytes(ptBytes, 0, ptBytes.Length, outBuf, 0);
        len += gcm.DoFinal(outBuf, len);
        var combined = outBuf[..len];

        // GCM 输出 = 密文 + Tag(16B)
        const int tagLen = 16; // GCM tag 固定 16 字节
        var ciphertext = combined[..^tagLen];
        var tag = combined[^tagLen..];

        return new CryptoResult
        {
            Ciphertext = ciphertext,
            Nonce = nonce,
            Tag = tag
        };
    }

    private static CryptoResult Sm4GcmDecrypt(byte[] key, ReadOnlyMemory<byte> ciphertext, CryptoParameters parameters)
    {
        var nonce = parameters.Nonce ?? throw new CryptoProviderException(
            ProviderErrorCodes.PROVIDER_OPERATION_FAILED, "GCM 模式解密需要提供 Nonce");

        // Tag 可能在 parameters.Tag 中，也可能追加在 ciphertext 末尾
        byte[] ctBytes;
        byte[] tag;
        if (parameters.Tag is { Length: > 0 })
        {
            ctBytes = ciphertext.ToArray();
            tag = parameters.Tag;
        }
        else
        {
            var tagLen = 16; // GCM tag 固定 16 字节
            ctBytes = ciphertext[..^tagLen].ToArray();
            tag = ciphertext[^tagLen..].ToArray();
        }

        var aeadParams = new AeadParameters(new KeyParameter(key), 128, nonce, parameters.Aad);
        var gcm = new GcmBlockCipher(new SM4Engine());
        gcm.Init(false, aeadParams);

        var ctWithTag = Combine(ctBytes, tag);
        var outBuf = new byte[gcm.GetOutputSize(ctWithTag.Length)];
        var len = gcm.ProcessBytes(ctWithTag, 0, ctWithTag.Length, outBuf, 0);
        len += gcm.DoFinal(outBuf, len);

        return new CryptoResult { Plaintext = outBuf[..len] };
    }

    // ══════════════════════════════════════════════
    // SM2 签名/验签
    // ══════════════════════════════════════════════

    public Task<SignResult> SignAsync(
        string providerKeyRef, ReadOnlyMemory<byte> data,
        CryptoParameters parameters, CancellationToken ct)
    {
        var key = GetKey(providerKeyRef);
        if (key.Algorithm != "SM2")
            throw new CryptoProviderException(
                ProviderErrorCodes.PROVIDER_KEY_INVALID,
                "签名操作需要 SM2 密钥");

        var privParams = (ECPrivateKeyParameters)PrivateKeyFactory.CreateKey(key.RawKey);
        var signer = new SM2Signer();
        signer.Init(true, new ParametersWithRandom(privParams, _random));
        signer.BlockUpdate(data.ToArray(), 0, data.Length);
        var derSig = signer.GenerateSignature();

        var format = string.Equals(parameters.SignatureFormat, "RAW", StringComparison.OrdinalIgnoreCase)
            ? "RAW" : "DER";
        var sigBytes = format == "RAW" ? DerSignatureToRaw(derSig) : derSig;

        return Task.FromResult(new SignResult { Signature = sigBytes, Format = format });
    }

    public Task<bool> VerifyAsync(
        string providerKeyRef, ReadOnlyMemory<byte> data,
        ReadOnlyMemory<byte> signature, CryptoParameters parameters, CancellationToken ct)
    {
        var key = GetKey(providerKeyRef);
        if (key.Algorithm != "SM2")
            throw new CryptoProviderException(
                ProviderErrorCodes.PROVIDER_KEY_INVALID,
                "验签操作需要 SM2 密钥");

        var pubPoint = Sm2Curve.Curve.DecodePoint(Convert.FromHexString(key.PublicKeyHex!));
        var pubParams = new ECPublicKeyParameters(pubPoint, Sm2Domain);

        var sigBytes = signature.ToArray();
        if (string.Equals(parameters.SignatureFormat, "RAW", StringComparison.OrdinalIgnoreCase))
        {
            sigBytes = RawSignatureToDer(sigBytes);
        }

        var signer = new SM2Signer();
        signer.Init(false, pubParams);
        signer.BlockUpdate(data.ToArray(), 0, data.Length);

        return Task.FromResult(signer.VerifySignature(sigBytes));
    }

    // ══════════════════════════════════════════════
    // 杂凑 / HMAC / 随机数
    // ══════════════════════════════════════════════

    public Task<byte[]> HashAsync(string algorithm, ReadOnlyMemory<byte> data, CancellationToken ct)
    {
        if (!string.Equals(algorithm, "SM3", StringComparison.OrdinalIgnoreCase))
            throw new CryptoProviderException(
                ProviderErrorCodes.PROVIDER_CAPABILITY_NOT_SUPPORTED,
                $"不支持的杂凑算法: {algorithm}");

        var digest = new SM3Digest();
        digest.BlockUpdate(data.ToArray(), 0, data.Length);
        var hash = new byte[digest.GetDigestSize()];
        digest.DoFinal(hash, 0);

        return Task.FromResult(hash);
    }

    public Task<byte[]> HmacAsync(string providerKeyRef, ReadOnlyMemory<byte> data, CancellationToken ct)
    {
        var key = GetKey(providerKeyRef);
        if (key.Algorithm != "HMAC_SM3")
            throw new CryptoProviderException(
                ProviderErrorCodes.PROVIDER_KEY_INVALID,
                "HMAC 操作需要 HMAC_SM3 密钥");

        var hmac = new HMac(new SM3Digest());
        hmac.Init(new KeyParameter(key.RawKey));
        hmac.BlockUpdate(data.ToArray(), 0, data.Length);

        var result = new byte[hmac.GetMacSize()];
        hmac.DoFinal(result, 0);

        return Task.FromResult(result);
    }

    public Task<RandomResult> GenerateRandomAsync(int length, CancellationToken ct)
    {
        var bytes = new byte[length];
        _random.NextBytes(bytes);
        return Task.FromResult(new RandomResult { Bytes = bytes });
    }

    // ══════════════════════════════════════════════
    // 密钥管理
    // ══════════════════════════════════════════════

    public Task<ProviderKeyInfo> GetKeyInfoAsync(string providerKeyRef, CancellationToken ct)
    {
        if (_keyStore.TryGetValue(providerKeyRef, out var key))
        {
            return Task.FromResult(new ProviderKeyInfo
            {
                ProviderKeyRef = providerKeyRef,
                Exists = true,
                Algorithm = key.Algorithm,
                CreatedAt = key.CreatedAt
            });
        }

        return Task.FromResult(new ProviderKeyInfo
        {
            ProviderKeyRef = providerKeyRef,
            Exists = false
        });
    }

    public Task DestroyKeyAsync(string providerKeyRef, CancellationToken ct)
    {
        _keyStore.TryRemove(providerKeyRef, out _);
        _logger.LogInformation("密钥已销毁: {KeyRef}", providerKeyRef);
        return Task.CompletedTask;
    }

    // ══════════════════════════════════════════════
    // 健康检查 / 能力声明
    // ══════════════════════════════════════════════

    public Task<ProviderHealth> CheckHealthAsync(CancellationToken ct)
    {
        return Task.FromResult(new ProviderHealth
        {
            Status = "HEALTHY",
            Message = $"Software Provider 运行正常，当前管理 {_keyStore.Count} 个密钥",
            Latency = TimeSpan.Zero
        });
    }

    public IReadOnlySet<string> GetCapabilities() => Capabilities;

    // ══════════════════════════════════════════════
    // SM2 密文格式转换
    // ══════════════════════════════════════════════

    /// <summary>C1(65)||C2(n)||C3(32) → C1(65)||C3(32)||C2(n)</summary>
    private static byte[] ConvertC1C2C3ToC1C3C2(byte[] c1c2c3)
    {
        const int c1Len = 65;
        const int c3Len = 32;
        var c2Len = c1c2c3.Length - c1Len - c3Len;

        var c1 = c1c2c3.AsSpan(0, c1Len);
        var c2 = c1c2c3.AsSpan(c1Len, c2Len);
        var c3 = c1c2c3.AsSpan(c1Len + c2Len, c3Len);

        var result = new byte[c1c2c3.Length];
        c1.CopyTo(result.AsSpan(0));
        c3.CopyTo(result.AsSpan(c1Len));
        c2.CopyTo(result.AsSpan(c1Len + c3Len));
        return result;
    }

    /// <summary>C1(65)||C3(32)||C2(n) → C1(65)||C2(n)||C3(32)</summary>
    private static byte[] ConvertC1C3C2ToC1C2C3(byte[] c1c3c2)
    {
        const int c1Len = 65;
        const int c3Len = 32;
        var c2Len = c1c3c2.Length - c1Len - c3Len;

        var c1 = c1c3c2.AsSpan(0, c1Len);
        var c3 = c1c3c2.AsSpan(c1Len, c3Len);
        var c2 = c1c3c2.AsSpan(c1Len + c3Len, c2Len);

        var result = new byte[c1c3c2.Length];
        c1.CopyTo(result.AsSpan(0));
        c2.CopyTo(result.AsSpan(c1Len));
        c3.CopyTo(result.AsSpan(c1Len + c2Len));
        return result;
    }

    // ══════════════════════════════════════════════
    // SM2 签名格式转换（DER ↔ RAW）
    // ══════════════════════════════════════════════

    /// <summary>DER 编码签名 → RAW(R||S) 格式，各 32 字节</summary>
    private static byte[] DerSignatureToRaw(byte[] der)
    {
        var seq = (DerSequence)DerSequence.FromByteArray(der);
        var r = ((DerInteger)seq[0]).Value;
        var s = ((DerInteger)seq[1]).Value;
        return ConcatFixedLength(r.ToByteArrayUnsigned(), s.ToByteArrayUnsigned(), 32);
    }

    /// <summary>RAW(R||S) 格式 → DER 编码签名</summary>
    private static byte[] RawSignatureToDer(byte[] raw)
    {
        if (raw.Length != 64)
            throw new CryptoProviderException(
                ProviderErrorCodes.PROVIDER_OPERATION_FAILED,
                $"RAW 签名长度应为 64 字节，实际为 {raw.Length} 字节");

        var r = new BigInteger(1, raw[..32]);
        var s = new BigInteger(1, raw[32..]);
        var seq = new DerSequence(new DerInteger(r), new DerInteger(s));
        return seq.GetDerEncoded();
    }

    private static byte[] ConcatFixedLength(byte[] r, byte[] s, int length)
    {
        var result = new byte[length * 2];
        PadAndCopy(r, result, 0, length);
        PadAndCopy(s, result, length, length);
        return result;
    }

    private static void PadAndCopy(byte[] src, byte[] dest, int destOffset, int length)
    {
        if (src.Length > length)
            Buffer.BlockCopy(src, src.Length - length, dest, destOffset, length);
        else
            Buffer.BlockCopy(src, 0, dest, destOffset + length - src.Length, src.Length);
    }

    // ══════════════════════════════════════════════
    // KEK 密钥包装（SM4-ECB）
    // ══════════════════════════════════════════════

    private byte[] WrapKeyMaterial(byte[] rawKey)
    {
        var kek = DeriveKek(_masterKek);
        var engine = new SM4Engine();
        engine.Init(true, new KeyParameter(kek));
        var padded = AddPkcs7Padding(rawKey, engine.GetBlockSize());
        var output = new byte[padded.Length];
        for (int i = 0; i < padded.Length; i += engine.GetBlockSize())
            engine.ProcessBlock(padded, i, output, i);
        return output;
    }

    private static byte[] DeriveKek(byte[] masterKey)
    {
        var digest = new SM3Digest();
        digest.BlockUpdate(masterKey, 0, masterKey.Length);
        var kek = new byte[16]; // SM4 密钥 16 字节
        var fullHash = new byte[digest.GetDigestSize()];
        digest.DoFinal(fullHash, 0);
        Array.Copy(fullHash, kek, 16);
        return kek;
    }

    // ══════════════════════════════════════════════
    // 辅助方法
    // ══════════════════════════════════════════════

    private StoredKey GetKey(string keyRef)
    {
        if (!_keyStore.TryGetValue(keyRef, out var key))
            throw new CryptoProviderException(
                ProviderErrorCodes.PROVIDER_KEY_NOT_FOUND,
                $"密钥不存在: {keyRef}");
        return key;
    }

    private string GenerateKeyId()
    {
        var bytes = new byte[16];
        _random.NextBytes(bytes);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string ComputeFingerprint(byte[] data)
    {
        var digest = new SM3Digest();
        digest.BlockUpdate(data, 0, data.Length);
        var hash = new byte[digest.GetDigestSize()];
        digest.DoFinal(hash, 0);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static byte[] GenerateIv()
    {
        var iv = new byte[16];
        new SecureRandom().NextBytes(iv);
        return iv;
    }

    private static byte[] GenerateGcmNonce()
    {
        var nonce = new byte[12];
        new SecureRandom().NextBytes(nonce);
        return nonce;
    }

    private static byte[] Combine(byte[] a, byte[] b)
    {
        var result = new byte[a.Length + b.Length];
        Buffer.BlockCopy(a, 0, result, 0, a.Length);
        Buffer.BlockCopy(b, 0, result, a.Length, b.Length);
        return result;
    }

    // ── 内部密钥存储结构 ──

    private sealed record StoredKey(
        string Algorithm,
        byte[] RawKey,
        string? PublicKeyHex,
        DateTime CreatedAt);
}
