namespace CryptoPlatform.Crypto.Abstractions;

/// <summary>
/// 密码运算 Provider 统一抽象。
/// 所有实现（Software / HSM）均实现此接口。
/// </summary>
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
}
