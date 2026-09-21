using CryptoPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CryptoPlatform.Persistence.Configurations;

public sealed class SysAuditLogConfiguration : IEntityTypeConfiguration<SysAuditLog>
{
    public void Configure(EntityTypeBuilder<SysAuditLog> b)
    {
        b.ToTable("sys_audit_log");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        b.Property(x => x.AuditId).HasColumnName("audit_id").HasMaxLength(64).IsRequired();
        b.Property(x => x.RequestId).HasColumnName("request_id").HasMaxLength(64);
        b.Property(x => x.TraceId).HasColumnName("trace_id").HasMaxLength(64);
        b.Property(x => x.Timestamp).HasColumnName("timestamp");
        b.Property(x => x.OperatorType).HasColumnName("operator_type").HasMaxLength(16).IsRequired();
        b.Property(x => x.OperatorId).HasColumnName("operator_id").HasMaxLength(64);
        b.Property(x => x.OperatorName).HasColumnName("operator_name").HasMaxLength(128);
        b.Property(x => x.AppId).HasColumnName("app_id").HasMaxLength(64);
        b.Property(x => x.SourceIp).HasColumnName("source_ip").HasMaxLength(45);
        b.Property(x => x.Operation).HasColumnName("operation").HasMaxLength(64).IsRequired();
        b.Property(x => x.KeyId).HasColumnName("key_id").HasMaxLength(64);
        b.Property(x => x.KeyVersion).HasColumnName("key_version");
        b.Property(x => x.ResultCode).HasColumnName("result_code").HasMaxLength(32).IsRequired();
        b.Property(x => x.DurationMs).HasColumnName("duration_ms");
        b.Property(x => x.PreviousHash).HasColumnName("previous_hash").HasMaxLength(128);
        b.Property(x => x.CurrentHash).HasColumnName("current_hash").HasMaxLength(128).IsRequired();
        b.Property(x => x.CreatedAt).HasColumnName("created_at");
        b.HasIndex(x => x.AuditId).IsUnique().HasDatabaseName("uk_audit_id");
        b.HasIndex(x => x.Timestamp).HasDatabaseName("ix_audit_timestamp");
        b.HasIndex(x => new { x.AppId, x.Timestamp }).HasDatabaseName("ix_audit_app_time");
        b.HasIndex(x => new { x.KeyId, x.Timestamp }).HasDatabaseName("ix_audit_key_time");
        b.HasIndex(x => new { x.Operation, x.Timestamp }).HasDatabaseName("ix_audit_operation");
    }
}
