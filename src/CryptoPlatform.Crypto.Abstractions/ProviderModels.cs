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
/// 密钥销毁结果（需求 V2.10 §2.6：DestroyKeyAsync 必须返回明确结果）。
/// 阶段二将替换现返回 Task 的 DestroyKeyAsync 签名。
/// </summary>
public sealed class DestroyKeyResult
{
    /// <summary>是否成功</summary>
    public bool Success { get; init; }

    /// <summary>在线材料是否已销毁（设备内 / 进程内材料）</summary>
    public bool OnlineMaterialDestroyed { get; init; }

    /// <summary>归档材料保留标记（V2.10 §3.2.23：ARCHIVE MATERIAL 仅用于历史解密/验签）</summary>
    public bool ArchiveMaterialRetained { get; init; }

    /// <summary>失败原因（Provider 不支持或设备异常时填写）</summary>
    public string? FailureReason { get; init; }

    /// <summary>语义化构造：Provider 不支持该项能力（显式 NotSupported，禁止静默失败）</summary>
    public static DestroyKeyResult NotSupported(string reason) => new()
    {
        Success = false,
        OnlineMaterialDestroyed = false,
        FailureReason = $"NotSupported: {reason}",
    };
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
