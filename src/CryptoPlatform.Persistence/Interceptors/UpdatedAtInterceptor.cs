using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace CryptoPlatform.Persistence.Interceptors;

/// <summary>
/// SaveChanges 拦截器：自动维护 updated_at 字段。
/// 通过 EF Core 拦截器统一维护，不依赖数据库 ON UPDATE（兼容国产数据库）。
/// </summary>
public sealed class UpdatedAtInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, InterceptionResult<int> result)
    {
        UpdateTimestamps(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        UpdateTimestamps(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void UpdateTimestamps(DbContext? context)
    {
        if (context is null) return;

        var now = DateTime.UtcNow;
        var entries = context.ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified);

        foreach (var entry in entries)
        {
            // updated_at — 所有实体在修改时更新
            var updatedAtProperty = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "UpdatedAt");
            if (updatedAtProperty is not null)
            {
                updatedAtProperty.CurrentValue = now;
            }

            // created_at — 新增实体时设置
            if (entry.State == EntityState.Added)
            {
                var createdAtProperty = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "CreatedAt");
                if (createdAtProperty is not null)
                {
                    createdAtProperty.CurrentValue = now;
                }
            }
        }
    }
}
