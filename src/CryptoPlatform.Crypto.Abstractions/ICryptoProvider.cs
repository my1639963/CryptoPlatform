namespace CryptoPlatform.Crypto.Abstractions;

/// <summary>
/// 密码运算 Provider 统一抽象。
/// 所有实现（Software / HSM）均实现此接口。
/// </summary>
/// <remarks>
/// <para>
/// V2.0（2026-09-30，基于 GM/T 0018-2023 重设计，见实现规范-CryptoProvider接口.md §36.3）：
/// 新增身份与能力契约 <see cref="ProviderId"/>、<see cref="StorageMode"/>、<see cref="Capabilities"/>
/// 与 <see cref="SelfTestAsync"/>。阶段一以默认接口成员（DIM）增量落地，保持既有实现可编译；
/// 阶段二迁移时删除全部 DIM 兼容成员，上述成员变为必须实现。
/// </para>
/// <para>
/// 设备侧语义（密钥对索引 / KEK 索引 / 会话密钥句柄、GM/T 0018-2023 生产函数全集）
/// 由 <see cref="Sdf.ISdfDevice"/> / <see cref="Sdf.ISdfSession"/> 契约承载，
/// 本接口不得直接暴露 SDF 句柄或厂商扩展。
/// </para>
/// </remarks>
public interface ICryptoProvider
{
    /// <summary>Provider 类型标识："SOFTWARE" / "HSM"</summary>
    string ProviderType { get; }

    /// <summary>Provider 实例名称（日志/路由用）</summary>
    string ProviderName { get; }

    // ── 密钥生成 ──

    /// <summary>生成对称密钥（SM4 / HMAC-SM3）</summary>
    Task<ProviderKeyResult> GenerateKeyAsync(KeyAlgorithm algorithm, CancellationToken ct);

    /// <summary>生成非对称密钥对（SM2）</summary>
    Task<ProviderKeyPairResult> GenerateKeyPairAsync(KeyPairAlgorithm algorithm, CancellationToken ct);

    // ── 加解密 ──

    /// <summary>加密</summary>
    Task<CryptoResult> EncryptAsync(
        string providerKeyRef, ReadOnlyMemory<byte> plaintext,
        CryptoParameters parameters, CancellationToken ct);

    /// <summary>解密</summary>
    Task<CryptoResult> DecryptAsync(
        string providerKeyRef, ReadOnlyMemory<byte> ciphertext,
        CryptoParameters parameters, CancellationToken ct);

    // ── 签名/验签 ──

    /// <summary>签名</summary>
    Task<SignResult> SignAsync(
        string providerKeyRef, ReadOnlyMemory<byte> data,
        CryptoParameters parameters, CancellationToken ct);

    /// <summary>验签</summary>
    Task<bool> VerifyAsync(
        string providerKeyRef, ReadOnlyMemory<byte> data,
        ReadOnlyMemory<byte> signature, CryptoParameters parameters, CancellationToken ct);

    // ── 杂凑 / MAC ──

    /// <summary>杂凑计算</summary>
    Task<byte[]> HashAsync(string algorithm, ReadOnlyMemory<byte> data, CancellationToken ct);

    /// <summary>HMAC 计算</summary>
    Task<byte[]> HmacAsync(string providerKeyRef, ReadOnlyMemory<byte> data, CancellationToken ct);

    // ── 随机数 ──

    /// <summary>生成安全随机数</summary>
    Task<RandomResult> GenerateRandomAsync(int length, CancellationToken ct);

    // ── 密钥管理 ──

    /// <summary>查询密钥信息</summary>
    Task<ProviderKeyInfo> GetKeyInfoAsync(string providerKeyRef, CancellationToken ct);

    /// <summary>销毁密钥</summary>
    Task DestroyKeyAsync(string providerKeyRef, CancellationToken ct);

    // ── 健康检查 ──

    /// <summary>健康检查</summary>
    Task<ProviderHealth> CheckHealthAsync(CancellationToken ct);

    // ── 能力声明 ──

    /// <summary>获取 Provider 支持的算法能力集合</summary>
    IReadOnlySet<string> GetCapabilities();

    // ═══════════════════════════════════════════════════════════
    // V2.0 增量契约（阶段一：默认接口成员；阶段二转为必须实现）
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Provider 唯一标识，对应 sys_crypto_device.DeviceId。
    /// 阶段一默认回落到 <see cref="ProviderType"/>。
    /// </summary>
    string ProviderId => ProviderType;

    /// <summary>
    /// 密钥存储模式（V2.10 §2.6）。部署模式互斥：Software 或 Hsm。
    /// 阶段一默认由 <see cref="ProviderType"/> 推导。
    /// </summary>
    KeyStorageMode StorageMode => string.Equals(ProviderType, "HSM", StringComparison.OrdinalIgnoreCase)
        ? KeyStorageMode.Hsm
        : KeyStorageMode.Software;

    /// <summary>
    /// 强类型能力声明（V2.10 §2.6 十项能力 + 算法集合）。
    /// <para>
    /// 默认实现**故意抛出异常**（fail-fast）：能力未声明即未准入，禁止以空能力或
    /// 全能力等可被误用的默认值上线（V2.10 §1.7.13 能力声明与显式降级禁止）。
    /// 阶段一新增实现必须覆写本成员。
    /// </para>
    /// </summary>
    ProviderCapabilities Capabilities =>
        throw new NotImplementedException(
            "Provider 未实现能力声明（ProviderCapabilities，需求 V2.10 §2.6）。" +
            "能力声明是准入强制项，禁止依赖默认实现上线。");

    /// <summary>
    /// 密码模块自检（V2.10 §3.1.6：覆盖 SM2/SM3/SM4/HMAC-SM3/随机数，
    /// 使用 GM/T 标准测试向量；失败时拒绝所有密码运算并记录安全事件）。
    /// 默认实现返回 NOT_SUPPORTED 语义结果；生产实现必须覆写。
    /// </summary>
    Task<SelfTestResult> SelfTestAsync(SelfTestRequest request, CancellationToken ct) =>
        Task.FromResult(new SelfTestResult
        {
            OverallStatus = "NOT_SUPPORTED",
            Items = [new SelfTestItem("SELF_TEST", false, "Provider 未实现自检（阶段一默认实现）")],
        });
}
