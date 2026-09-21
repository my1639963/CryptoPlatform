# 37. HSM Adapter 接口规范

> 本章节提供 HSM 适配器项目的接口规范、厂商 SDK 隔离原则、连接池设计和错误码映射。

## 37.1 项目结构

```text
src/
├── CryptoPlatform.Crypto/
│   ├── Abstractions/           ← ICryptoProvider 接口（第36章）
│   ├── Software/               ← SoftwareCryptoProvider
│   │   └── SoftwareCryptoProvider.cs
│   └── Hsm/
│       ├── HsmCryptoProvider.cs       ← 统一 HSM Provider 入口
│       ├── HsmConnectionPool.cs       ← 连接池
│       └── Adapters/
│           ├── IHsmSdkAdapter.cs      ← 厂商 SDK 适配接口
│           ├── VendorXAdapter.cs       ← 厂商 X 实现
│           └── VendorYAdapter.cs       ← 厂商 Y 实现
```

## 37.2 厂商 SDK 隔离接口

```csharp
namespace CryptoPlatform.Crypto.Hsm.Adapters;

/// 厂商 SDK 防腐层接口。
/// 每个 HSM 厂商实现此接口，将厂商特定 API 转换为平台统一模型。
/// 业务层代码禁止直接引用任何厂商 SDK 命名空间。
public interface IHsmSdkAdapter
{
    string Vendor { get; }
    string Model { get; }

    // 连接管理
    Task ConnectAsync(HsmConnectionConfig config, CancellationToken ct);
    Task DisconnectAsync(CancellationToken ct);
    bool IsConnected { get; }

    // 密钥生成
    Task<HsmKeyHandle> GenerateKeyAsync(string algorithm, HsmKeyAttributes attrs, CancellationToken ct);
    Task<HsmKeyPairHandle> GenerateKeyPairAsync(string algorithm, HsmKeyAttributes attrs, CancellationToken ct);

    // 密码运算
    Task<byte[]> EncryptAsync(HsmKeyHandle key, byte[] plaintext, HsmCryptoParams parms, CancellationToken ct);
    Task<byte[]> DecryptAsync(HsmKeyHandle key, byte[] ciphertext, HsmCryptoParams parms, CancellationToken ct);
    Task<byte[]> SignAsync(HsmKeyHandle key, byte[] data, HsmCryptoParams parms, CancellationToken ct);
    Task<bool> VerifyAsync(HsmKeyHandle key, byte[] data, byte[] signature, HsmCryptoParams parms, CancellationToken ct);

    // 哈希
    Task<byte[]> HashAsync(string algorithm, byte[] data, CancellationToken ct);

    // HMAC
    Task<byte[]> HmacAsync(HsmKeyHandle key, byte[] data, CancellationToken ct);

    // 随机数
    Task<byte[]> GenerateRandomAsync(int length, CancellationToken ct);

    // 密钥管理
    Task<HsmKeyInfo> GetKeyInfoAsync(HsmKeyHandle key, CancellationToken ct);
    Task DestroyKeyAsync(HsmKeyHandle key, CancellationToken ct);

    // 健康
    Task<HsmHealthInfo> CheckHealthAsync(CancellationToken ct);
}
```

## 37.3 HSM DTO

```csharp
namespace CryptoPlatform.Crypto.Hsm;

public sealed class HsmConnectionConfig
{
    public string Endpoint { get; init; } = "";
    public int Port { get; init; }
    public string? Credential { get; init; }    // 加密存储的认证凭据
    public int TimeoutMs { get; init; } = 5000;
    public int MaxConnections { get; init; } = 10;
}

public sealed record HsmKeyHandle(string Value);       // 不透明句柄
public sealed record HsmKeyPairHandle(string PrivateKeyHandle, string PublicKeyHandle);

public sealed class HsmKeyAttributes
{
    public string? Label { get; init; }
    public int KeySize { get; init; }
    public bool Extractable { get; init; }
    public string[] KeyUsages { get; init; } = Array.Empty<string>();
}

public sealed class HsmCryptoParams
{
    public string? Mode { get; init; }
    public byte[]? Iv { get; init; }
    public byte[]? Aad { get; init; }
    public string? Padding { get; init; }
    public string? SignatureFormat { get; init; }
    public string? CipherFormat { get; init; }
}

public sealed class HsmKeyInfo
{
    public bool Exists { get; init; }
    public string? Algorithm { get; init; }
    public DateTime? CreatedAt { get; init; }
}

public sealed class HsmHealthInfo
{
    public bool Healthy { get; init; }
    public string? Message { get; init; }
    public TimeSpan Latency { get; init; }
}
```

## 37.4 连接池设计

```csharp
namespace CryptoPlatform.Crypto.Hsm;

public sealed class HsmConnectionPool : IAsyncDisposable
{
    private readonly IHsmSdkAdapter _adapter;
    private readonly HsmConnectionConfig _config;
    private readonly Channel<HsmSession> _available;
    private readonly SemaphoreSlim _gate;
    private readonly ILogger _logger;
    private int _created;

    public HsmConnectionPool(IHsmSdkAdapter adapter, HsmConnectionConfig config, ILogger logger)
    {
        _adapter = adapter;
        _config = config;
        _gate = new SemaphoreSlim(config.MaxConnections, config.MaxConnections);
        _available = Channel.CreateBounded<HsmSession>(config.MaxConnections);
        _logger = logger;
    }

    public async Task<HsmSession> AcquireAsync(CancellationToken ct)
    {
        if (!_available.Reader.TryRead(out var session))
        {
            await _gate.WaitAsync(ct);
            try
            {
                if (!_available.Reader.TryRead(out session))
                {
                    session = await CreateSessionAsync(ct);
                    Interlocked.Increment(ref _created);
                }
            }
            catch { _gate.Release(); throw; }
        }
        return session;
    }

    public async Task ReleaseAsync(HsmSession session)
    {
        if (session.IsHealthy)
            await _available.Writer.WriteAsync(session);
        else
        {
            await session.DisposeAsync();
            Interlocked.Decrement(ref _created);
        }
        _gate.Release();
    }

    private async Task<HsmSession> CreateSessionAsync(CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        await _adapter.ConnectAsync(_config, ct);
        _logger.LogInformation("HSM 连接建立，耗时 {Elapsed}ms", sw.ElapsedMilliseconds);
        return new HsmSession(_adapter);
    }

    public async ValueTask DisposeAsync()
    {
        while (_available.Reader.TryRead(out var session))
            await session.DisposeAsync();
        _gate.Dispose();
    }
}

public sealed class HsmSession : IAsyncDisposable
{
    public IHsmSdkAdapter Adapter { get; }
    public bool IsHealthy => Adapter.IsConnected;
    public DateTime AcquiredAt { get; init; } = DateTime.UtcNow;

    public HsmSession(IHsmSdkAdapter adapter) => Adapter = adapter;
    public ValueTask DisposeAsync() => Adapter.DisconnectAsync(CancellationToken.None);
}
```

## 37.5 错误码映射

```csharp
namespace CryptoPlatform.Crypto.Hsm;

/// 将厂商特定错误码映射为平台统一错误码。
/// 每个厂商 Adapter 维护自己的映射表。
public static class HsmErrorCodeMapper
{
    // 示例映射（实际需根据厂商 SDK 文档填充）
    private static readonly Dictionary<int, string> GenericMap = new()
    {
        { 0x0000, ProviderErrorCodes.PROVIDER_UNAVAILABLE },
        { 0x0001, ProviderErrorCodes.PROVIDER_TIMEOUT },
        { 0x0002, ProviderErrorCodes.PROVIDER_KEY_NOT_FOUND },
        { 0x0003, ProviderErrorCodes.PROVIDER_KEY_INVALID },
        { 0x0004, ProviderErrorCodes.PROVIDER_OPERATION_FAILED },
        { 0x0005, ProviderErrorCodes.PROVIDER_DEVICE_OFFLINE },
        { 0x0006, ProviderErrorCodes.PROVIDER_AUTH_FAILED },
    };

    public static string Map(int vendorCode)
        => GenericMap.TryGetValue(vendorCode, out var code)
            ? code
            : ProviderErrorCodes.PROVIDER_OPERATION_FAILED;
}
```

## 37.6 HsmCryptoProvider 实现骨架

```csharp
namespace CryptoPlatform.Crypto.Hsm;

public sealed class HsmCryptoProvider : ICryptoProvider
{
    public string ProviderType => "HSM";

    private readonly HsmConnectionPool _pool;
    private readonly IHsmSdkAdapter _adapter;
    private readonly ILogger<HsmCryptoProvider> _logger;

    public HsmCryptoProvider(HsmConnectionPool pool, IHsmSdkAdapter adapter, ILogger<HsmCryptoProvider> logger)
    {
        _pool = pool; _adapter = adapter; _logger = logger;
    }

    public async Task<ProviderKeyResult> GenerateKeyAsync(KeyAlgorithm algorithm, CancellationToken ct)
    {
        var session = await _pool.AcquireAsync(ct);
        try
        {
            var handle = await _adapter.GenerateKeyAsync(algorithm.ToString(), new HsmKeyAttributes(), ct);
            return new ProviderKeyResult
            {
                ProviderKeyRef = handle.Value,
                Fingerprint = await ComputeFingerprintAsync(handle, ct),
                DeviceId = null // 由 Router 填充
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HSM GenerateKey 失败: {Algorithm}", algorithm);
            throw new CryptoProviderException(ProviderErrorCodes.PROVIDER_OPERATION_FAILED, ex.Message, ex);
        }
        finally { await _pool.ReleaseAsync(session); }
    }

    // Encrypt / Decrypt / Sign / Verify 同理：Acquire → 调用 → Release
    // 此处省略重复结构，实际编码时按相同模式实现

    public Task<ProviderHealth> CheckHealthAsync(CancellationToken ct)
        => _adapter.CheckHealthAsync(ct)
            .ContinueWith(t => new ProviderHealth
            {
                Status = t.Result.Healthy ? "HEALTHY" : "UNHEALTHY",
                Message = t.Result.Message,
                Latency = t.Result.Latency
            }, ct);

    public IReadOnlySet<string> GetCapabilities()
        => new HashSet<string> { "SM2_SIGN", "SM2_VERIFY", "SM2_ENCRYPT", "SM2_DECRYPT",
            "SM4_ECB", "SM4_CBC", "SM4_CTR", "SM4_GCM", "SM3", "HMAC_SM3", "RANDOM" };

    private Task<string> ComputeFingerprintAsync(HsmKeyHandle handle, CancellationToken ct)
        => _adapter.HashAsync("SM3", Array.Empty<byte>(), ct)
            .ContinueWith(t => Convert.ToHexString(t.Result)[..32], ct);
}
```
