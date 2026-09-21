namespace CryptoPlatform.Crypto.Abstractions;

/// <summary>
/// 加解密结果
/// </summary>
public sealed class CryptoResult
{
    /// <summary>密文（加密结果）</summary>
    public byte[] Ciphertext { get; init; } = [];

    /// <summary>明文（解密结果）</summary>
    public byte[] Plaintext { get; init; } = [];

    /// <summary>GCM 模式随机数（加密时返回，供解密使用）</summary>
    public byte[]? Nonce { get; init; }

    /// <summary>GCM 认证标签（加密时返回）</summary>
    public byte[]? Tag { get; init; }
}

/// <summary>
/// 签名结果
/// </summary>
public sealed class SignResult
{
    /// <summary>签名值</summary>
    public byte[] Signature { get; init; } = [];

    /// <summary>签名格式：DER / RAW</summary>
    public string Format { get; init; } = "";
}

/// <summary>
/// 随机数生成结果
/// </summary>
public sealed class RandomResult
{
    /// <summary>随机字节数组</summary>
    public byte[] Bytes { get; init; } = [];
}
