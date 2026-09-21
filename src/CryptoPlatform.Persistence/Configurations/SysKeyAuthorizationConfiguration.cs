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
