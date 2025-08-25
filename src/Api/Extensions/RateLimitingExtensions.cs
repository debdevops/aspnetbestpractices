using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Extensions;

public static class RateLimitingExtensions
{
    public const string PublicPolicy    = "public";
    public const string ExpensivePolicy = "expensive";
    public const string HealthPolicy    = "health";

    public static IServiceCollection AddRateLimitingConfigured(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.AddFixedWindowLimiter(PublicPolicy, o =>
            {
                o.PermitLimit = 100;
                o.Window = TimeSpan.FromMinutes(1);
                o.QueueLimit = 0;
            });

            options.AddFixedWindowLimiter(HealthPolicy, o =>
            {
                o.PermitLimit = 30;
                o.Window = TimeSpan.FromMinutes(1);
                o.QueueLimit = 0;
            });

            options.AddFixedWindowLimiter(ExpensivePolicy, o =>
            {
                o.PermitLimit = 10;
                o.Window = TimeSpan.FromMinutes(1);
                o.QueueLimit = 0;
            });

            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        });

        return services;
    }
}
