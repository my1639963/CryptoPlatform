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
        b.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        b.Property(x => x.PermissionCode).HasColumnName("permission_code").HasMaxLength(64).IsRequired();
        b.Property(x => x.PermissionName).HasColumnName("permission_name").HasMaxLength(128).IsRequired();
        b.Property(x => x.ResourceType).HasColumnName("resource_type").HasMaxLength(32).IsRequired();
        b.Property(x => x.CreatedAt).HasColumnName("created_at");
        b.HasIndex(x => x.PermissionCode).IsUnique().HasDatabaseName("uk_permission_code");
    }
}
