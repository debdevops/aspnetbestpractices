using Api.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Api.Extensions;

public static class SecurityHeaderExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
    {
        var env  = app.ApplicationServices.GetRequiredService<IHostEnvironment>();
        var opts = app.ApplicationServices.GetRequiredService<IOptions<CspOptions>>().Value;

        if (!env.IsDevelopment())
        {
            app.UseHsts();
        }

        app.Use(async (ctx, next) =>
        {
            ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
            ctx.Response.Headers["X-Frame-Options"] = "DENY";
            ctx.Response.Headers["Referrer-Policy"] = "no-referrer";
            ctx.Response.Headers["X-XSS-Protection"] = "0";
            ctx.Response.Headers["Cross-Origin-Resource-Policy"] = "same-site";
            ctx.Response.Headers["Cross-Origin-Opener-Policy"] = "same-origin"; 
            ctx.Response.Headers["Cross-Origin-Embedder-Policy"] = "require-corp";
            ctx.Response.Headers.CacheControl = "no-store";
            ctx.Response.Headers.Pragma = "no-cache";
            ctx.Response.Headers.Remove("Server");

        // Treat the 'Local' environment like Development for developer-time conveniences
        // (e.g. allowing inline scripts/styles for Swagger UI). This prevents CSP from
        // blocking Swagger's inline styles when running via the 'Local' profile.
        var isDev = env.IsDevelopment() || string.Equals(env.EnvironmentName, "Local", StringComparison.OrdinalIgnoreCase);
            var devScript = $"{opts.ScriptSrc} 'unsafe-inline' 'unsafe-eval'";
            var devStyle  = $"{opts.StyleSrc} 'unsafe-inline'";

            var csp = CspPolicies.Build(new CspOptions
            {
                DefaultSrc     = opts.DefaultSrc,
                ScriptSrc      = isDev ? devScript : opts.ScriptSrc,
                StyleSrc       = isDev ? devStyle  : opts.StyleSrc,
                ImgSrc         = opts.ImgSrc,
                FontSrc        = opts.FontSrc,
                ConnectSrc     = opts.ConnectSrc,
                FrameAncestors = opts.FrameAncestors,
                ObjectSrc      = opts.ObjectSrc,
                BaseUri        = opts.BaseUri,
                FormAction     = opts.FormAction,
                EnableReportOnly = opts.EnableReportOnly
            });

            var headerName = opts.EnableReportOnly ? "Content-Security-Policy-Report-Only" : "Content-Security-Policy";
            ctx.Response.Headers[headerName] = csp;

            await next();
        });

        return app;
    }
}
