using CryptoPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CryptoPlatform.Persistence.Configurations;

public sealed class SysConfigConfiguration : IEntityTypeConfiguration<SysConfig>
{
    public void Configure(EntityTypeBuilder<SysConfig> b)
    {
        b.ToTable("sys_config");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.ConfigKey).HasMaxLength(128).IsRequired();
        b.Property(x => x.ConfigValue).HasMaxLength(2000).IsRequired();
        b.Property(x => x.Description).HasMaxLength(256);
        b.Property(x => x.UpdatedBy).HasMaxLength(64);
        b.HasIndex(x => x.ConfigKey).IsUnique();
    }
}
