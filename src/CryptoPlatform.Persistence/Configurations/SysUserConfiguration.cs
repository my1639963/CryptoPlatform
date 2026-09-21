using CryptoPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CryptoPlatform.Persistence.Configurations;

public sealed class SysUserConfiguration : IEntityTypeConfiguration<SysUser>
{
    public void Configure(EntityTypeBuilder<SysUser> b)
    {
        b.ToTable("sys_user");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Username).HasMaxLength(64).IsRequired();
        b.Property(x => x.PasswordHash).HasMaxLength(255).IsRequired();
        b.Property(x => x.DisplayName).HasMaxLength(128);
        b.Property(x => x.Status).HasColumnType("tinyint").IsRequired();
        b.Property(x => x.LockedUntil);
        b.HasIndex(x => x.Username).IsUnique();
        b.HasMany(x => x.UserRoles).WithOne().HasForeignKey(x => x.UserId);
    }
}
