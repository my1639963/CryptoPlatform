# 33. EF Core 10 全部 Entity / Configuration

> 本章节提供所有实体类、Fluent API 配置和 DbContext 定义，可直接作为编码基线。国产数据库 Provider 差异需在目标环境验证后调整。

## 33.1 枚举定义

```csharp
namespace CryptoPlatform.Domain.Enums;

public enum ApplicationStatus : byte
{
    Active = 0,
    Disabled = 1,
    Locked = 2
}

public enum KeyType
{
    SM2,
    SM4,
    HMAC,
    ROOT
}

public enum KeyUsage
{
    SIGN,
    ENCRYPT,
    MAC,
    WRAP
}

public enum KeyStatus
{
    CREATED,
    ACTIVE,
    ROTATED,
    DISABLED,
    EXPIRED,
    REVOKED,
    DESTROYED
}

public enum ProviderType
{
    HSM,
    SOFTWARE
}

public enum DeviceStatus
{
    ONLINE,
    DEGRADED,
    OFFLINE,
    MAINTENANCE
}

public enum AuditOperatorType
{
    APP,
    ADMIN,
    USER,
    SYSTEM
}

public enum SecurityEventSeverity
{
    INFO,
    LOW,
    MEDIUM,
    HIGH,
    CRITICAL
}

public enum SecurityEventStatus
{
    OPEN,
    ACKNOWLEDGED,
    HANDLING,
    RESOLVED,
    CLOSED
}
```

## 33.2 实体类

```csharp
namespace CryptoPlatform.Domain.Entities;

public sealed class SysApplication
{
    public long Id { get; set; }
    public string AppId { get; set; } = null!;
    public string AppName { get; set; } = null!;
    public byte Status { get; set; }
    public string? IpWhitelist { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<SysApplicationSecret> Secrets { get; set; } = new List<SysApplicationSecret>();
}

public sealed class SysApplicationSecret
{
    public long Id { get; set; }
    public long ApplicationId { get; set; }
    public string SecretHash { get; set; } = null!;
    public int SecretVersion { get; set; }
    public byte Status { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? RevokedAt { get; set; }
}

public sealed class SysKey
{
    public long Id { get; set; }
    public string KeyId { get; set; } = null!;
    public string OwnerAppId { get; set; } = null!;
    public string KeyType { get; set; } = null!;
    public string KeyUsage { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Status { get; set; } = null!;
    public int? CurrentVersion { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ActivatedAt { get; set; }
    public DateTime? DestroyedAt { get; set; }
    public string? Description { get; set; }
    public string ConcurrencyStamp { get; set; } = null!;

    public ICollection<SysKeyVersion> Versions { get; set; } = new List<SysKeyVersion>();
    public ICollection<SysKeyAuthorization> Authorizations { get; set; } = new List<SysKeyAuthorization>();
}

public sealed class SysKeyVersion
{
    public long Id { get; set; }
    public string KeyId { get; set; } = null!;
    public int VersionNo { get; set; }
    public string ProviderType { get; set; } = null!;
    public string? DeviceId { get; set; }
    public string ProviderKeyRef { get; set; } = null!;
    public string? PublicKeyMaterial { get; set; }
    public string? EncryptedKeyMaterial { get; set; }
    public string Fingerprint { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime? ActivatedAt { get; set; }
    public DateTime? RotatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? DestroyedAt { get; set; }
    public string? DestroyResult { get; set; }
    public long UsageCount { get; set; }
    public long UsageBytes { get; set; }
    public string? CreatedRequestId { get; set; }
    public string ConcurrencyStamp { get; set; } = null!;
}

public sealed class SysKeyAuthorization
{
    public long Id { get; set; }
    public string KeyId { get; set; } = null!;
    public string AppId { get; set; } = null!;
    public string Permissions { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateTime? ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime? RevokedAt { get; set; }
}

public sealed class SysCryptoDevice
{
    public long Id { get; set; }
    public string DeviceId { get; set; } = null!;
    public string DeviceName { get; set; } = null!;
    public string Vendor { get; set; } = null!;
    public string? Model { get; set; }
    public string? SerialNumber { get; set; }
    public string? Endpoint { get; set; }
    public string ProviderType { get; set; } = null!;
    public string Status { get; set; } = null!;
    public int Priority { get; set; }
    public bool IsPrimary { get; set; }
    public DateTime? LastHealthAt { get; set; }
    public long ErrorCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class SysUser
{
    public long Id { get; set; }
    public string Username { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public string? DisplayName { get; set; }
    public byte Status { get; set; }
    public int LoginFailCount { get; set; }
    public DateTime? LockedUntil { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<SysUserRole> UserRoles { get; set; } = new List<SysUserRole>();
}

public sealed class SysRole
{
    public long Id { get; set; }
    public string RoleCode { get; set; } = null!;
    public string RoleName { get; set; } = null!;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class SysPermission
{
    public long Id { get; set; }
    public string PermissionCode { get; set; } = null!;
    public string PermissionName { get; set; } = null!;
    public string ResourceType { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}

public sealed class SysUserRole
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public long RoleId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class SysAuditLog
{
    public long Id { get; set; }
    public string AuditId { get; set; } = null!;
    public string? RequestId { get; set; }
    public string? TraceId { get; set; }
    public DateTime Timestamp { get; set; }
    public string OperatorType { get; set; } = null!;
    public string? OperatorId { get; set; }
    public string? OperatorName { get; set; }
    public string? AppId { get; set; }
    public string? SourceIp { get; set; }
    public string Operation { get; set; } = null!;
    public string? KeyId { get; set; }
    public int? KeyVersion { get; set; }
    public string ResultCode { get; set; } = null!;
    public int? DurationMs { get; set; }
    public string? PreviousHash { get; set; }
    public string CurrentHash { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}

public sealed class SysSecurityEvent
{
    public long Id { get; set; }
    public string EventId { get; set; } = null!;
    public string EventType { get; set; } = null!;
    public string Severity { get; set; } = null!;
    public string? AppId { get; set; }
    public string? OperatorId { get; set; }
    public string? SourceIp { get; set; }
    public string? RelatedKeyId { get; set; }
    public string? RequestId { get; set; }
    public string? Description { get; set; }
    public string Status { get; set; } = null!;
    public DateTime DetectedAt { get; set; }
    public DateTime? HandledAt { get; set; }
    public string? Handler { get; set; }
    public string? HandlingResult { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class SysConfig
{
    public long Id { get; set; }
    public string ConfigKey { get; set; } = null!;
    public string ConfigValue { get; set; } = null!;
    public string? Description { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class SysIdempotencyRecord
{
    public long Id { get; set; }
    public string AppId { get; set; } = null!;
    public string IdempotencyKey { get; set; } = null!;
    public string HttpMethod { get; set; } = null!;
    public string RequestPath { get; set; } = null!;
    public string RequestHash { get; set; } = null!;
    public int? ResponseCode { get; set; }
    public string? ResponseBodyHash { get; set; }
    public string Status { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}
```

## 33.3 Fluent API 配置

```csharp
namespace CryptoPlatform.Persistence.Configurations;

public sealed class SysApplicationConfiguration : IEntityTypeConfiguration<SysApplication>
{
    public void Configure(EntityTypeBuilder<SysApplication> b)
    {
        b.ToTable("sys_application");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.AppId).HasMaxLength(64).IsRequired();
        b.Property(x => x.AppName).HasMaxLength(128).IsRequired();
        b.Property(x => x.IpWhitelist).HasColumnType("text");
        b.Property(x => x.Description).HasMaxLength(500);
        b.HasIndex(x => x.AppId).IsUnique();
        b.HasMany(x => x.Secrets).WithOne().HasForeignKey(x => x.ApplicationId);
    }
}

public sealed class SysApplicationSecretConfiguration : IEntityTypeConfiguration<SysApplicationSecret>
{
    public void Configure(EntityTypeBuilder<SysApplicationSecret> b)
    {
        b.ToTable("sys_application_secret");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.SecretHash).HasMaxLength(255).IsRequired();
        b.HasIndex(x => x.ApplicationId);
        b.HasIndex(x => new { x.Status, x.ExpiresAt });
    }
}

public sealed class SysKeyConfiguration : IEntityTypeConfiguration<SysKey>
{
    public void Configure(EntityTypeBuilder<SysKey> b)
    {
        b.ToTable("sys_key");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.KeyId).HasMaxLength(64).IsRequired();
        b.Property(x => x.OwnerAppId).HasMaxLength(64).IsRequired();
        b.Property(x => x.KeyType).HasMaxLength(32).IsRequired();
        b.Property(x => x.KeyUsage).HasMaxLength(32).IsRequired();
        b.Property(x => x.Name).HasMaxLength(128).IsRequired();
        b.Property(x => x.Status).HasMaxLength(16).IsRequired();
        b.Property(x => x.Description).HasMaxLength(512);
        b.Property(x => x.ConcurrencyStamp).HasMaxLength(64).IsRequired().IsConcurrencyToken();
        b.HasIndex(x => x.KeyId).IsUnique();
        b.HasIndex(x => new { x.OwnerAppId, x.Status });
        b.HasIndex(x => new { x.OwnerAppId, x.KeyType, x.Status });
        b.HasMany(x => x.Versions).WithOne().HasForeignKey(x => x.KeyId).HasPrincipalKey(x => x.KeyId);
        b.HasMany(x => x.Authorizations).WithOne().HasForeignKey(x => x.KeyId).HasPrincipalKey(x => x.KeyId);
    }
}

public sealed class SysKeyVersionConfiguration : IEntityTypeConfiguration<SysKeyVersion>
{
    public void Configure(EntityTypeBuilder<SysKeyVersion> b)
    {
        b.ToTable("sys_key_version");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.KeyId).HasMaxLength(64).IsRequired();
        b.Property(x => x.ProviderType).HasMaxLength(32).IsRequired();
        b.Property(x => x.DeviceId).HasMaxLength(64);
        b.Property(x => x.ProviderKeyRef).HasMaxLength(512).IsRequired();
        b.Property(x => x.PublicKeyMaterial).HasColumnType("text");
        b.Property(x => x.EncryptedKeyMaterial).HasColumnType("longtext");
        b.Property(x => x.Fingerprint).HasMaxLength(128).IsRequired();
        b.Property(x => x.Status).HasMaxLength(16).IsRequired();
        b.Property(x => x.DestroyResult).HasMaxLength(32);
        b.Property(x => x.CreatedRequestId).HasMaxLength(64);
        b.Property(x => x.ConcurrencyStamp).HasMaxLength(64).IsRequired().IsConcurrencyToken();
        b.HasIndex(x => new { x.KeyId, x.VersionNo }).IsUnique();
        b.HasIndex(x => new { x.KeyId, x.Status });
        b.HasIndex(x => new { x.ProviderType, x.DeviceId });
        b.HasIndex(x => x.Fingerprint);
    }
}

public sealed class SysKeyAuthorizationConfiguration : IEntityTypeConfiguration<SysKeyAuthorization>
{
    public void Configure(EntityTypeBuilder<SysKeyAuthorization> b)
    {
        b.ToTable("sys_key_authorization");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.KeyId).HasMaxLength(64).IsRequired();
        b.Property(x => x.AppId).HasMaxLength(64).IsRequired();
        b.Property(x => x.Permissions).HasMaxLength(500).IsRequired();
        b.Property(x => x.Status).HasMaxLength(16).IsRequired();
        b.Property(x => x.CreatedBy).HasMaxLength(64);
        b.Property(x => x.UpdatedBy).HasMaxLength(64);
        b.HasIndex(x => new { x.KeyId, x.AppId }).IsUnique();
        b.HasIndex(x => new { x.AppId, x.Status });
        b.HasIndex(x => new { x.KeyId, x.Status });
    }
}

public sealed class SysCryptoDeviceConfiguration : IEntityTypeConfiguration<SysCryptoDevice>
{
    public void Configure(EntityTypeBuilder<SysCryptoDevice> b)
    {
        b.ToTable("sys_crypto_device");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.DeviceId).HasMaxLength(64).IsRequired();
        b.Property(x => x.DeviceName).HasMaxLength(128).IsRequired();
        b.Property(x => x.Vendor).HasMaxLength(64).IsRequired();
        b.Property(x => x.Model).HasMaxLength(64);
        b.Property(x => x.SerialNumber).HasMaxLength(128);
        b.Property(x => x.Endpoint).HasMaxLength(256);
        b.Property(x => x.ProviderType).HasMaxLength(32).IsRequired();
        b.Property(x => x.Status).HasMaxLength(16).IsRequired();
        b.HasIndex(x => x.DeviceId).IsUnique();
        b.HasIndex(x => new { x.ProviderType, x.Status });
    }
}

public sealed class SysAuditLogConfiguration : IEntityTypeConfiguration<SysAuditLog>
{
    public void Configure(EntityTypeBuilder<SysAuditLog> b)
    {
        b.ToTable("sys_audit_log");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.AuditId).HasMaxLength(64).IsRequired();
        b.Property(x => x.RequestId).HasMaxLength(64);
        b.Property(x => x.TraceId).HasMaxLength(64);
        b.Property(x => x.OperatorType).HasMaxLength(16).IsRequired();
        b.Property(x => x.OperatorId).HasMaxLength(64);
        b.Property(x => x.OperatorName).HasMaxLength(128);
        b.Property(x => x.AppId).HasMaxLength(64);
        b.Property(x => x.SourceIp).HasMaxLength(45);
        b.Property(x => x.Operation).HasMaxLength(64).IsRequired();
        b.Property(x => x.KeyId).HasMaxLength(64);
        b.Property(x => x.ResultCode).HasMaxLength(32).IsRequired();
        b.Property(x => x.PreviousHash).HasMaxLength(128);
        b.Property(x => x.CurrentHash).HasMaxLength(128).IsRequired();
        b.HasIndex(x => x.AuditId).IsUnique();
        b.HasIndex(x => x.Timestamp);
        b.HasIndex(x => new { x.AppId, x.Timestamp });
        b.HasIndex(x => new { x.KeyId, x.Timestamp });
        b.HasIndex(x => new { x.Operation, x.Timestamp });
    }
}

public sealed class SysSecurityEventConfiguration : IEntityTypeConfiguration<SysSecurityEvent>
{
    public void Configure(EntityTypeBuilder<SysSecurityEvent> b)
    {
        b.ToTable("sys_security_event");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.EventId).HasMaxLength(64).IsRequired();
        b.Property(x => x.EventType).HasMaxLength(64).IsRequired();
        b.Property(x => x.Severity).HasMaxLength(16).IsRequired();
        b.Property(x => x.AppId).HasMaxLength(64);
        b.Property(x => x.Description).HasMaxLength(1000);
        b.Property(x => x.Status).HasMaxLength(16).IsRequired();
        b.Property(x => x.Handler).HasMaxLength(64);
        b.Property(x => x.HandlingResult).HasMaxLength(500);
        b.HasIndex(x => x.EventId).IsUnique();
        b.HasIndex(x => new { x.EventType, x.Status });
        b.HasIndex(x => new { x.Severity, x.Status });
        b.HasIndex(x => x.DetectedAt);
    }
}

public sealed class SysIdempotencyRecordConfiguration : IEntityTypeConfiguration<SysIdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<SysIdempotencyRecord> b)
    {
        b.ToTable("sys_idempotency_record");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.AppId).HasMaxLength(64).IsRequired();
        b.Property(x => x.IdempotencyKey).HasMaxLength(128).IsRequired();
        b.Property(x => x.HttpMethod).HasMaxLength(10).IsRequired();
        b.Property(x => x.RequestPath).HasMaxLength(256).IsRequired();
        b.Property(x => x.RequestHash).HasMaxLength(128).IsRequired();
        b.Property(x => x.ResponseBodyHash).HasMaxLength(128);
        b.Property(x => x.Status).HasMaxLength(16).IsRequired();
        b.HasIndex(x => new { x.AppId, x.HttpMethod, x.RequestPath, x.IdempotencyKey }).IsUnique();
        b.HasIndex(x => x.ExpiresAt);
    }
}
```

## 33.4 DbContext

```csharp
namespace CryptoPlatform.Persistence;

public sealed class CryptoPlatformDbContext : DbContext
{
    public CryptoPlatformDbContext(DbContextOptions<CryptoPlatformDbContext> options)
        : base(options) { }

    public DbSet<SysApplication> Applications => Set<SysApplication>();
    public DbSet<SysApplicationSecret> ApplicationSecrets => Set<SysApplicationSecret>();
    public DbSet<SysKey> Keys => Set<SysKey>();
    public DbSet<SysKeyVersion> KeyVersions => Set<SysKeyVersion>();
    public DbSet<SysKeyAuthorization> KeyAuthorizations => Set<SysKeyAuthorization>();
    public DbSet<SysCryptoDevice> CryptoDevices => Set<SysCryptoDevice>();
    public DbSet<SysUser> Users => Set<SysUser>();
    public DbSet<SysRole> Roles => Set<SysRole>();
    public DbSet<SysPermission> Permissions => Set<SysPermission>();
    public DbSet<SysUserRole> UserRoles => Set<SysUserRole>();
    public DbSet<SysAuditLog> AuditLogs => Set<SysAuditLog>();
    public DbSet<SysSecurityEvent> SecurityEvents => Set<SysSecurityEvent>();
    public DbSet<SysConfig> Configs => Set<SysConfig>();
    public DbSet<SysIdempotencyRecord> IdempotencyRecords => Set<SysIdempotencyRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CryptoPlatformDbContext).Assembly);
    }
}
```

## 33.5 DI 注册

```csharp
public static class PersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── 雪花 ID 生成器（单例） ──
        var workerId = long.TryParse(configuration["Snowflake:WorkerId"], out var id) ? id : 0L;
        services.AddSingleton(new SnowflakeIdGenerator(workerId));

        // ── 拦截器（单例） ──
        services.AddSingleton<UpdatedAtInterceptor>();
        services.AddSingleton<SnowflakeIdInterceptor>();

        services.AddDbContext<CryptoPlatformDbContext>((sp, options) =>
        {
            var connectionString = configuration.GetConnectionString("Default");
            options.UseMySQL(connectionString);

            // 注入拦截器
            options.AddInterceptors(
                sp.GetRequiredService<UpdatedAtInterceptor>(),
                sp.GetRequiredService<SnowflakeIdInterceptor>());
        });

        return services;
    }
}
```

> **雪花 ID 说明：** `SnowflakeIdGenerator` 采用标准 64 位结构（41 bits 时间戳 + 10 bits 工作节点 + 12 bits 序列号），起始纪元 2020-01-01，可用约 69 年。`SnowflakeIdInterceptor` 在 `SaveChanges` 前自动为 `Id == 0` 的新增实体分配雪花 ID，所有实体的 `Id` 配置为 `ValueGeneratedNever()`，不依赖数据库自增。
