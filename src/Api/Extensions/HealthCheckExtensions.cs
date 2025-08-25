using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Api.Extensions;

public static class HealthCheckExtensions
{
    public static IServiceCollection AddHealthChecksConfigured(this IServiceCollection services, IConfiguration _)
    {
        services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy("OK"));

        return services;
    }
}
