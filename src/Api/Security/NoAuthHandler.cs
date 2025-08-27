using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Text.Encodings.Web;
using Microsoft.Extensions.Logging;

namespace Api.Security;

public class NoAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public NoAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, TimeProvider timeProvider)
        : base(options, logger, encoder)
    {
        if (Options is AuthenticationSchemeOptions authOptions)
        {
            authOptions.TimeProvider = timeProvider;
        }
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // No authentication performed; return NoResult so authorization can handle unauthenticated requests.
        return Task.FromResult(AuthenticateResult.NoResult());
    }
}
