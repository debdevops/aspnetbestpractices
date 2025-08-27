namespace Api.Security;

public enum AuthenticationProvider
{
    None,
    ApiKey,
    Jwt,
    AzureAd
}

public class AuthenticationOptions
{
    public AuthenticationProvider Provider { get; set; } = AuthenticationProvider.ApiKey;
    public string? ApiKey { get; set; }
    // For JWT: authority, audience, etc.
    public string? JwtAuthority { get; set; }
    public string? JwtAudience { get; set; }
    // For AzureAd: config will be read from standard Microsoft.Identity.Web settings
}
