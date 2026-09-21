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
        b.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        b.Property(x => x.UserId).HasColumnName("user_id");
        b.Property(x => x.RoleId).HasColumnName("role_id");
        b.Property(x => x.CreatedAt).HasColumnName("created_at");
        b.HasIndex(x => new { x.UserId, x.RoleId }).IsUnique().HasDatabaseName("uk_user_role");
        b.HasIndex(x => x.UserId).HasDatabaseName("ix_user_role_user");
        b.HasIndex(x => x.RoleId).HasDatabaseName("ix_user_role_role");
        b.HasOne(x => x.Role).WithMany().HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Restrict);
    }
}
