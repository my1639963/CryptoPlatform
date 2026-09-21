using CryptoPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CryptoPlatform.Persistence.Configurations;

public sealed class SysKeyVersionConfiguration : IEntityTypeConfiguration<SysKeyVersion>
{
    public void Configure(EntityTypeBuilder<SysKeyVersion> b)
    {
        b.ToTable("sys_key_version");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        b.Property(x => x.KeyId).HasColumnName("key_id").HasMaxLength(64).IsRequired();
        b.Property(x => x.VersionNo).HasColumnName("version_no");
        b.Property(x => x.ProviderType).HasColumnName("provider_type").HasMaxLength(32).IsRequired();
        b.Property(x => x.DeviceId).HasColumnName("device_id").HasMaxLength(64);
        b.Property(x => x.ProviderKeyRef).HasColumnName("provider_key_ref").HasMaxLength(512).IsRequired();
        b.Property(x => x.PublicKeyMaterial).HasColumnName("public_key_material").HasColumnType("text");
        b.Property(x => x.EncryptedKeyMaterial).HasColumnName("encrypted_key_material").HasColumnType("longtext");
        b.Property(x => x.Fingerprint).HasColumnName("fingerprint").HasMaxLength(128).IsRequired();
        b.Property(x => x.Status).HasColumnName("status").HasMaxLength(16).IsRequired();
        b.Property(x => x.CreatedAt).HasColumnName("created_at");
        b.Property(x => x.ActivatedAt).HasColumnName("activated_at");
        b.Property(x => x.RotatedAt).HasColumnName("rotated_at");
        b.Property(x => x.ExpiresAt).HasColumnName("expires_at");
        b.Property(x => x.DestroyedAt).HasColumnName("destroyed_at");
        b.Property(x => x.DestroyResult).HasColumnName("destroy_result").HasMaxLength(32);
        b.Property(x => x.UsageCount).HasColumnName("usage_count");
        b.Property(x => x.UsageBytes).HasColumnName("usage_bytes");
        b.Property(x => x.CreatedRequestId).HasColumnName("created_request_id").HasMaxLength(64);
        b.Property(x => x.ConcurrencyStamp).HasColumnName("concurrency_stamp").HasMaxLength(64).IsRequired().IsConcurrencyToken();
        b.HasIndex(x => new { x.KeyId, x.VersionNo }).IsUnique().HasDatabaseName("uk_key_version");
        b.HasIndex(x => new { x.KeyId, x.Status }).HasDatabaseName("ix_key_version_status");
        b.HasIndex(x => new { x.ProviderType, x.DeviceId }).HasDatabaseName("ix_key_version_provider");
        b.HasIndex(x => x.Fingerprint).HasDatabaseName("ix_key_version_fingerprint");
        b.HasIndex(x => x.ProviderKeyRef).HasDatabaseName("ix_key_version_provider_ref");
    }
}
