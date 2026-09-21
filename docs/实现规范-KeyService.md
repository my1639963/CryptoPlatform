# 34. KeyService 完整代码骨架

> 本章节提供密钥管理服务的完整接口定义、实现类骨架、事务边界、异常处理和依赖注入注册。

## 34.1 请求/响应 DTO

```csharp
namespace CryptoPlatform.Application.Keys;

public sealed record CreateKeyCommand(
    string KeyType,       // SM2 / SM4 / HMAC / ROOT
    string KeyUsage,      // SIGN / ENCRYPT / MAC / WRAP
    string Name,
    DateTime? ExpiresAt,
    string? Description);

public sealed record RotateKeyCommand(string? Description);

public sealed record DestroyKeyCommand(string Reason, bool DestroyAllVersions);

public sealed record ImportKeyCommand(
    string KeyType,
    string KeyUsage,
    string Name,
    string Format,           // PEM / DER / RAW
    string? PublicKeyData,   // Base64（非对称密钥）
    string? PrivateKeyData,  // Base64（非对称密钥，加密传输）
    string? SymmetricData);  // Base64（对称密钥，加密传输）

public sealed record KeyDescriptor(
    string KeyId,
    string OwnerAppId,
    string KeyType,
    string KeyUsage,
    string Name,
    string Status,
    int? CurrentVersion,
    DateTime? ExpiresAt,
    DateTime CreatedAt,
    DateTime? ActivatedAt,
    string? Description);

public sealed record KeyVersionDescriptor(
    string KeyId,
    int VersionNo,
    string ProviderType,
    string? DeviceId,
    string? PublicKeyMaterial,
    string Fingerprint,
    string Status,
    DateTime CreatedAt,
    DateTime? ActivatedAt,
    DateTime? RotatedAt,
    DateTime? ExpiresAt);
```

## 34.2 IKeyService 接口

```csharp
namespace CryptoPlatform.Application.Keys;

public interface IKeyService
{
    Task<KeyDescriptor> CreateAsync(CreateKeyCommand command, CancellationToken ct);
    Task<KeyDescriptor> GetAsync(string keyId, CancellationToken ct);
    Task<KeyDescriptor> ActivateAsync(string keyId, CancellationToken ct);
    Task<KeyDescriptor> RotateAsync(string keyId, RotateKeyCommand command, CancellationToken ct);
    Task DisableAsync(string keyId, CancellationToken ct);
    Task RevokeAsync(string keyId, CancellationToken ct);
    Task DestroyAsync(string keyId, DestroyKeyCommand command, CancellationToken ct);
    Task<KeyVersionDescriptor> GetVersionAsync(string keyId, int versionNo, CancellationToken ct);
    Task<IReadOnlyList<KeyVersionDescriptor>> GetVersionsAsync(string keyId, CancellationToken ct);
    Task<KeyDescriptor> ImportAsync(ImportKeyCommand command, CancellationToken ct);
}
```

## 34.3 KeyService 实现骨架

```csharp
namespace CryptoPlatform.Application.Keys;

public sealed class KeyService : IKeyService
{
    private readonly CryptoPlatformDbContext _db;
    private readonly ICryptoProviderRouter _providerRouter;
    private readonly IAuditService _audit;
    private readonly ISecurityEventService _securityEvents;
    private readonly IIdGenerator _idGen;
    private readonly ILogger<KeyService> _logger;

    public KeyService(
        CryptoPlatformDbContext db,
        ICryptoProviderRouter providerRouter,
        IAuditService audit,
        ISecurityEventService securityEvents,
        IIdGenerator idGen,
        ILogger<KeyService> logger)
    {
        _db = db;
        _providerRouter = providerRouter;
        _audit = audit;
        _securityEvents = securityEvents;
        _idGen = idGen;
        _logger = logger;
    }

    // ────────────────── Create ──────────────────

    public async Task<KeyDescriptor> CreateAsync(CreateKeyCommand cmd, CancellationToken ct)
    {
        ValidateKeyTypeAndUsage(cmd.KeyType, cmd.KeyUsage);

        var keyId = _idGen.NewKeyId();
        var key = new SysKey
        {
            KeyId = keyId,
            OwnerAppId = CurrentContext.AppId,
            KeyType = cmd.KeyType,
            KeyUsage = cmd.KeyUsage,
            Name = cmd.Name,
            Status = nameof(KeyStatus.CREATED),
            ExpiresAt = cmd.ExpiresAt,
            Description = cmd.Description,
            CreatedAt = DateTime.UtcNow,
            ConcurrencyStamp = Guid.NewGuid().ToString("N")
        };

        // 阶段 1：写入逻辑密钥（CREATED）
        _db.Keys.Add(key);
        await _db.SaveChangesAsync(ct);

        try
        {
            // 阶段 2：调用 Provider 生成密钥材料
            var provider = _providerRouter.GetDefaultProvider();
            var result = cmd.KeyType == nameof(KeyType.SM2)
                ? await GenerateKeyPairAsync(provider, cmd, ct)
                : await GenerateSymmetricKeyAsync(provider, cmd, ct);

            // 阶段 3：写入 KeyVersion
            var version = new SysKeyVersion
            {
                KeyId = keyId,
                VersionNo = 1,
                ProviderType = provider.ProviderType,
                DeviceId = result.DeviceId,
                ProviderKeyRef = result.ProviderKeyRef,
                PublicKeyMaterial = result.PublicKeyMaterial,
                EncryptedKeyMaterial = result.EncryptedKeyMaterial,
                Fingerprint = result.Fingerprint,
                Status = nameof(KeyStatus.CREATED),
                CreatedAt = DateTime.UtcNow,
                CreatedRequestId = CurrentContext.RequestId,
                ConcurrencyStamp = Guid.NewGuid().ToString("N")
            };

            _db.KeyVersions.Add(version);
            key.CurrentVersion = 1;
            await _db.SaveChangesAsync(ct);

            await _audit.LogAsync("KEY_CREATE", keyId, 1, "SUCCESS", ct);
            return ToDescriptor(key);
        }
        catch (Exception ex)
        {
            // Provider 成功但 DB 失败 → 补偿
            _logger.LogError(ex, "Key {KeyId} 创建后写入失败，进入补偿", keyId);
            key.Status = nameof(KeyStatus.REVOKED);
            await _db.SaveChangesAsync(CancellationToken.None);
            await _securityEvents.RaiseAsync("KEY_CREATE_FAILED", keyId, ex.Message, ct);
            throw;
        }
    }

    // ────────────────── Activate ──────────────────

    public async Task<KeyDescriptor> ActivateAsync(string keyId, CancellationToken ct)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var key = await LoadKeyForUpdateAsync(keyId, ct);
        var version = await LoadCurrentVersionForUpdateAsync(keyId, key.CurrentVersion ?? 1, ct);

        if (version.Status != nameof(KeyStatus.CREATED))
            throw new BusinessException("KEY_INVALID_STATE", "仅 CREATED 状态可激活");

        // Provider 自检
        var provider = _providerRouter.ResolveProvider(version);
        var health = await provider.CheckHealthAsync(ct);
        if (health.Status != "HEALTHY")
            throw new BusinessException("PROVIDER_UNAVAILABLE", "Provider 不可用");

        version.Status = nameof(KeyStatus.ACTIVE);
        version.ActivatedAt = DateTime.UtcNow;
        key.Status = nameof(KeyStatus.ACTIVE);
        key.ActivatedAt = DateTime.UtcNow;

        await _audit.LogAsync("KEY_ACTIVATE", keyId, version.VersionNo, "SUCCESS", ct);
        await tx.CommitAsync(ct);

        return ToDescriptor(key);
    }

    // ────────────────── Rotate ──────────────────

    public async Task<KeyDescriptor> RotateAsync(string keyId, RotateKeyCommand cmd, CancellationToken ct)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var key = await LoadKeyForUpdateAsync(keyId, ct);
        if (key.Status != nameof(KeyStatus.ACTIVE))
            throw new BusinessException("KEY_NOT_ACTIVE", "仅 ACTIVE 密钥可轮换");

        var oldVersionNo = key.CurrentVersion!.Value;
        var oldVersion = await LoadCurrentVersionForUpdateAsync(keyId, oldVersionNo, ct);

        // 生成新版本
        var newVersionNo = oldVersionNo + 1;
        var provider = _providerRouter.ResolveProvider(oldVersion);

        var result = await GenerateKeyByVersionAsync(provider, key, ct);

        var newVersion = new SysKeyVersion
        {
            KeyId = keyId,
            VersionNo = newVersionNo,
            ProviderType = provider.ProviderType,
            DeviceId = result.DeviceId,
            ProviderKeyRef = result.ProviderKeyRef,
            PublicKeyMaterial = result.PublicKeyMaterial,
            EncryptedKeyMaterial = result.EncryptedKeyMaterial,
            Fingerprint = result.Fingerprint,
            Status = nameof(KeyStatus.ACTIVE),
            ActivatedAt = DateTime.UtcNow,
            RotatedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            CreatedRequestId = CurrentContext.RequestId,
            ConcurrencyStamp = Guid.NewGuid().ToString("N")
        };

        _db.KeyVersions.Add(newVersion);

        // 旧版本转 ROTATED
        oldVersion.Status = nameof(KeyStatus.ROTATED);
        oldVersion.RotatedAt = DateTime.UtcNow;

        key.CurrentVersion = newVersionNo;

        await _audit.LogAsync("KEY_ROTATE", keyId, newVersionNo, "SUCCESS", ct);
        await tx.CommitAsync(ct);

        return ToDescriptor(key);
    }

    // ────────────────── Destroy ──────────────────

    public async Task DestroyAsync(string keyId, DestroyKeyCommand cmd, CancellationToken ct)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var key = await LoadKeyForUpdateAsync(keyId, ct);
        if (key.Status == nameof(KeyStatus.DESTROYED))
            throw new BusinessException("KEY_ALREADY_DESTROYD", "密钥已销毁");

        var versions = await _db.KeyVersions
            .Where(v => v.KeyId == keyId && v.Status != nameof(KeyStatus.DESTROYED))
            .ToListAsync(ct);

        foreach (var version in versions)
        {
            var provider = _providerRouter.ResolveProvider(version);
            try
            {
                await provider.DestroyKeyAsync(version.ProviderKeyRef, ct);
                version.Status = nameof(KeyStatus.DESTROYED);
                version.DestroyedAt = DateTime.UtcNow;
                version.DestroyResult = "Success";
            }
            catch (Exception ex)
            {
                version.DestroyResult = "Failed";
                _logger.LogError(ex, "Key {KeyId} V{Ver} Provider 销毁失败", keyId, version.VersionNo);
                throw;
            }
        }

        key.Status = nameof(KeyStatus.DESTROYED);
        key.DestroyedAt = DateTime.UtcNow;

        await _audit.LogAsync("KEY_DESTROY", keyId, null, "SUCCESS", ct);
        await tx.CommitAsync(ct);
    }

    // ────────────────── Disable / Revoke ──────────────────

    public async Task DisableAsync(string keyId, CancellationToken ct)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        var key = await LoadKeyForUpdateAsync(keyId, ct);
        if (key.Status != nameof(KeyStatus.ACTIVE))
            throw new BusinessException("KEY_NOT_ACTIVE", "仅 ACTIVE 可停用");
        key.Status = nameof(KeyStatus.DISABLED);
        await _audit.LogAsync("KEY_DISABLE", keyId, null, "SUCCESS", ct);
        await tx.CommitAsync(ct);
    }

    public async Task RevokeAsync(string keyId, CancellationToken ct)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        var key = await LoadKeyForUpdateAsync(keyId, ct);
        var allowed = new[] { nameof(KeyStatus.CREATED), nameof(KeyStatus.ACTIVE), nameof(KeyStatus.DISABLED) };
        if (!allowed.Contains(key.Status))
            throw new BusinessException("KEY_REVOKE_NOT_ALLOWED", $"当前状态 {key.Status} 不允许撤销");
        key.Status = nameof(KeyStatus.REVOKED);
        await _audit.LogAsync("KEY_REVOKE", keyId, null, "SUCCESS", ct);
        await tx.CommitAsync(ct);
    }

    // ────────────────── 查询 ──────────────────

    public async Task<KeyDescriptor> GetAsync(string keyId, CancellationToken ct)
    {
        var key = await _db.Keys.FirstOrDefaultAsync(k => k.KeyId == keyId, ct)
            ?? throw new BusinessException("KEY_NOT_FOUND", "密钥不存在");
        return ToDescriptor(key);
    }

    public async Task<KeyVersionDescriptor> GetVersionAsync(string keyId, int versionNo, CancellationToken ct)
    {
        var v = await _db.KeyVersions
            .FirstOrDefaultAsync(x => x.KeyId == keyId && x.VersionNo == versionNo, ct)
            ?? throw new BusinessException("KEY_VERSION_NOT_FOUND", "版本不存在");
        return ToVersionDescriptor(v);
    }

    public async Task<IReadOnlyList<KeyVersionDescriptor>> GetVersionsAsync(string keyId, CancellationToken ct)
    {
        return await _db.KeyVersions
            .Where(v => v.KeyId == keyId)
            .OrderByDescending(v => v.VersionNo)
            .Select(v => ToVersionDescriptor(v))
            .ToListAsync(ct);
    }

    // ────────────────── Import（骨架） ──────────────────

    public async Task<KeyDescriptor> ImportAsync(ImportKeyCommand cmd, CancellationToken ct)
    {
        // TODO: 校验格式 → Provider.ImportKey → 写入 DB → 审计
        throw new NotImplementedException();
    }

    // ────────────────── 私有辅助 ──────────────────

    private async Task<SysKey> LoadKeyForUpdateAsync(string keyId, CancellationToken ct)
    {
        var key = await _db.Keys.FirstOrDefaultAsync(k => k.KeyId == keyId, ct)
            ?? throw new BusinessException("KEY_NOT_FOUND", "密钥不存在");
        _db.Entry(key).State = EntityState.Modified; // 触发行锁
        return key;
    }

    private async Task<SysKeyVersion> LoadCurrentVersionForUpdateAsync(string keyId, int versionNo, CancellationToken ct)
    {
        var v = await _db.KeyVersions
            .FirstOrDefaultAsync(x => x.KeyId == keyId && x.VersionNo == versionNo, ct)
            ?? throw new BusinessException("KEY_VERSION_NOT_FOUND", "版本不存在");
        _db.Entry(v).State = EntityState.Modified;
        return v;
    }

    private static void ValidateKeyTypeAndUsage(string keyType, string keyUsage)
    {
        var valid = (keyType, keyUsage) switch
        {
            ("SM2", "SIGN") => true,
            ("SM2", "ENCRYPT") => true,
            ("SM4", "ENCRYPT") => true,
            ("HMAC", "MAC") => true,
            ("ROOT", "WRAP") => true,
            _ => false
        };
        if (!valid)
            throw new BusinessException("INVALID_KEY_TYPE_USAGE", $"不支持的 KeyType/KeyUsage 组合: {keyType}/{keyUsage}");
    }

    private static KeyDescriptor ToDescriptor(SysKey k) => new(
        k.KeyId, k.OwnerAppId, k.KeyType, k.KeyUsage, k.Name,
        k.Status, k.CurrentVersion, k.ExpiresAt, k.CreatedAt, k.ActivatedAt, k.Description);

    private static KeyVersionDescriptor ToVersionDescriptor(SysKeyVersion v) => new(
        v.KeyId, v.VersionNo, v.ProviderType, v.DeviceId,
        v.PublicKeyMaterial, v.Fingerprint, v.Status,
        v.CreatedAt, v.ActivatedAt, v.RotatedAt, v.ExpiresAt);
}
```

## 34.4 DI 注册

```csharp
public static class KeyServiceCollectionExtensions
{
    public static IServiceCollection AddKeyService(this IServiceCollection services)
    {
        services.AddScoped<IKeyService, KeyService>();
        return services;
    }
}
```

## 34.5 异常定义

```csharp
namespace CryptoPlatform.Application;

public class BusinessException : Exception
{
    public string Code { get; }
    public BusinessException(string code, string message) : base(message) => Code = code;
}

// 错误码规范：
// KEY_NOT_FOUND / KEY_VERSION_NOT_FOUND / KEY_INVALID_STATE
// KEY_NOT_ACTIVE / KEY_ALREADY_DESTROYD / KEY_DESTROYED
// INVALID_KEY_TYPE_USAGE / KEY_CREATE_FAILED
// PROVIDER_UNAVAILABLE / PROVIDER_OPERATION_FAILED
```
