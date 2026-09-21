# 36. ICryptoProvider 完整接口及 DTO

> 本章节提供 Provider 抽象层的完整接口定义、所有请求/响应 DTO、枚举类型和错误码。

## 36.1 ICryptoProvider 接口

```csharp
namespace CryptoPlatform.Crypto.Abstractions;

public interface ICryptoProvider
{
    string ProviderType { get; }  // "HSM" / "SOFTWARE"

    // 密钥生成
    Task<ProviderKeyResult> GenerateKeyAsync(KeyAlgorithm algorithm, CancellationToken ct);
    Task<ProviderKeyPairResult> GenerateKeyPairAsync(KeyAlgorithm algorithm, CancellationToken ct);

    // 密码运算
    Task<CryptoResult> EncryptAsync(string providerKeyRef, ReadOnlyMemory<byte> plaintext, CryptoParameters parameters, CancellationToken ct);
    Task<CryptoResult> DecryptAsync(string providerKeyRef, ReadOnlyMemory<byte> ciphertext, CryptoParameters parameters, CancellationToken ct);
    Task<SignResult> SignAsync(string providerKeyRef, ReadOnlyMemory<byte> digest, CryptoParameters parameters, CancellationToken ct);
    Task<bool> VerifyAsync(string providerKeyRef, ReadOnlyMemory<byte> digest, ReadOnlyMemory<byte> signature, CryptoParameters parameters, CancellationToken ct);

    // 哈希
    Task<byte[]> HashAsync(string algorithm, ReadOnlyMemory<byte> data, CancellationToken ct);

    // HMAC
    Task<byte[]> HmacAsync(string providerKeyRef, ReadOnlyMemory<byte> data, CancellationToken ct);

    // 随机数
    Task<RandomResult> GenerateRandomAsync(int length, CancellationToken ct);

    // 密钥管理
    Task<ProviderKeyInfo> GetKeyInfoAsync(string providerKeyRef, CancellationToken ct);
    Task DestroyKeyAsync(string providerKeyRef, CancellationToken ct);

    // 健康检查
    Task<ProviderHealth> CheckHealthAsync(CancellationToken ct);

    // 能力声明
    IReadOnlySet<string> GetCapabilities();
}
```

## 36.2 ICryptoProviderRouter 接口

```csharp
namespace CryptoPlatform.Crypto.Abstractions;

public interface ICryptoProviderRouter
{
    /// 获取系统默认 Provider
    ICryptoProvider GetDefaultProvider();

    /// 根据 KeyVersion 解析对应 Provider
    ICryptoProvider ResolveProvider(SysKeyVersion version);

    /// 根据 DeviceId 解析指定 Provider
    ICryptoProvider ResolveByDevice(string deviceId);
}
```

## 36.3 DTO 定义

```csharp
namespace CryptoPlatform.Crypto.Abstractions;

public sealed class CryptoParameters
{
    public string? Mode { get; init; }       // GCM / CBC / CTR / ECB
    public byte[]? Nonce { get; init; }
    public byte[]? Aad { get; init; }
    public byte[]? Tag { get; init; }
    public string? Padding { get; init; }    // PKCS7 / NONE
    public string? SignatureFormat { get; init; } // DER / RAW
    public string? CipherFormat { get; init; }    // C1C3C2 / C1C2C3
}

public sealed class CryptoResult
{
    public byte[] Ciphertext { get; init; } = Array.Empty<byte>();
    public byte[] Plaintext { get; init; } = Array.Empty<byte>();
    public byte[]? Nonce { get; init; }
    public byte[]? Tag { get; init; }
}

public sealed class SignResult
{
    public byte[] Signature { get; init; } = Array.Empty<byte>();
    public string Format { get; init; } = "";
}

public sealed class RandomResult
{
    public byte[] Bytes { get; init; } = Array.Empty<byte>();
}

public sealed class ProviderKeyResult
{
    public string ProviderKeyRef { get; init; } = "";
    public string? PublicKeyMaterial { get; init; }
    public string Fingerprint { get; init; } = "";
    public string? DeviceId { get; init; }
    public string? EncryptedKeyMaterial { get; init; }  // 软件模式
}

public sealed class ProviderKeyPairResult
{
    public string ProviderKeyRef { get; init; } = "";
    public string PublicKeyMaterial { get; init; } = "";
    public string Fingerprint { get; init; } = "";
    public string? DeviceId { get; init; }
}

public sealed class ProviderKeyInfo
{
    public string ProviderKeyRef { get; init; } = "";
    public bool Exists { get; init; }
    public string? Algorithm { get; init; }
    public DateTime? CreatedAt { get; init; }
}

public sealed class ProviderHealth
{
    public string Status { get; init; } = "UNKNOWN";  // HEALTHY / DEGRADED / UNHEALTHY
    public string? Message { get; init; }
    public TimeSpan Latency { get; init; }
}
```

## 36.4 枚举

```csharp
namespace CryptoPlatform.Crypto.Abstractions;

public enum KeyAlgorithm
{
    SM2,
    SM4_128,
    HMAC_SM3
}
```

## 36.5 Provider 错误码

```csharp
namespace CryptoPlatform.Crypto.Abstractions;

public static class ProviderErrorCodes
{
    public const string PROVIDER_UNAVAILABLE = "PROVIDER_UNAVAILABLE";
    public const string PROVIDER_TIMEOUT = "PROVIDER_TIMEOUT";
    public const string PROVIDER_KEY_NOT_FOUND = "PROVIDER_KEY_NOT_FOUND";
    public const string PROVIDER_KEY_INVALID = "PROVIDER_KEY_INVALID";
    public const string PROVIDER_OPERATION_FAILED = "PROVIDER_OPERATION_FAILED";
    public const string PROVIDER_DEVICE_OFFLINE = "PROVIDER_DEVICE_OFFLINE";
    public const string PROVIDER_AUTH_FAILED = "PROVIDER_AUTH_FAILED";
    public const string PROVIDER_CAPABILITY_NOT_SUPPORTED = "PROVIDER_CAPABILITY_NOT_SUPPORTED";
}

public class CryptoProviderException : Exception
{
    public string ErrorCode { get; }
    public CryptoProviderException(string errorCode, string message)
        : base(message) => ErrorCode = errorCode;
    public CryptoProviderException(string errorCode, string message, Exception inner)
        : base(message, inner) => ErrorCode = errorCode;
}
```

## 36.6 ProviderRouter 实现骨架

```csharp
namespace CryptoPlatform.Crypto;

public sealed class CryptoProviderRouter : ICryptoProviderRouter
{
    private readonly IEnumerable<ICryptoProvider> _providers;
    private readonly CryptoPlatformDbContext _db;
    private readonly ILogger<CryptoProviderRouter> _logger;

    public CryptoProviderRouter(
        IEnumerable<ICryptoProvider> providers,
        CryptoPlatformDbContext db,
        ILogger<CryptoProviderRouter> logger)
    {
        _providers = providers;
        _db = db;
        _logger = logger;
    }

    public ICryptoProvider GetDefaultProvider()
    {
        // 读取系统配置中的默认 Provider 类型
        return _providers.First();
    }

    public ICryptoProvider ResolveProvider(SysKeyVersion version)
    {
        var type = version.ProviderType;
        var provider = _providers.FirstOrDefault(p => p.ProviderType == type)
            ?? throw new CryptoProviderException(
                ProviderErrorCodes.PROVIDER_UNAVAILABLE,
                $"Provider {type} 未注册");
        return provider;
    }

    public ICryptoProvider ResolveByDevice(string deviceId)
    {
        var device = _db.CryptoDevices.FirstOrDefault(d => d.DeviceId == deviceId)
            ?? throw new CryptoProviderException(
                ProviderErrorCodes.PROVIDER_DEVICE_OFFLINE,
                $"设备 {deviceId} 未注册");
        return _providers.First(p => p.ProviderType == device.ProviderType);
    }
}
```

## 36.7 DI 注册

```csharp
public static class CryptoProviderServiceCollectionExtensions
{
    public static IServiceCollection AddCryptoProviders(this IServiceCollection services)
    {
        services.AddSingleton<ICryptoProviderRouter, CryptoProviderRouter>();
        // 具体 Provider 由各项目注册：
        // services.AddSingleton<ICryptoProvider, SoftwareCryptoProvider>();
        // services.AddSingleton<ICryptoProvider, HsmCryptoProvider>();
        return services;
    }
}
```
