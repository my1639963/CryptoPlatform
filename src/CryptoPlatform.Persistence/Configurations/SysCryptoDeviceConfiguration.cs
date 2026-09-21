using CryptoPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CryptoPlatform.Persistence.Configurations;

public sealed class SysCryptoDeviceConfiguration : IEntityTypeConfiguration<SysCryptoDevice>
{
    public void Configure(EntityTypeBuilder<SysCryptoDevice> b)
    {
        b.ToTable("sys_crypto_device");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        b.Property(x => x.DeviceId).HasColumnName("device_id").HasMaxLength(64).IsRequired();
        b.Property(x => x.DeviceName).HasColumnName("device_name").HasMaxLength(128).IsRequired();
        b.Property(x => x.Vendor).HasColumnName("vendor").HasMaxLength(64).IsRequired();
        b.Property(x => x.Model).HasColumnName("model").HasMaxLength(64);
        b.Property(x => x.SerialNumber).HasColumnName("serial_number").HasMaxLength(128);
        b.Property(x => x.Endpoint).HasColumnName("endpoint").HasMaxLength(256);
        b.Property(x => x.ProviderType).HasColumnName("provider_type").HasMaxLength(32).IsRequired();
        b.Property(x => x.Status).HasColumnName("status").HasMaxLength(16).IsRequired();
        b.Property(x => x.Priority).HasColumnName("priority");
        b.Property(x => x.IsPrimary).HasColumnName("is_primary");
        b.Property(x => x.LastHealthAt).HasColumnName("last_health_at");
        b.Property(x => x.ErrorCount).HasColumnName("error_count");
        b.Property(x => x.CreatedAt).HasColumnName("created_at");
        b.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        b.HasIndex(x => x.DeviceId).IsUnique().HasDatabaseName("uk_device_id");
        b.HasIndex(x => new { x.ProviderType, x.Status }).HasDatabaseName("ix_device_provider");
    }
}
