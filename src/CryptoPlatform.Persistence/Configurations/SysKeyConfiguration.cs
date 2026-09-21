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
