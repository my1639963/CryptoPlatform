using CryptoPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CryptoPlatform.Persistence.Configurations;

public sealed class SysApplicationConfiguration : IEntityTypeConfiguration<SysApplication>
{
    public void Configure(EntityTypeBuilder<SysApplication> b)
    {
        b.ToTable("sys_application");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        b.Property(x => x.AppId).HasColumnName("app_id").HasMaxLength(64).IsRequired();
        b.Property(x => x.AppName).HasColumnName("app_name").HasMaxLength(128).IsRequired();
        b.Property(x => x.Status).HasColumnName("status");
        b.Property(x => x.IpWhitelist).HasColumnName("ip_whitelist").HasColumnType("text");
        b.Property(x => x.Description).HasColumnName("description").HasMaxLength(500);
        b.Property(x => x.CreatedAt).HasColumnName("created_at");
        b.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        b.HasIndex(x => x.AppId).IsUnique().HasDatabaseName("uk_application_app_id");
        b.HasMany(x => x.Secrets).WithOne().HasForeignKey(x => x.ApplicationId);
    }
}
