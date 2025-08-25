using Asp.Versioning;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Extensions;

public static class ApiVersioningExtensions
{
    public static IServiceCollection AddApiVersioningConfigured(this IServiceCollection services)
    {
        services.AddApiVersioning(o =>
        {
            o.DefaultApiVersion = new ApiVersion(1, 0);
            o.AssumeDefaultVersionWhenUnspecified = true;
            o.ReportApiVersions = true;
            o.ApiVersionReader = new UrlSegmentApiVersionReader();
        })
        .AddApiExplorer(o =>
        {
            o.GroupNameFormat = "'v'VVV";
            o.SubstituteApiVersionInUrl = true;
        });

        return services;
    }
}
