using CryptoPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CryptoPlatform.Persistence.Configurations;

public sealed class SysApplicationSecretConfiguration : IEntityTypeConfiguration<SysApplicationSecret>
{
    public void Configure(EntityTypeBuilder<SysApplicationSecret> b)
    {
        b.ToTable("sys_application_secret");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.SecretHash).HasMaxLength(255).IsRequired();
        b.HasIndex(x => x.ApplicationId);
        b.HasIndex(x => new { x.Status, x.ExpiresAt });
    }
}
