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
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.AppId).HasMaxLength(64).IsRequired();
        b.Property(x => x.IdempotencyKey).HasMaxLength(128).IsRequired();
        b.Property(x => x.HttpMethod).HasMaxLength(10).IsRequired();
        b.Property(x => x.RequestPath).HasMaxLength(256).IsRequired();
        b.Property(x => x.RequestHash).HasMaxLength(128).IsRequired();
        b.Property(x => x.ResponseBodyHash).HasMaxLength(128);
        b.Property(x => x.Status).HasMaxLength(16).IsRequired();
        b.HasIndex(x => new { x.AppId, x.HttpMethod, x.RequestPath, x.IdempotencyKey }).IsUnique();
        b.HasIndex(x => x.ExpiresAt);
    }
}
