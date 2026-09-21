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
