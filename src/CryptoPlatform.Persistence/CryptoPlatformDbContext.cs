using CryptoPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CryptoPlatform.Persistence;

public sealed class CryptoPlatformDbContext : DbContext
{
    public CryptoPlatformDbContext(DbContextOptions<CryptoPlatformDbContext> options)
        : base(options) { }

    public DbSet<SysApplication> Applications => Set<SysApplication>();
    public DbSet<SysApplicationSecret> ApplicationSecrets => Set<SysApplicationSecret>();
    public DbSet<SysKey> Keys => Set<SysKey>();
    public DbSet<SysKeyVersion> KeyVersions => Set<SysKeyVersion>();
    public DbSet<SysKeyAuthorization> KeyAuthorizations => Set<SysKeyAuthorization>();
    public DbSet<SysCryptoDevice> CryptoDevices => Set<SysCryptoDevice>();
    public DbSet<SysUser> Users => Set<SysUser>();
    public DbSet<SysRole> Roles => Set<SysRole>();
    public DbSet<SysPermission> Permissions => Set<SysPermission>();
    public DbSet<SysUserRole> UserRoles => Set<SysUserRole>();
    public DbSet<SysAuditLog> AuditLogs => Set<SysAuditLog>();
    public DbSet<SysSecurityEvent> SecurityEvents => Set<SysSecurityEvent>();
    public DbSet<SysConfig> Configs => Set<SysConfig>();
    public DbSet<SysIdempotencyRecord> IdempotencyRecords => Set<SysIdempotencyRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CryptoPlatformDbContext).Assembly);
    }
}
