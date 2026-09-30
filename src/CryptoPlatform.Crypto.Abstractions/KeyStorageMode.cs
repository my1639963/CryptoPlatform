namespace CryptoPlatform.Crypto.Abstractions;

/// <summary>
/// 密钥存储模式（需求 V2.10 §2.6 / §5.6.3）。
/// 平台仅支持其中一种部署模式，互斥，不支持混合部署。
/// </summary>
public enum KeyStorageMode
{
    /// <summary>软件密码模块（SoftSdfDevice，允许用于生产环境）</summary>
    Software,

    /// <summary>硬件密码设备 HSM（NativeSdfDevice，生产环境优先）</summary>
    Hsm,
}
