using Microsoft.Extensions.DependencyInjection;

namespace CryptoPlatform.Security;

public static class SecurityServiceCollectionExtensions
{
    public static IServiceCollection AddSecurityServices(this IServiceCollection services)
    {
        services.AddScoped<ISecurityEventService, SecurityEventService>();
        return services;
    }
}
