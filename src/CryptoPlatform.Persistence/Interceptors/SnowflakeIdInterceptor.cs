using CryptoPlatform.Persistence.IdGeneration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace CryptoPlatform.Persistence.Interceptors;

/// <summary>
/// SaveChanges 拦截器：自动为新增实体分配雪花 ID。
/// 当实体的 Id 属性为默认值 0 时，由雪花 ID 生成器分配唯一 ID。
/// </summary>
public sealed class SnowflakeIdInterceptor : SaveChangesInterceptor
{
    private readonly SnowflakeIdGenerator _idGenerator;

    public SnowflakeIdInterceptor(SnowflakeIdGenerator idGenerator)
    {
        _idGenerator = idGenerator;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, InterceptionResult<int> result)
    {
        AssignIds(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        AssignIds(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void AssignIds(DbContext? context)
    {
        if (context is null) return;

        var entries = context.ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added);

        foreach (var entry in entries)
        {
            var idProperty = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "Id");
            if (idProperty is not null && idProperty.CurrentValue is not null && (long)idProperty.CurrentValue == 0L)
            {
                idProperty.CurrentValue = _idGenerator.NextId();
            }
        }
    }
}
