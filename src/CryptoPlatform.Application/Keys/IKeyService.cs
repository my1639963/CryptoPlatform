namespace CryptoPlatform.Application.Keys;

/// <summary>
/// 密钥管理服务接口。
/// 提供密钥全生命周期管理：创建 → 激活 → 轮换 → 禁用 → 撤销 → 销毁。
/// </summary>
public interface IKeyService
{
    /// <summary>创建密钥（生成密钥材料 + 写入数据库）</summary>
    Task<KeyDescriptor> CreateAsync(CreateKeyCommand command, CancellationToken ct);

    /// <summary>查询密钥信息</summary>
    Task<KeyDescriptor> GetAsync(string keyId, CancellationToken ct);

    /// <summary>激活密钥（CREATED → ACTIVE）</summary>
    Task<KeyDescriptor> ActivateAsync(string keyId, CancellationToken ct);

    /// <summary>轮换密钥（生成新版本，旧版本转 ROTATED）</summary>
    Task<KeyDescriptor> RotateAsync(string keyId, RotateKeyCommand command, CancellationToken ct);

    /// <summary>禁用密钥（ACTIVE → DISABLED）</summary>
    Task DisableAsync(string keyId, CancellationToken ct);

    /// <summary>撤销密钥（CREATED/ACTIVE/DISABLED → REVOKED）</summary>
    Task RevokeAsync(string keyId, CancellationToken ct);

    /// <summary>销毁密钥（所有版本销毁，终态）</summary>
    Task DestroyAsync(string keyId, DestroyKeyCommand command, CancellationToken ct);

    /// <summary>查询指定版本信息</summary>
    Task<KeyVersionDescriptor> GetVersionAsync(string keyId, int versionNo, CancellationToken ct);

    /// <summary>查询所有版本列表</summary>
    Task<IReadOnlyList<KeyVersionDescriptor>> GetVersionsAsync(string keyId, CancellationToken ct);

    /// <summary>导入外部密钥</summary>
    Task<KeyDescriptor> ImportAsync(ImportKeyCommand command, CancellationToken ct);
}
