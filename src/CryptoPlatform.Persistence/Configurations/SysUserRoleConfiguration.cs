using CryptoPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CryptoPlatform.Persistence.Configurations;

public sealed class SysUserRoleConfiguration : IEntityTypeConfiguration<SysUserRole>
{
    public void Configure(EntityTypeBuilder<SysUserRole> b)
    {
        b.ToTable("sys_user_role");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.HasIndex(x => new { x.UserId, x.RoleId }).IsUnique();
        b.HasIndex(x => x.UserId);
        b.HasIndex(x => x.RoleId);
    }
}
