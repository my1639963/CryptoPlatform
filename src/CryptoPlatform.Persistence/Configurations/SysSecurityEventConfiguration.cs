using CryptoPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CryptoPlatform.Persistence.Configurations;

public sealed class SysSecurityEventConfiguration : IEntityTypeConfiguration<SysSecurityEvent>
{
    public void Configure(EntityTypeBuilder<SysSecurityEvent> b)
    {
        b.ToTable("sys_security_event");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.EventId).HasMaxLength(64).IsRequired();
        b.Property(x => x.EventType).HasMaxLength(64).IsRequired();
        b.Property(x => x.Severity).HasMaxLength(16).IsRequired();
        b.Property(x => x.AppId).HasMaxLength(64);
        b.Property(x => x.Description).HasMaxLength(1000);
        b.Property(x => x.Status).HasMaxLength(16).IsRequired();
        b.Property(x => x.Handler).HasMaxLength(64);
        b.Property(x => x.HandlingResult).HasMaxLength(500);
        b.HasIndex(x => x.EventId).IsUnique();
        b.HasIndex(x => new { x.EventType, x.Status });
        b.HasIndex(x => new { x.Severity, x.Status });
        b.HasIndex(x => x.DetectedAt);
    }
}
