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
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.AuditId).HasMaxLength(64).IsRequired();
        b.Property(x => x.RequestId).HasMaxLength(64);
        b.Property(x => x.TraceId).HasMaxLength(64);
        b.Property(x => x.OperatorType).HasMaxLength(16).IsRequired();
        b.Property(x => x.OperatorId).HasMaxLength(64);
        b.Property(x => x.OperatorName).HasMaxLength(128);
        b.Property(x => x.AppId).HasMaxLength(64);
        b.Property(x => x.SourceIp).HasMaxLength(45);
        b.Property(x => x.Operation).HasMaxLength(64).IsRequired();
        b.Property(x => x.KeyId).HasMaxLength(64);
        b.Property(x => x.ResultCode).HasMaxLength(32).IsRequired();
        b.Property(x => x.PreviousHash).HasMaxLength(128);
        b.Property(x => x.CurrentHash).HasMaxLength(128).IsRequired();
        b.HasIndex(x => x.AuditId).IsUnique();
        b.HasIndex(x => x.Timestamp);
        b.HasIndex(x => new { x.AppId, x.Timestamp });
        b.HasIndex(x => new { x.KeyId, x.Timestamp });
        b.HasIndex(x => new { x.Operation, x.Timestamp });
    }
}
