namespace CryptoPlatform.Crypto.Abstractions;

/* ─────────────────────────────────────────────────────────────
 * 可选能力扩展接口（需求 V2.10 §2.6）。
 *
 * 非所有密码设备都必须实现以下能力；由 ProviderCapabilities 与可选接口共同决定。
 * 能力缺失时平台对应接口返回明确 NotSupported 并记录审计，不得静默失败，
 * 不得为绕过设备安全边界而做替代实现。
 * ───────────────────────────────────────────────────────────── */

/// <summary>密钥包裹请求。</summary>
public sealed record WrapKeyRequest
{
    /// <summary>被包裹密钥的 Provider 引用</summary>
    public required string ProviderKeyRef { get; init; }

    /// <summary>包裹密钥（KEK）的 Provider 引用；缺省使用 L2 KEK-Runtime</summary>
    public string? KekKeyRef { get; init; }
}

/// <summary>密钥包裹结果（WrappedMaterial 形态，可持久化）。</summary>
public sealed record WrappedKey
{
    public required byte[] WrappedMaterial { get; init; }

    /// <summary>密钥校验值（KCV，用于恢复后身份校验）</summary>
    public string? Kcv { get; init; }

    /// <summary>包裹算法标识（平台语义名，如 SM4_ECB）</summary>
    public string Algorithm { get; init; } = "";
}

/// <summary>密钥解包裹请求。</summary>
public sealed record UnwrapKeyRequest
{
    public required byte[] WrappedMaterial { get; init; }

    public string? KekKeyRef { get; init; }

    /// <summary>解包裹后密钥的预期算法（用于 KeyType 兼容性校验，V2.10 §3.1.7）</summary>
    public string? ExpectedAlgorithm { get; init; }
}

/// <summary>密钥解包裹结果。明文材料仅在 Provider 进程内有效，禁止落库与出 API。</summary>
public sealed record UnwrapKeyResult
{
    /// <summary>Provider 内部密钥引用（会话密钥句柄的稳定标识）</summary>
    public required string ProviderKeyRef { get; init; }

    public required string Algorithm { get; init; }

    public int KeyBits { get; init; }
}

/// <summary>Provider 级密钥备份请求（HSM 场景对应 Vendor Backup / Security Domain Backup 的登记）。</summary>
public sealed record BackupRequest
{
    public required string KeyId { get; init; }

    public required string ProviderKeyRef { get; init; }

    /// <summary>备份目的地名（Provider 特定）</summary>
    public string? Target { get; init; }
}

/// <summary>Provider 级备份结果（平台仅登记可观测的备份元数据，V2.10 §3.11.5）。</summary>
public sealed record BackupResult
{
    public bool Success { get; init; }

    /// <summary>Provider 回执（Vendor Backup 任务号/凭据等）</summary>
    public string? Receipt { get; init; }

    public string? FailureReason { get; init; }
}

/// <summary>Provider 级密钥恢复请求。恢复后必须执行 KeyId → ProviderKeyIdentity → ProviderReference 身份校验。</summary>
public sealed record RestoreRequest
{
    public required string KeyId { get; init; }

    /// <summary>Provider 特定恢复载荷（Vendor Backup 回执/介质引用）</summary>
    public required string ProviderPayload { get; init; }

    /// <summary>预期密钥身份（KCV / PublicKeyFingerprint / ProviderKeyIdentity）</summary>
    public string? ExpectedKeyIdentity { get; init; }
}

/// <summary>Provider 级恢复结果。</summary>
public sealed record RestoreResult
{
    public bool Success { get; init; }

    /// <summary>身份校验是否通过（校验失败视为恢复失败并告警）</summary>
    public bool IdentityVerified { get; init; }

    public string? FailureReason { get; init; }
}

/// <summary>密钥导入请求。导入必须采用带保护结构的方式（GM/T 0018-2023 无明文导入）。</summary>
public sealed record ImportKeyRequest
{
    /// <summary>受保护密钥材料（KEK / IPK / EPK 包裹形态）</summary>
    public required byte[] ProtectedMaterial { get; init; }

    /// <summary>材料包裹方式："KEK" / "IPK" / "EPK" / "ENVELOPED_ECC_KEY"</summary>
    public required string WrapScheme { get; init; }

    /// <summary>预期算法（KeyType 兼容性校验）</summary>
    public required string Algorithm { get; init; }

    /// <summary>保护密钥引用（WrapScheme=KEK 时的 KEK 索引引用）</summary>
    public string? KekKeyRef { get; init; }
}

/// <summary>密钥导出请求。HSM 通常不支持；支持时也仅限 KEK 保护下的会话密钥。</summary>
public sealed record ExportKeyRequest
{
    public required string ProviderKeyRef { get; init; }

    /// <summary>导出保护方式；目前仅支持 "KEK"</summary>
    public string WrapScheme { get; init; } = "KEK";

    public string? KekKeyRef { get; init; }
}

/// <summary>密钥导出结果。</summary>
public sealed record ExportedKey
{
    public required byte[] ProtectedMaterial { get; init; }

    public required string WrapScheme { get; init; }

    public string? Kcv { get; init; }
}

/// <summary>Root Key 轮换请求（F-RK-004：需安全管理员审批、二次确认）。</summary>
public sealed record RotateRootKeyRequest
{
    /// <summary>本次轮换的审批单号（审计关联）</summary>
    public required string ApprovalNo { get; init; }

    /// <summary>是否同时对存量 KEK/Data Key 执行重包裹</summary>
    public bool RewrapChildren { get; init; } = true;
}

/// <summary>Root Key 轮换结果。</summary>
public sealed record RotateRootKeyResult
{
    public bool Success { get; init; }

    /// <summary>新 Root Key 的 ProviderKeyIdentity</summary>
    public string? NewKeyIdentity { get; init; }

    /// <summary>已重包裹的密钥数量（RewrapChildren=true 时）</summary>
    public int RewrappedCount { get; init; }

    public string? FailureReason { get; init; }
}

/// <summary>重包裹请求（F-RK-004 / F-KEK-003）。</summary>
public sealed record RewrapRequest
{
    /// <summary>目标密钥引用集合；空表示按策略全量</summary>
    public IReadOnlyList<string> ProviderKeyRefs { get; init; } = [];

    /// <summary>新保护密钥引用（轮换后的 KEK）</summary>
    public string? NewKekKeyRef { get; init; }
}

/// <summary>重包裹结果。</summary>
public sealed record RewrapResult
{
    public bool Success { get; init; }

    public int SuccessCount { get; init; }

    public int FailureCount { get; init; }

    public IReadOnlyList<string> FailureRefs { get; init; } = [];
}

/// <summary>Root Key 状态（F-RK-007 监控）。</summary>
public sealed record RootKeyStatus
{
    /// <summary>READY / NOT_READY / NOT_PROVISIONED</summary>
    public string Status { get; init; } = "UNKNOWN";

    public string? KeyIdentity { get; init; }

    public DateTimeOffset? LastVerifiedAt { get; init; }
}

/// <summary>密钥包裹/解包裹能力（Provider 准入强制项，V2.10 §2.6 最小能力准入集）。</summary>
public interface IKeyWrappingProvider
{
    Task<WrappedKey> WrapKeyAsync(WrapKeyRequest request, CancellationToken ct);

    /// <summary>解包裹：明文材料只在 Provider 进程内存在，出现在设备侧安全边界内（HSM）或进程内存（Software）。</summary>
    Task<UnwrapKeyResult> UnwrapKeyAsync(UnwrapKeyRequest request, CancellationToken ct);
}

/// <summary>Provider 级密钥备份能力（可选）。</summary>
public interface IKeyBackupProvider
{
    Task<BackupResult> BackupAsync(BackupRequest request, CancellationToken ct);
}

/// <summary>Provider 级密钥恢复能力（可选）。恢复后必须执行密钥身份校验。</summary>
public interface IKeyRestoreProvider
{
    Task<RestoreResult> RestoreAsync(RestoreRequest request, CancellationToken ct);
}

/// <summary>密钥导入能力（可选）。</summary>
public interface IKeyImportProvider
{
    Task<ProviderKeyResult> ImportKeyAsync(ImportKeyRequest request, CancellationToken ct);
}

/// <summary>密钥导出能力（可选；HSM 通常不支持）。</summary>
public interface IKeyExportProvider
{
    Task<ExportedKey> ExportKeyAsync(ExportKeyRequest request, CancellationToken ct);
}

/// <summary>平台 Root Key / KEK 轮换与重包裹能力（可选，F-RK-004 / F-KEK-003）。</summary>
public interface IRootKeyProvider
{
    Task<RotateRootKeyResult> RotateRootKeyAsync(RotateRootKeyRequest request, CancellationToken ct);

    Task<RewrapResult> RewrapAsync(RewrapRequest request, CancellationToken ct);

    Task<RootKeyStatus> GetRootKeyStatusAsync(CancellationToken ct);
}
