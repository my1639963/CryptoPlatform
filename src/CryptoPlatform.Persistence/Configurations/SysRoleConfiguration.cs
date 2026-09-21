using CryptoPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CryptoPlatform.Persistence.Configurations;

public sealed class SysRoleConfiguration : IEntityTypeConfiguration<SysRole>
{
    public void Configure(EntityTypeBuilder<SysRole> b)
    {
        b.ToTable("sys_role");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        b.Property(x => x.RoleCode).HasColumnName("role_code").HasMaxLength(32).IsRequired();
        b.Property(x => x.RoleName).HasColumnName("role_name").HasMaxLength(64).IsRequired();
        b.Property(x => x.Description).HasColumnName("description").HasMaxLength(256);
        b.Property(x => x.CreatedAt).HasColumnName("created_at");
        b.HasIndex(x => x.RoleCode).IsUnique().HasDatabaseName("uk_role_code");
    }
}
