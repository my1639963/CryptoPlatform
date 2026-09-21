namespace CryptoPlatform.Crypto.Abstractions;

/// <summary>
/// 对称密钥生成结果（SM4 / HMAC-SM3）
/// </summary>
public sealed class ProviderKeyResult
{
    /// <summary>Provider 内部密钥引用（格式：SOFTWARE:{KeyId}:{VersionNo}）</summary>
    public string ProviderKeyRef { get; init; } = "";

    /// <summary>公钥材料（SM2 场景；对称算法为 null）</summary>
    public string? PublicKeyMaterial { get; init; }

    /// <summary>密钥指纹（SM3 哈希）</summary>
    public string Fingerprint { get; init; } = "";

    /// <summary>设备 ID（软件模式为 null）</summary>
    public string? DeviceId { get; init; }

    /// <summary>加密后的密钥材料（软件模式，经 KEK 加密）</summary>
    public string? EncryptedKeyMaterial { get; init; }
}

/// <summary>
/// 非对称密钥对生成结果（SM2）
/// </summary>
public sealed class ProviderKeyPairResult
{
    /// <summary>Provider 内部密钥引用</summary>
    public string ProviderKeyRef { get; init; } = "";

    /// <summary>公钥材料（Hex 编码的未压缩公钥）</summary>
    public string PublicKeyMaterial { get; init; } = "";

    /// <summary>密钥指纹（SM3 哈希）</summary>
    public string Fingerprint { get; init; } = "";

    /// <summary>设备 ID（软件模式为 null）</summary>
    public string? DeviceId { get; init; }

    /// <summary>加密后的私钥材料（软件模式，经 KEK 加密）</summary>
    public string? EncryptedKeyMaterial { get; init; }
}

/// <summary>
/// 密钥信息
/// </summary>
public sealed class ProviderKeyInfo
{
    /// <summary>Provider 内部密钥引用</summary>
    public string ProviderKeyRef { get; init; } = "";

    /// <summary>密钥是否存在</summary>
    public bool Exists { get; init; }

    /// <summary>算法名称</summary>
    public string? Algorithm { get; init; }

    /// <summary>创建时间</summary>
    public DateTime? CreatedAt { get; init; }
}

/// <summary>
/// Provider 健康检查结果
/// </summary>
public sealed class ProviderHealth
{
    /// <summary>状态：HEALTHY / DEGRADED / UNHEALTHY</summary>
    public string Status { get; init; } = "UNKNOWN";

    /// <summary>状态描述</summary>
    public string? Message { get; init; }

    /// <summary>检查延迟</summary>
    public TimeSpan Latency { get; init; }
}
