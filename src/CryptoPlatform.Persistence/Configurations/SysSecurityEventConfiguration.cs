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
        b.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        b.Property(x => x.EventId).HasColumnName("event_id").HasMaxLength(64).IsRequired();
        b.Property(x => x.EventType).HasColumnName("event_type").HasMaxLength(64).IsRequired();
        b.Property(x => x.Severity).HasColumnName("severity").HasMaxLength(16).IsRequired();
        b.Property(x => x.AppId).HasColumnName("app_id").HasMaxLength(64);
        b.Property(x => x.OperatorId).HasColumnName("operator_id").HasMaxLength(64);
        b.Property(x => x.SourceIp).HasColumnName("source_ip").HasMaxLength(45);
        b.Property(x => x.RelatedKeyId).HasColumnName("related_key_id").HasMaxLength(64);
        b.Property(x => x.RequestId).HasColumnName("request_id").HasMaxLength(64);
        b.Property(x => x.Description).HasColumnName("description").HasMaxLength(1000);
        b.Property(x => x.Status).HasColumnName("status").HasMaxLength(16).IsRequired();
        b.Property(x => x.DetectedAt).HasColumnName("detected_at");
        b.Property(x => x.HandledAt).HasColumnName("handled_at");
        b.Property(x => x.Handler).HasColumnName("handler").HasMaxLength(64);
        b.Property(x => x.HandlingResult).HasColumnName("handling_result").HasMaxLength(500);
        b.Property(x => x.CreatedAt).HasColumnName("created_at");
        b.HasIndex(x => x.EventId).IsUnique().HasDatabaseName("uk_event_id");
        b.HasIndex(x => new { x.EventType, x.Status }).HasDatabaseName("ix_event_type_status");
        b.HasIndex(x => new { x.Severity, x.Status }).HasDatabaseName("ix_event_severity");
        b.HasIndex(x => x.DetectedAt).HasDatabaseName("ix_event_detected");
    }
}
