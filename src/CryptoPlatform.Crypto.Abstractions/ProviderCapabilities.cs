namespace CryptoPlatform.Crypto.Abstractions;

/// <summary>
/// Provider 能力声明（需求 V2.10 §2.6）。
/// <para>
/// 算法类能力（<see cref="Algorithms"/>）的唯一事实来源是 SDF_GetDeviceInfo 返回的
/// DEVICEINFO 能力字段（实现规范-HSM适配器 §36.4.3 / §37.12）；人工登记值仅用于与设备
/// 实测结果比对，不一致即拒绝准入。不支持的能力必须返回明确的 NotSupported 并记录审计，
/// 不得静默失败或绕过密码设备安全边界（V2.10 §1.7.13）。
/// </para>
/// </summary>
public sealed class ProviderCapabilities
{
    /// <summary>是否支持密钥 Wrap（准入强制项）</summary>
    public bool CanWrapKey { get; init; }

    /// <summary>是否支持密钥 Unwrap（准入强制项）</summary>
    public bool CanUnwrapKey { get; init; }

    /// <summary>是否支持 Provider 级密钥备份（HSM = Vendor Backup 回执登记）</summary>
    public bool CanBackupKey { get; init; }

    /// <summary>是否支持 Provider 级密钥恢复</summary>
    public bool CanRestoreKey { get; init; }

    /// <summary>是否支持密钥导入（导入必须带保护结构；GM/T 0018-2023 无明文导入）</summary>
    public bool CanImportKey { get; init; }

    /// <summary>是否支持密钥导出（HSM 通常为 false）</summary>
    public bool CanExportKey { get; init; }

    /// <summary>是否支持不可导出密钥（索引型密钥对天然不出设备）</summary>
    public bool SupportsNonExportableKey { get; init; }

    /// <summary>是否支持平台逻辑 Root Key 轮换</summary>
    public bool CanRotateRootKey { get; init; }

    /// <summary>是否支持密钥重包裹/重新保护</summary>
    public bool CanRewrapKey { get; init; }

    /// <summary>是否支持 Key Identity / KCV / Fingerprint 校验</summary>
    public bool SupportsKeyIdentityVerification { get; init; }

    /// <summary>
    /// 算法能力集合（如 SM2、SM4_ECB、SM4_CBC、SM4_CTR、SM4_GCM、SM3、HMAC_SM3、RANDOM；
    /// 厂商扩展能力以 _EXT 后缀声明，如 SM2_DECRYPT_EXT）。
    /// 事实来源：SDF_GetDeviceInfo。
    /// </summary>
    public IReadOnlySet<string> Algorithms { get; init; } = new HashSet<string>();

    /// <summary>算法能力查询</summary>
    public bool Supports(string algorithm) => Algorithms.Contains(algorithm);
}

/// <summary>准入校验结果。</summary>
public sealed record AdmissionResult(bool Admitted, IReadOnlyList<string> Failures)
{
    public static AdmissionResult Ok() => new(true, []);
    public static AdmissionResult Fail(params string[] failures) => new(false, failures);
}

/// <summary>
/// Provider 最小能力准入校验（需求 V2.10 §2.6 最小能力准入集）。
/// 在 Provider 注册与启用流程中强制执行，校验结果必须记录审计。
/// </summary>
public static class ProviderCapabilitiesValidator
{
    /// <summary>准入所需的最低算法能力集合</summary>
    public static readonly string[] RequiredAlgorithms =
    [
        "SM2", "SM3", "SM4", "HMAC_SM3", "RANDOM",
    ];

    /// <summary>执行准入校验。任一不满足即拒绝准入，不得降级使用。</summary>
    public static AdmissionResult ValidateForAdmission(ProviderCapabilities capabilities)
    {
        var failures = new List<string>();

        // 1. 平台密钥体系（L2 KEK Wrap L3 Data Key；Software 模式 WrappedMaterial 存储）硬依赖
        if (!capabilities.CanWrapKey)
        {
            failures.Add("CanWrapKey 未声明（准入强制项）");
        }

        if (!capabilities.CanUnwrapKey)
        {
            failures.Add("CanUnwrapKey 未声明（准入强制项）");
        }

        // 2. 算法能力覆盖需求 §3.1
        foreach (var alg in RequiredAlgorithms)
        {
            if (!capabilities.Supports(alg))
            {
                failures.Add($"缺少必需算法能力: {alg}");
            }
        }

        return failures.Count == 0 ? AdmissionResult.Ok() : AdmissionResult.Fail([.. failures]);
    }
}
