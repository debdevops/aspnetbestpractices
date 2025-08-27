using System.Linq;
using System.IO.Compression;
using Asp.Versioning;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;
using Api.Extensions;
using Api.Middleware;
using Api.Security;
using Api.Repositories;
using Api.Services;
using FluentValidation;
using Polly;
using Polly.Extensions.Http;
using Polly.Contrib.WaitAndRetry;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using Microsoft.AspNetCore.Authentication;
// feature management
using Microsoft.FeatureManagement;

var builder = WebApplication.CreateBuilder(args);

ConfigurationValidator.ValidateConfiguration(builder.Configuration);

// Kestrel hardening
builder.WebHost.UseKestrel(o =>
{
    o.AddServerHeader = false;
    o.Limits.MaxRequestBodySize = 10 * 1024 * 1024; // 10 MB
    o.Limits.KeepAliveTimeout = TimeSpan.FromSeconds(60);
    o.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(15);
    o.Limits.MaxRequestLineSize = 8 * 1024;
    o.Limits.MaxRequestHeadersTotalSize = 32 * 1024;
});

// Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Forwarded headers (when behind a proxy)
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    o.KnownNetworks.Clear();
    o.KnownProxies.Clear();
});

// HSTS options (used outside Development)
builder.Services.AddHsts(o =>
{
    o.Preload = true;
    o.IncludeSubDomains = true;
    o.MaxAge = TimeSpan.FromDays(365);
});

// Options
builder.Services.Configure<CspOptions>(options =>
{
    var cspSection = builder.Configuration.GetSection("Security:Csp");
    cspSection.Bind(options);

    // Validate CSP configuration
    if (string.IsNullOrWhiteSpace(options.DefaultSrc))
        throw new InvalidOperationException("CSP DefaultSrc cannot be empty");
});

// API Versioning
builder.Services.AddApiVersioningConfigured();

// Controllers + binder limits + JSON
builder.Services.AddControllers(options =>
{
    options.MaxModelBindingCollectionSize = 1000;
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
})
.AddJsonOptions(o =>
{
    o.JsonSerializerOptions.PropertyNamingPolicy = null;
    o.JsonSerializerOptions.WriteIndented = false;
    o.JsonSerializerOptions.MaxDepth = 32;
});

// FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// ProblemDetails (400)
builder.Services.AddProblemDetailsConfigured();

// Swagger
builder.Services.AddSwaggerConfigured();

// Health Checks
builder.Services.AddHealthChecksConfigured(builder.Configuration);

// Rate Limiting
builder.Services.AddRateLimitingConfigured();

// OpenTelemetry
builder.Services.AddOpenTelemetryConfigured(builder.Configuration);

// HTTP Logging (allow-list headers)
builder.Services.AddHttpLogging(o =>
{
    o.LoggingFields = HttpLoggingFields.RequestPropertiesAndHeaders |
                      HttpLoggingFields.ResponsePropertiesAndHeaders;
    o.RequestHeaders.Clear();
    o.RequestHeaders.Add("User-Agent");
    o.RequestHeaders.Add("Content-Type");
    o.RequestHeaders.Add("Referer");
    o.RequestHeaders.Add("traceparent");
    o.ResponseHeaders.Clear();
    o.ResponseHeaders.Add("Content-Type");
    o.ResponseHeaders.Add("traceparent");
});

// Response Compression
builder.Services.AddResponseCompression(opts =>
{
    opts.EnableForHttps = true;
    opts.Providers.Add<BrotliCompressionProvider>();
    opts.Providers.Add<GzipCompressionProvider>();
    opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[]
    {
        "application/json",
        "application/problem+json"
    });
});
builder.Services.Configure<BrotliCompressionProviderOptions>(o => o.Level = CompressionLevel.Fastest);
builder.Services.Configure<GzipCompressionProviderOptions>(o => o.Level = CompressionLevel.Fastest);

// Request Decompression (gzip, br, deflate by default)
builder.Services.AddRequestDecompression();

// CORS (only if called from browsers) - allowed origins read from configuration
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                     ?? new[] { "https://frontend.example" };

builder.Services.AddCors(o =>
{
    o.AddPolicy("api", p => p
        .WithOrigins(allowedOrigins)
        .WithMethods("GET", "POST", "PUT", "DELETE")
        .AllowAnyHeader()
        .WithExposedHeaders("ETag", "api-supported-versions")
        .SetPreflightMaxAge(TimeSpan.FromHours(1)));
});

// Configure header propagation options
builder.Services.Configure<HeaderPropagationOptions>(builder.Configuration.GetSection("HeaderPropagation"));

// Feature management for runtime toggles
builder.Services.AddFeatureManagement();

// Add authorization services
builder.Services.AddAuthorization();

// Register known schemes: ApiKey, JwtBearer (named "Jwt"), AzureAd (registered via Microsoft.Identity.Web as JwtBearer)
builder.Services.AddAuthentication()
    .AddScheme<ApiKeyAuthenticationSchemeOptions, ApiKeyAuthenticationHandler>("ApiKey", options =>
    {
        options.ApiKey = builder.Configuration["Authentication:ApiKey"];
    })
    .AddJwtBearer("Jwt", options =>
    {
        options.Authority = builder.Configuration["Authentication:JwtAuthority"];
        options.Audience = builder.Configuration["Authentication:JwtAudience"];
    })
    // Azure AD binding; this will register JwtBearer as well and can be selected dynamically
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

// Add a policy scheme named "Dynamic" that selects the active scheme at runtime
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "Dynamic";
})
.AddPolicyScheme("Dynamic", "Dynamic authentication scheme", options =>
{
    options.ForwardDefaultSelector = context =>
    {
        // If feature flag to enable auth is false, return a scheme that does no authentication
        var featMgr = context.RequestServices.GetService<Microsoft.FeatureManagement.IFeatureManagerSnapshot>();
        var cfg = context.RequestServices.GetService<IConfiguration>();

        var enableAuth = true;
        try
        {
            // Feature name is 'EnableAuth' under Features
            enableAuth = featMgr?.IsEnabledAsync("EnableAuth").GetAwaiter().GetResult() ?? true;
        }
        catch { enableAuth = true; }

        if (!enableAuth)
            return "NoAuth";

        var providerStr = cfg?["Authentication:Provider"] ?? "AzureAd";
        return providerStr switch
        {
            "ApiKey" => "ApiKey",
            "Jwt" => "Jwt",
            "AzureAd" => JwtBearerDefaults.AuthenticationScheme,
            _ => JwtBearerDefaults.AuthenticationScheme
        };
    };
});

// Register NoAuth scheme
builder.Services.AddAuthentication().AddScheme<AuthenticationSchemeOptions, Api.Security.NoAuthHandler>("NoAuth", _ => { });


// Middlewares & cache
builder.Services.AddTransient<ExceptionHandlingMiddleware>();
builder.Services.AddTransient<JsonOnlyMiddleware>();
builder.Services.AddTransient<IdempotencyMiddleware>();
builder.Services.AddMemoryCache();

// Add to Program.cs
builder.Logging.AddFilter("Microsoft.AspNetCore", LogLevel.Warning);
builder.Logging.AddFilter("System.Net.Http.HttpClient", LogLevel.Warning);

// Add structured logging
builder.Services.Configure<JsonConsoleFormatterOptions>(options =>
{
    options.IncludeScopes = true;
    options.TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff";
    options.UseUtcTimestamp = true;
});

// Polly: v7-style jittered retries, per-try timeout, circuit breaker
var jitterDelays = Backoff.DecorrelatedJitterBackoffV2(TimeSpan.FromMilliseconds(200), 3).ToArray();

IAsyncPolicy<HttpResponseMessage> RetryPolicy() =>
    HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(r => (int)r.StatusCode == 429)
        .OrResult(r => r.StatusCode == System.Net.HttpStatusCode.RequestTimeout)
        .WaitAndRetryAsync(
            retryCount: jitterDelays.Length,
            sleepDurationProvider: retryAttempt => jitterDelays[Math.Max(0, retryAttempt - 1)]
        );

var perTryTimeout = Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(5));
var circuitBreaker = HttpPolicyExtensions
    .HandleTransientHttpError()
    .CircuitBreakerAsync(handledEventsAllowedBeforeBreaking: 5, durationOfBreak: TimeSpan.FromSeconds(30));

// HttpClient + policies (idempotency-aware retries)
builder.Services.AddHttpClient("DownstreamApi", client =>
{
    var baseUrl = builder.Configuration["Downstream:BaseUrl"];
    if (string.IsNullOrWhiteSpace(baseUrl) || !Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri))
        throw new InvalidOperationException("Invalid Downstream:BaseUrl configuration");
    
    client.BaseAddress = uri;
    client.Timeout = TimeSpan.FromSeconds(10);
})

.AddHttpMessageHandler<HttpContextHeadersDelegatingHandler>()

.AddPolicyHandler((sp, req) =>
{
    var method = req.Method.Method?.ToUpperInvariant();
    var isIdempotent = method is "GET" or "PUT" or "DELETE" or "HEAD";
    var hasIdemKey = req.Headers.Contains("Idempotency-Key");
    return isIdempotent || hasIdemKey
        ? Policy.WrapAsync(perTryTimeout, RetryPolicy(), circuitBreaker)
        : Policy.WrapAsync(perTryTimeout, circuitBreaker);
});

builder.Services.AddHttpClient<IDownstreamClient, DownstreamClient>("DownstreamApi");

// Allow access to HttpContext when preparing outgoing requests
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<HttpContextHeadersDelegatingHandler>();

// Repository
builder.Services.AddSingleton<ITodoRepository, InMemoryTodoRepository>();

builder.Services.Configure<FormOptions>(options =>
{
    options.ValueLengthLimit = 4096;
    options.MultipartBodyLengthLimit = 10 * 1024 * 1024; // 10MB
    options.MultipartHeadersLengthLimit = 16384;
});

var app = builder.Build();

// Forwarded headers (must be before redirection)
app.UseForwardedHeaders();

// Security headers & HTTPS
app.UseSecurityHeaders();
app.UseHttpsRedirection();

// Rate Limiting
app.UseRateLimiter();

// HTTP logging
app.UseHttpLogging();

// Decompress requests BEFORE body-reading middlewares
app.UseRequestDecompression();

// JSON-only + Idempotency
app.UseMiddleware<JsonOnlyMiddleware>();
app.UseMiddleware<IdempotencyMiddleware>();

// Compress responses
app.UseResponseCompression();

// Swagger in Development and Local environments (Local used by launchSettings.json)
if (app.Environment.IsDevelopment() || string.Equals(app.Environment.EnvironmentName, "Local", StringComparison.OrdinalIgnoreCase))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// CORS (if needed)
app.UseCors("api");

// Authentication & Authorization
// In production-like environments enable authentication and authorization.
// For local developer workflows (ASPNETCORE_ENVIRONMENT=Local) we skip these
// middlewares to make manual testing and Swagger exploration easier.
var isLocalEnv = string.Equals(app.Environment.EnvironmentName, "Local", StringComparison.OrdinalIgnoreCase);
// Also allow disabling auth via configuration (Features:EnableAuth = false) for CI/local runs
var enableAuth = app.Configuration.GetValue<bool?>("Features:EnableAuth") ?? true;
if (isLocalEnv || !enableAuth)
{
    app.Logger.LogInformation("Authentication/authorization disabled for local/testing (IsLocal={IsLocal}, EnableAuth={EnableAuth})", isLocalEnv, enableAuth);
}
else
{
    // Ensure authentication runs before authorization and before hitting controllers
    app.UseAuthentication();
    app.UseAuthorization();
}

// Controllers
app.MapControllers().RequireRateLimiting(RateLimitingExtensions.PublicPolicy);

// Health endpoints
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false })
   .RequireRateLimiting(RateLimitingExtensions.HealthPolicy);

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = async (ctx, report) =>
    {
        ctx.Response.ContentType = "application/json";
        var payload = new
        {
            status = report.Status.ToString(),
            results = report.Entries.ToDictionary(
                e => e.Key,
                e => new { status = e.Value.Status.ToString(), description = e.Value.Description })
        };
        await ctx.Response.WriteAsJsonAsync(payload);
    }
}).RequireRateLimiting(RateLimitingExtensions.HealthPolicy);

app.Run();

public partial class Program { }
