namespace CryptoPlatform.Crypto.Abstractions;

/// <summary>
/// 密码运算参数。
/// 根据算法和模式不同，各字段含义：
/// SM4-GCM：Mode=GCM, Nonce(12B), Aad, Tag(16B)
/// SM4-CBC：Mode=CBC, Nonce=IV(16B), Padding=PKCS7/NONE
/// SM4-CTR：Mode=CTR, Nonce=IV(16B)
/// SM4-ECB：Mode=ECB, Padding=PKCS7/NONE
/// SM2：CipherFormat=C1C3C2/C1C2C3, SignatureFormat=DER/RAW
/// </summary>
public sealed class CryptoParameters
{
    /// <summary>加密模式：GCM / CBC / CTR / ECB</summary>
    public string? Mode { get; init; }

    /// <summary>随机数 / IV。GCM 为 12 字节，CBC/CTR 为 16 字节</summary>
    public byte[]? Nonce { get; init; }

    /// <summary>GCM 附加认证数据（AAD）</summary>
    public byte[]? Aad { get; init; }

    /// <summary>GCM 认证标签（16 字节）。解密时提供</summary>
    public byte[]? Tag { get; init; }

    /// <summary>填充模式：PKCS7 / NONE</summary>
    public string? Padding { get; init; }

    /// <summary>SM2 签名格式：DER / RAW</summary>
    public string? SignatureFormat { get; init; }

    /// <summary>SM2 密文格式：C1C3C2（新标准）/ C1C2C3（旧标准）</summary>
    public string? CipherFormat { get; init; }
}
