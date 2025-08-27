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
    // For local/testing scenarios where the Dynamic policy forwards to "NoAuth",
    // return a lightweight authenticated principal so [Authorize] succeeds.
    var claims = new[] { new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, "LocalDev") };
    var identity = new System.Security.Claims.ClaimsIdentity(claims, Scheme.Name);
    var principal = new System.Security.Claims.ClaimsPrincipal(identity);
    var ticket = new AuthenticationTicket(principal, Scheme.Name);
    return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
