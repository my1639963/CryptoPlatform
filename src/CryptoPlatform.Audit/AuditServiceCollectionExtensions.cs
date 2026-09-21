using Microsoft.Extensions.DependencyInjection;

namespace CryptoPlatform.Audit;

public static class AuditServiceCollectionExtensions
{
    public static IServiceCollection AddAuditServices(this IServiceCollection services)
    {
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IHashChainService, HashChainService>();
        return services;
    }
}
