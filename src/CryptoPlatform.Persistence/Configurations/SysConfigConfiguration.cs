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
        b.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        b.Property(x => x.ConfigKey).HasColumnName("config_key").HasMaxLength(128).IsRequired();
        b.Property(x => x.ConfigValue).HasColumnName("config_value").HasMaxLength(2000).IsRequired();
        b.Property(x => x.Description).HasColumnName("description").HasMaxLength(256);
        b.Property(x => x.UpdatedBy).HasColumnName("updated_by").HasMaxLength(64);
        b.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        b.HasIndex(x => x.ConfigKey).IsUnique().HasDatabaseName("uk_config_key");
    }
}
