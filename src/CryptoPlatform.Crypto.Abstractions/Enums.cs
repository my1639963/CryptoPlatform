namespace CryptoPlatform.Crypto.Abstractions;

/// <summary>
/// 对称密钥算法
/// </summary>
public enum KeyAlgorithm
{
    /// <summary>SM2（非对称，用于签名/密钥协商）</summary>
    SM2,

    /// <summary>SM4 128 位对称加密</summary>
    SM4_128,

    /// <summary>HMAC-SM3 消息认证码</summary>
    HMAC_SM3
}

/// <summary>
/// 非对称密钥对算法
/// </summary>
public enum KeyPairAlgorithm
{
    /// <summary>SM2 椭圆曲线</summary>
    SM2
}

/// <summary>
/// 杂凑算法
/// </summary>
public enum HashAlgorithmName
{
    /// <summary>SM3 密码杂凑（256 位）</summary>
    SM3
}
