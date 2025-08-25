using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Api.Services;

public sealed class HttpContextHeadersDelegatingHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<HttpContextHeadersDelegatingHandler> _logger;
    private readonly HeaderPropagationOptions _options;

    public HttpContextHeadersDelegatingHandler(
        IHttpContextAccessor httpContextAccessor,
        ILogger<HttpContextHeadersDelegatingHandler> logger,
        Microsoft.Extensions.Options.IOptions<HeaderPropagationOptions> options)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? new HeaderPropagationOptions();
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        try
        {
            var context = _httpContextAccessor.HttpContext;
            if (context != null)
            {
                var headersToCopy = _options.HeadersToPropagate ?? new List<string>();
                foreach (var header in headersToCopy)
                {
                    if (context.Request.Headers.TryGetValue(header, out var values))
                    {
                        // Avoid duplicating headers
                        if (request.Headers.Contains(header))
                            continue;

                        // Use TryAddWithoutValidation to preserve header formatting
                        var vals = values.ToArray();
                        request.Headers.TryAddWithoutValidation(header, vals);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to propagate headers to outgoing request");
        }

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
