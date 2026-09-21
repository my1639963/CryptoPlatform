using CryptoPlatform.Audit;
using CryptoPlatform.Crypto.Abstractions;
using CryptoPlatform.Domain;
using CryptoPlatform.Domain.Entities;
using CryptoPlatform.Domain.Enums;
using CryptoPlatform.Infrastructure.Locking;
using CryptoPlatform.Persistence;
using CryptoPlatform.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CryptoPlatform.Application.Keys;

/// <summary>
/// 密钥管理服务实现。
/// 负责密钥全生命周期管理，包括 Provider 调用、数据库持久化和审计日志。
/// 使用 Saga 补偿模式处理 Provider+DB 分布式操作失败场景。
/// </summary>
public sealed class KeyService : IKeyService
{
    private readonly CryptoPlatformDbContext _db;
    private readonly ICryptoProviderRouter _providerRouter;
    private readonly IAuditService _audit;
    private readonly ISecurityEventService _securityEvents;
    private readonly IOperationContext _operationContext;
    private readonly IDistributedLock _distributedLock;
    private readonly ILogger<KeyService> _logger;

    public KeyService(
        CryptoPlatformDbContext db,
        ICryptoProviderRouter providerRouter,
        IAuditService audit,
        ISecurityEventService securityEvents,
        IOperationContext operationContext,
        IDistributedLock distributedLock,
        ILogger<KeyService> logger)
    {
        _db = db;
        _providerRouter = providerRouter;
        _audit = audit;
        _securityEvents = securityEvents;
        _operationContext = operationContext;
        _distributedLock = distributedLock;
        _logger = logger;
    }

    // ══════════════════════════════════════════════
    // Create
    // ══════════════════════════════════════════════

    public async Task<KeyDescriptor> CreateAsync(CreateKeyCommand cmd, CancellationToken ct)
    {
        ValidateKeyTypeAndUsage(cmd.KeyType, cmd.KeyUsage);

        var keyId = $"KEY-{Guid.NewGuid():N}";
        var key = new SysKey
        {
            KeyId = keyId,
            OwnerAppId = _operationContext.AppId,
            KeyType = cmd.KeyType,
            KeyUsage = cmd.KeyUsage,
            Name = cmd.Name,
            Status = nameof(KeyStatus.CREATED),
            ExpiresAt = cmd.ExpiresAt,
            Description = cmd.Description,
            CreatedAt = DateTime.UtcNow,
            ConcurrencyStamp = Guid.NewGuid().ToString("N")
        };

        // 阶段 1：写入逻辑密钥（CREATED 状态）
        _db.Keys.Add(key);
        await _db.SaveChangesAsync(ct);

        try
        {
            // 阶段 2：调用 Provider 生成密钥材料
            var provider = _providerRouter.GetDefaultProvider();
            string providerKeyRef;
            string? publicKeyMaterial;
            string? encryptedKeyMaterial;
            string fingerprint;

            if (cmd.KeyType == nameof(KeyType.SM2))
            {
                var result = await provider.GenerateKeyPairAsync(KeyPairAlgorithm.SM2, ct);
                providerKeyRef = result.ProviderKeyRef;
                publicKeyMaterial = result.PublicKeyMaterial;
                encryptedKeyMaterial = result.EncryptedKeyMaterial;
                fingerprint = result.Fingerprint;
            }
            else
            {
                var algorithm = MapToKeyAlgorithm(cmd.KeyType);
                var result = await provider.GenerateKeyAsync(algorithm, ct);
                providerKeyRef = result.ProviderKeyRef;
                publicKeyMaterial = null;
                encryptedKeyMaterial = result.EncryptedKeyMaterial;
                fingerprint = result.Fingerprint;
            }

            // 阶段 3：写入 KeyVersion
            var version = new SysKeyVersion
            {
                KeyId = keyId,
                VersionNo = 1,
                ProviderType = provider.ProviderType,
                DeviceId = null,
                ProviderKeyRef = providerKeyRef,
                PublicKeyMaterial = publicKeyMaterial,
                EncryptedKeyMaterial = encryptedKeyMaterial,
                Fingerprint = fingerprint,
                Status = nameof(KeyStatus.CREATED),
                CreatedAt = DateTime.UtcNow,
                CreatedRequestId = _operationContext.RequestId,
                ConcurrencyStamp = Guid.NewGuid().ToString("N")
            };

            _db.KeyVersions.Add(version);
            key.CurrentVersion = 1;
            await _db.SaveChangesAsync(ct);

            await _audit.LogAsync("KEY_CREATE", keyId, 1, "SUCCESS", ct);
            _logger.LogInformation("密钥 {KeyId} 创建成功，类型={KeyType}，用途={KeyUsage}", keyId, cmd.KeyType, cmd.KeyUsage);

            return ToDescriptor(key);
        }
        catch (Exception ex)
        {
            // Saga 补偿：Provider 已成功但 DB 写入失败 → 标记密钥为 REVOKED
            _logger.LogError(ex, "密钥 {KeyId} 创建后写入失败，进入补偿流程", keyId);
            key.Status = nameof(KeyStatus.REVOKED);
            await _db.SaveChangesAsync(CancellationToken.None);
            await _securityEvents.RaiseAsync("KEY_CREATE_FAILED", keyId, ex.Message, CancellationToken.None);
            throw;
        }
    }

    // ══════════════════════════════════════════════
    // Activate
    // ══════════════════════════════════════════════

    public async Task<KeyDescriptor> ActivateAsync(string keyId, CancellationToken ct)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var key = await LoadKeyForUpdateAsync(keyId, ct);
        var versionNo = key.CurrentVersion ?? throw new BusinessException("KEY_NO_VERSION", "密钥无可用版本");
        var version = await LoadVersionForUpdateAsync(keyId, versionNo, ct);

        // 状态校验
        var fromStatus = Enum.Parse<KeyStatus>(key.Status);
        KeyStatusTransition.Validate(fromStatus, KeyStatus.ACTIVE);

        // Provider 自检
        var provider = _providerRouter.ResolveProvider(version.ProviderType);
        var health = await provider.CheckHealthAsync(ct);
        if (health.Status != "HEALTHY")
            throw new BusinessException("PROVIDER_UNAVAILABLE", "Provider 不可用");

        version.Status = nameof(KeyStatus.ACTIVE);
        version.ActivatedAt = DateTime.UtcNow;
        key.Status = nameof(KeyStatus.ACTIVE);
        key.ActivatedAt = DateTime.UtcNow;

        await _audit.LogAsync("KEY_ACTIVATE", keyId, versionNo, "SUCCESS", ct);
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        _logger.LogInformation("密钥 {KeyId} 已激活", keyId);
        return ToDescriptor(key);
    }

    // ══════════════════════════════════════════════
    // Rotate
    // ══════════════════════════════════════════════

    public async Task<KeyDescriptor> RotateAsync(string keyId, RotateKeyCommand cmd, CancellationToken ct)
    {
        var lockKey = $"lock:key-rotate:{keyId}";
        await using var lockHandle = await _distributedLock.TryAcquireAsync(
            lockKey, expiry: TimeSpan.FromSeconds(30), waitTimeout: TimeSpan.FromSeconds(10), ct)
            ?? throw new BusinessException("KEY_ROTATION_LOCKED", "密钥轮换操作正在进行中，请稍后重试");

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var key = await LoadKeyForUpdateAsync(keyId, ct);
        if (key.Status != nameof(KeyStatus.ACTIVE))
            throw new BusinessException("KEY_NOT_ACTIVE", "仅 ACTIVE 密钥可轮换");

        var oldVersionNo = key.CurrentVersion!.Value;
        var oldVersion = await LoadVersionForUpdateAsync(keyId, oldVersionNo, ct);

        // 生成新版本
        var newVersionNo = oldVersionNo + 1;
        var provider = _providerRouter.ResolveProvider(oldVersion.ProviderType);

        string providerKeyRef;
        string? publicKeyMaterial;
        string? encryptedKeyMaterial;
        string fingerprint;

        if (key.KeyType == nameof(KeyType.SM2))
        {
            var result = await provider.GenerateKeyPairAsync(KeyPairAlgorithm.SM2, ct);
            providerKeyRef = result.ProviderKeyRef;
            publicKeyMaterial = result.PublicKeyMaterial;
            encryptedKeyMaterial = result.EncryptedKeyMaterial;
            fingerprint = result.Fingerprint;
        }
        else
        {
            var algorithm = MapToKeyAlgorithm(key.KeyType);
            var result = await provider.GenerateKeyAsync(algorithm, ct);
            providerKeyRef = result.ProviderKeyRef;
            publicKeyMaterial = null;
            encryptedKeyMaterial = result.EncryptedKeyMaterial;
            fingerprint = result.Fingerprint;
        }

        var newVersion = new SysKeyVersion
        {
            KeyId = keyId,
            VersionNo = newVersionNo,
            ProviderType = provider.ProviderType,
            DeviceId = null,
            ProviderKeyRef = providerKeyRef,
            PublicKeyMaterial = publicKeyMaterial,
            EncryptedKeyMaterial = encryptedKeyMaterial,
            Fingerprint = fingerprint,
            Status = nameof(KeyStatus.ACTIVE),
            ActivatedAt = DateTime.UtcNow,
            RotatedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            CreatedRequestId = _operationContext.RequestId,
            ConcurrencyStamp = Guid.NewGuid().ToString("N")
        };

        _db.KeyVersions.Add(newVersion);

        // 旧版本转 ROTATED
        oldVersion.Status = nameof(KeyStatus.ROTATED);
        oldVersion.RotatedAt = DateTime.UtcNow;

        key.CurrentVersion = newVersionNo;

        await _audit.LogAsync("KEY_ROTATE", keyId, newVersionNo, "SUCCESS", ct);
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        _logger.LogInformation("密钥 {KeyId} 已轮换，新版本={NewVer}", keyId, newVersionNo);
        return ToDescriptor(key);
    }

    // ══════════════════════════════════════════════
    // Disable
    // ══════════════════════════════════════════════

    public async Task DisableAsync(string keyId, CancellationToken ct)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var key = await LoadKeyForUpdateAsync(keyId, ct);
        var fromStatus = Enum.Parse<KeyStatus>(key.Status);
        KeyStatusTransition.Validate(fromStatus, KeyStatus.DISABLED);

        key.Status = nameof(KeyStatus.DISABLED);

        await _audit.LogAsync("KEY_DISABLE", keyId, null, "SUCCESS", ct);
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        _logger.LogInformation("密钥 {KeyId} 已禁用", keyId);
    }

    // ══════════════════════════════════════════════
    // Revoke
    // ══════════════════════════════════════════════

    public async Task RevokeAsync(string keyId, CancellationToken ct)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var key = await LoadKeyForUpdateAsync(keyId, ct);
        var fromStatus = Enum.Parse<KeyStatus>(key.Status);
        KeyStatusTransition.Validate(fromStatus, KeyStatus.REVOKED);

        key.Status = nameof(KeyStatus.REVOKED);

        await _audit.LogAsync("KEY_REVOKE", keyId, null, "SUCCESS", ct);
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        _logger.LogInformation("密钥 {KeyId} 已撤销", keyId);
    }

    // ══════════════════════════════════════════════
    // Destroy
    // ══════════════════════════════════════════════

    public async Task DestroyAsync(string keyId, DestroyKeyCommand cmd, CancellationToken ct)
    {
        var lockKey = $"lock:key-destroy:{keyId}";
        await using var lockHandle = await _distributedLock.TryAcquireAsync(
            lockKey, expiry: TimeSpan.FromSeconds(30), waitTimeout: TimeSpan.FromSeconds(10), ct)
            ?? throw new BusinessException("KEY_DESTROY_LOCKED", "密钥销毁操作正在进行中，请稍后重试");

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var key = await LoadKeyForUpdateAsync(keyId, ct);
        if (key.Status == nameof(KeyStatus.DESTROYED))
            throw new BusinessException("KEY_ALREADY_DESTROYED", "密钥已销毁");

        var fromStatus = Enum.Parse<KeyStatus>(key.Status);
        KeyStatusTransition.Validate(fromStatus, KeyStatus.DESTROYED);

        // 销毁所有未销毁的版本
        var versions = await _db.KeyVersions
            .Where(v => v.KeyId == keyId && v.Status != nameof(KeyStatus.DESTROYED))
            .ToListAsync(ct);

        foreach (var version in versions)
        {
            var provider = _providerRouter.ResolveProvider(version.ProviderType);
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
                _logger.LogError(ex, "密钥 {KeyId} V{Ver} Provider 销毁失败", keyId, version.VersionNo);
                throw;
            }
        }

        key.Status = nameof(KeyStatus.DESTROYED);
        key.DestroyedAt = DateTime.UtcNow;

        await _audit.LogAsync("KEY_DESTROY", keyId, null, "SUCCESS", ct);
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        _logger.LogInformation("密钥 {KeyId} 已销毁，原因={Reason}", keyId, cmd.Reason);
    }

    // ══════════════════════════════════════════════
    // Query
    // ══════════════════════════════════════════════

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

    // ══════════════════════════════════════════════
    // Import（Phase 3 骨架，Phase 6 完善）
    // ══════════════════════════════════════════════

    public async Task<KeyDescriptor> ImportAsync(ImportKeyCommand cmd, CancellationToken ct)
    {
        ValidateKeyTypeAndUsage(cmd.KeyType, cmd.KeyUsage);

        // TODO: Phase 6 完善密钥导入流程
        // 校验格式 → Provider.ImportKey → 写入 DB → 审计
        throw new BusinessException("NOT_IMPLEMENTED", "密钥导入功能尚未实现");
    }

    // ══════════════════════════════════════════════
    // 私有辅助方法
    // ══════════════════════════════════════════════

    private async Task<SysKey> LoadKeyForUpdateAsync(string keyId, CancellationToken ct)
    {
        var key = await _db.Keys.FirstOrDefaultAsync(k => k.KeyId == keyId, ct)
            ?? throw new BusinessException("KEY_NOT_FOUND", "密钥不存在");
        // InMemory DB 不支持行锁；生产环境通过 SELECT FOR UPDATE 或乐观并发控制
        return key;
    }

    private async Task<SysKeyVersion> LoadVersionForUpdateAsync(string keyId, int versionNo, CancellationToken ct)
    {
        var v = await _db.KeyVersions
            .FirstOrDefaultAsync(x => x.KeyId == keyId && x.VersionNo == versionNo, ct)
            ?? throw new BusinessException("KEY_VERSION_NOT_FOUND", "版本不存在");
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
            throw new BusinessException("INVALID_KEY_TYPE_USAGE",
                $"不支持的 KeyType/KeyUsage 组合: {keyType}/{keyUsage}");
    }

    private static KeyAlgorithm MapToKeyAlgorithm(string keyType) => keyType switch
    {
        "SM4" => KeyAlgorithm.SM4_128,
        "HMAC" => KeyAlgorithm.HMAC_SM3,
        "ROOT" => KeyAlgorithm.SM4_128, // ROOT 密钥本质是 SM4 密钥，用于 KEK 派生
        _ => throw new BusinessException("INVALID_KEY_TYPE", $"不支持的对称密钥类型: {keyType}")
    };

    private static KeyDescriptor ToDescriptor(SysKey k) => new(
        k.KeyId, k.OwnerAppId, k.KeyType, k.KeyUsage, k.Name,
        k.Status, k.CurrentVersion, k.ExpiresAt, k.CreatedAt, k.ActivatedAt, k.Description);

    private static KeyVersionDescriptor ToVersionDescriptor(SysKeyVersion v) => new(
        v.KeyId, v.VersionNo, v.ProviderType, v.DeviceId,
        v.PublicKeyMaterial, v.Fingerprint, v.Status,
        v.CreatedAt, v.ActivatedAt, v.RotatedAt, v.ExpiresAt);
}
