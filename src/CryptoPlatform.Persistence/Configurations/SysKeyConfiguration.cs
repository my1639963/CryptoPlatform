using CryptoPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CryptoPlatform.Persistence.Configurations;

public sealed class SysKeyConfiguration : IEntityTypeConfiguration<SysKey>
{
    public void Configure(EntityTypeBuilder<SysKey> b)
    {
        b.ToTable("sys_key");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        b.Property(x => x.KeyId).HasColumnName("key_id").HasMaxLength(64).IsRequired();
        b.Property(x => x.OwnerAppId).HasColumnName("owner_app_id").HasMaxLength(64).IsRequired();
        b.Property(x => x.KeyType).HasColumnName("key_type").HasMaxLength(32).IsRequired();
        b.Property(x => x.KeyUsage).HasColumnName("key_usage").HasMaxLength(32).IsRequired();
        b.Property(x => x.Name).HasColumnName("name").HasMaxLength(128).IsRequired();
        b.Property(x => x.Status).HasColumnName("status").HasMaxLength(16).IsRequired();
        b.Property(x => x.CurrentVersion).HasColumnName("current_version");
        b.Property(x => x.ExpiresAt).HasColumnName("expires_at");
        b.Property(x => x.CreatedAt).HasColumnName("created_at");
        b.Property(x => x.ActivatedAt).HasColumnName("activated_at");
        b.Property(x => x.DestroyedAt).HasColumnName("destroyed_at");
        b.Property(x => x.Description).HasColumnName("description").HasMaxLength(512);
        b.Property(x => x.ConcurrencyStamp).HasColumnName("concurrency_stamp").HasMaxLength(64).IsRequired().IsConcurrencyToken();
        b.HasIndex(x => x.KeyId).IsUnique().HasDatabaseName("uk_key_id");
        b.HasIndex(x => new { x.OwnerAppId, x.Status }).HasDatabaseName("ix_key_owner_status");
        b.HasIndex(x => new { x.OwnerAppId, x.KeyType, x.Status }).HasDatabaseName("ix_key_owner_type_status");
        b.HasIndex(x => new { x.KeyType, x.KeyUsage }).HasDatabaseName("ix_key_type_usage");
        b.HasMany(x => x.Versions).WithOne().HasForeignKey(x => x.KeyId).HasPrincipalKey(x => x.KeyId);
        b.HasMany(x => x.Authorizations).WithOne().HasForeignKey(x => x.KeyId).HasPrincipalKey(x => x.KeyId);
    }
}
