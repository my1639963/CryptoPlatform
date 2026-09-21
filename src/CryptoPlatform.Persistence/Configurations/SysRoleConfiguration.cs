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
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.RoleCode).HasMaxLength(32).IsRequired();
        b.Property(x => x.RoleName).HasMaxLength(64).IsRequired();
        b.Property(x => x.Description).HasMaxLength(256);
        b.HasIndex(x => x.RoleCode).IsUnique();
    }
}
