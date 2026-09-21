using CryptoPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CryptoPlatform.Persistence.Configurations;

public sealed class SysPermissionConfiguration : IEntityTypeConfiguration<SysPermission>
{
    public void Configure(EntityTypeBuilder<SysPermission> b)
    {
        b.ToTable("sys_permission");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.PermissionCode).HasMaxLength(64).IsRequired();
        b.Property(x => x.PermissionName).HasMaxLength(128).IsRequired();
        b.Property(x => x.ResourceType).HasMaxLength(32).IsRequired();
        b.HasIndex(x => x.PermissionCode).IsUnique();
    }
}
