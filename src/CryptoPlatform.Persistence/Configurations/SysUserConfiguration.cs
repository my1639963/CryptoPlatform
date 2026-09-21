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
        b.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        b.Property(x => x.Username).HasColumnName("username").HasMaxLength(64).IsRequired();
        b.Property(x => x.PasswordHash).HasColumnName("password_hash").HasMaxLength(512).IsRequired();
        b.Property(x => x.PasswordAlgorithm).HasColumnName("password_algorithm").HasMaxLength(32).IsRequired().HasDefaultValue("PBKDF2-SHA256");
        b.Property(x => x.PasswordVersion).HasColumnName("password_version").IsRequired().HasDefaultValue(1);
        b.Property(x => x.PasswordChangedAt).HasColumnName("password_changed_at");
        b.Property(x => x.MustModifyPassword).HasColumnName("must_modify_pwd").IsRequired().HasDefaultValue(false);
        b.Property(x => x.DisplayName).HasColumnName("display_name").HasMaxLength(128);
        b.Property(x => x.Status).HasColumnName("status").HasColumnType("tinyint").IsRequired();
        b.Property(x => x.LoginFailCount).HasColumnName("login_fail_count");
        b.Property(x => x.LockedUntil).HasColumnName("locked_until");
        b.Property(x => x.LastLoginAt).HasColumnName("last_login_at");
        b.Property(x => x.CreatedAt).HasColumnName("created_at");
        b.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        b.HasIndex(x => x.Username).IsUnique().HasDatabaseName("uk_user_username");
        b.HasMany(x => x.UserRoles).WithOne().HasForeignKey(x => x.UserId);
    }
}
