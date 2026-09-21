namespace CryptoPlatform.Application.Keys;

// ────────────────── Commands ──────────────────

/// <summary>创建密钥命令</summary>
public sealed record CreateKeyCommand(
    string KeyType,       // SM2 / SM4 / HMAC / ROOT
    string KeyUsage,      // SIGN / ENCRYPT / MAC / WRAP
    string Name,
    DateTime? ExpiresAt,
    string? Description);

/// <summary>密钥轮换命令</summary>
public sealed record RotateKeyCommand(string? Description);

/// <summary>密钥销毁命令</summary>
public sealed record DestroyKeyCommand(string Reason, bool DestroyAllVersions);

/// <summary>密钥导入命令</summary>
public sealed record ImportKeyCommand(
    string KeyType,
    string KeyUsage,
    string Name,
    string Format,           // PEM / DER / RAW
    string? PublicKeyData,   // Base64（非对称密钥）
    string? PrivateKeyData,  // Base64（非对称密钥，加密传输）
    string? SymmetricData);  // Base64（对称密钥，加密传输）

// ────────────────── Responses ──────────────────

/// <summary>密钥描述符（对外展示）</summary>
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

/// <summary>密钥版本描述符</summary>
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
