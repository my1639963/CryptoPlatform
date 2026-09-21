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
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.AppId).HasMaxLength(64).IsRequired();
        b.Property(x => x.AppName).HasMaxLength(128).IsRequired();
        b.Property(x => x.IpWhitelist).HasColumnType("text");
        b.Property(x => x.Description).HasMaxLength(500);
        b.HasIndex(x => x.AppId).IsUnique();
        b.HasMany(x => x.Secrets).WithOne().HasForeignKey(x => x.ApplicationId);
    }
}
