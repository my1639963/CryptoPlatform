using CryptoPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CryptoPlatform.Persistence.Configurations;

public sealed class SysIdempotencyRecordConfiguration : IEntityTypeConfiguration<SysIdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<SysIdempotencyRecord> b)
    {
        b.ToTable("sys_idempotency_record");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        b.Property(x => x.AppId).HasColumnName("app_id").HasMaxLength(64).IsRequired();
        b.Property(x => x.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(128).IsRequired();
        b.Property(x => x.HttpMethod).HasColumnName("http_method").HasMaxLength(10).IsRequired();
        b.Property(x => x.RequestPath).HasColumnName("request_path").HasMaxLength(256).IsRequired();
        b.Property(x => x.RequestHash).HasColumnName("request_hash").HasMaxLength(128).IsRequired();
        b.Property(x => x.ResponseCode).HasColumnName("response_code");
        b.Property(x => x.ResponseBodyHash).HasColumnName("response_body_hash").HasMaxLength(128);
        b.Property(x => x.Status).HasColumnName("status").HasMaxLength(16).IsRequired();
        b.Property(x => x.CreatedAt).HasColumnName("created_at");
        b.Property(x => x.ExpiresAt).HasColumnName("expires_at");
        b.HasIndex(x => new { x.AppId, x.HttpMethod, x.RequestPath, x.IdempotencyKey }).IsUnique().HasDatabaseName("uk_idempotency");
        b.HasIndex(x => x.ExpiresAt).HasDatabaseName("ix_idempotency_expires");
    }
}
