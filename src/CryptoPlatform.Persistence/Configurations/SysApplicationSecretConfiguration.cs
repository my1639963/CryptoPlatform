using CryptoPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CryptoPlatform.Persistence.Configurations;

public sealed class SysApplicationSecretConfiguration : IEntityTypeConfiguration<SysApplicationSecret>
{
    public void Configure(EntityTypeBuilder<SysApplicationSecret> b)
    {
        b.ToTable("sys_application_secret");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        b.Property(x => x.ApplicationId).HasColumnName("application_id");
        b.Property(x => x.SecretHash).HasColumnName("secret_hash").HasMaxLength(255).IsRequired();
        b.Property(x => x.SecretVersion).HasColumnName("secret_version");
        b.Property(x => x.Status).HasColumnName("status");
        b.Property(x => x.ExpiresAt).HasColumnName("expires_at");
        b.Property(x => x.LastUsedAt).HasColumnName("last_used_at");
        b.Property(x => x.CreatedAt).HasColumnName("created_at");
        b.Property(x => x.RevokedAt).HasColumnName("revoked_at");
        b.HasIndex(x => x.ApplicationId).HasDatabaseName("ix_app_secret_application");
        b.HasIndex(x => new { x.Status, x.ExpiresAt }).HasDatabaseName("ix_app_secret_status");
    }
}
