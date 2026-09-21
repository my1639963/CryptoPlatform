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
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.DeviceId).HasMaxLength(64).IsRequired();
        b.Property(x => x.DeviceName).HasMaxLength(128).IsRequired();
        b.Property(x => x.Vendor).HasMaxLength(64).IsRequired();
        b.Property(x => x.Model).HasMaxLength(64);
        b.Property(x => x.SerialNumber).HasMaxLength(128);
        b.Property(x => x.Endpoint).HasMaxLength(256);
        b.Property(x => x.ProviderType).HasMaxLength(32).IsRequired();
        b.Property(x => x.Status).HasMaxLength(16).IsRequired();
        b.HasIndex(x => x.DeviceId).IsUnique();
        b.HasIndex(x => new { x.ProviderType, x.Status });
    }
}
