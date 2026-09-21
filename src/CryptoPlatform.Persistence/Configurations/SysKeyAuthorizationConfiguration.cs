using CryptoPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CryptoPlatform.Persistence.Configurations;

public sealed class SysKeyAuthorizationConfiguration : IEntityTypeConfiguration<SysKeyAuthorization>
{
    public void Configure(EntityTypeBuilder<SysKeyAuthorization> b)
    {
        b.ToTable("sys_key_authorization");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        b.Property(x => x.KeyId).HasColumnName("key_id").HasMaxLength(64).IsRequired();
        b.Property(x => x.AppId).HasColumnName("app_id").HasMaxLength(64).IsRequired();
        b.Property(x => x.Permissions).HasColumnName("permissions").HasMaxLength(500).IsRequired();
        b.Property(x => x.Status).HasColumnName("status").HasMaxLength(16).IsRequired();
        b.Property(x => x.ExpiresAt).HasColumnName("expires_at");
        b.Property(x => x.CreatedAt).HasColumnName("created_at");
        b.Property(x => x.CreatedBy).HasColumnName("created_by").HasMaxLength(64);
        b.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        b.Property(x => x.UpdatedBy).HasColumnName("updated_by").HasMaxLength(64);
        b.Property(x => x.RevokedAt).HasColumnName("revoked_at");
        b.HasIndex(x => new { x.KeyId, x.AppId }).IsUnique().HasDatabaseName("uk_key_auth");
        b.HasIndex(x => new { x.AppId, x.Status }).HasDatabaseName("ix_key_auth_app_status");
        b.HasIndex(x => new { x.KeyId, x.Status }).HasDatabaseName("ix_key_auth_key_status");
    }
}
