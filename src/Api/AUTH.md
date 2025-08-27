
# Authentication in this project

This document explains the switchable authentication system used by the API, how to configure it, and where to change values for local development or production.

Default recommended behavior for environments in this repository:

- Local (developer machine): No authentication (useful for quick iterations and running tests). The project looks for `appsettings.Local.json` and environment overrides.
- Development: Azure AD (`AzureAd`) by default. See the `appsettings.Development.json` sample below.

The API supports these authentication providers (selectable at runtime):

- `None` (no auth) — useful for local dev or tests when the feature flag is off.
- `ApiKey` — simple API key validation using a header.
- `Jwt` — JWT Bearer tokens issued by a compatible identity provider.
- `AzureAd` — Microsoft Identity / Azure AD using Microsoft.Identity.Web.

A runtime feature flag controls whether authentication is enforced. Configuration keys you need to know:

- `Features:EnableAuth` (bool) — when `false` the API will use the no-op handler and requests are unauthenticated.
- `Authentication:Provider` — one of `None`, `ApiKey`, `Jwt`, `AzureAd`.
- `Authentication:ApiKey` — the API key value used by the `ApiKey` provider.
- `Authentication:JwtAuthority` and `Authentication:JwtAudience` — values used by the `Jwt` provider.

Location: the project reads these values from `appsettings.json` (and environment overrides). The sample below shows the keys and marks the places you must change.

## Example `appsettings.json` snippet (copy into `src/Api/appsettings.json` or your environment-specific file)

```json
{
  "Features": {
    "EnableAuth": true
  },
  "Authentication": {
    "Provider": "AzureAd",    // <-- set to ApiKey | Jwt | AzureAd | None
    "JwtAuthority": "https://your-issuer.example.com/",
    "JwtAudience": "api://your-audience"
  },
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/"
  }
}
```

Secrets and IDs should not be stored in committed JSON files. Use environment variables. Recommended variable names (ASP.NET configuration maps __ to :)

- Azure AD

  - `AzureAd__TenantId` (tenant id)
  - `AzureAd__ClientId` (application/client id)
  - `AzureAd__ClientSecret` (client secret)

- Jwt

  - `Authentication__JwtAuthority` (issuer URL)
  - `Authentication__JwtAudience` (expected audience)

- ApiKey

  - `Authentication__ApiKey` (the shared API key value)

Example environment variable exports for Development (Azure AD):

```bash
export ASPNETCORE_ENVIRONMENT=Development
export AzureAd__TenantId=your-tenant-id
export AzureAd__ClientId=your-client-id
export AzureAd__ClientSecret=your-client-secret
export Features__EnableAuth=true
export Authentication__Provider=AzureAd
```

Example environment variable exports for Local (no auth):

```bash
export ASPNETCORE_ENVIRONMENT=Local
export Features__EnableAuth=false
export Authentication__Provider=None
```

Notes:

- If you set `Features:EnableAuth` to `false` the API will run without authenticating requests (the code registers a NoAuth handler). This is convenient for local development and the test project.
- When `Features:EnableAuth` is `true`, the runtime `Authentication:Provider` determines which scheme is used.

### Environment variable overrides

You can override configuration with environment variables. Examples for `zsh` (export in shell or a `.env` tool):

```bash
export DOTNET_ENVIRONMENT=Development
export Authentication__Provider=ApiKey
export Authentication__ApiKey=super-secret-local-key
export Features__EnableAuth=true
```

Replace values where indicated above.

### What header does the ApiKey provider expect?

The ApiKey handler compares a header value against `Authentication:ApiKey`. By default the project uses the custom ApiKey authentication handler. Confirm the header name in `src/Api/Extensions/ApiKeyAuthenticationHandler.cs` (look for the header key string). Common headers used are `X-Api-Key` or `Authorization` with a custom scheme — adjust your clients to match the handler.

Example curl using a header named `X-Api-Key`:

```bash
curl -v http://localhost:5000/api/v1/todos \
  -H "X-Api-Key: REPLACE_WITH_YOUR_API_KEY"
```

If your handler expects an `Authorization` header with scheme, use:

```bash
curl -v http://localhost:5000/api/v1/todos \
  -H "Authorization: ApiKey REPLACE_WITH_YOUR_API_KEY"
```

### JWT provider: testing locally

To test `Jwt` provider locally you can get a JWT from your identity provider (or mint a token with the right issuer/audience/claims) and send it as a Bearer token:

```bash
curl -v http://localhost:5000/api/v1/todos \
  -H "Authorization: Bearer <your-jwt-here>"
```

Make sure `Authentication:JwtAuthority` and `Authentication:JwtAudience` match the token's claims.

### Azure AD provider

When using `AzureAd` the project relies on `Microsoft.Identity.Web` configuration. Replace the `TenantId`, `ClientId`, and `ClientSecret` in the `AzureAd` section above with values from your Azure AD app registration. For production, prefer using a secrets store or managed identity instead of committing secrets to files.

### Tests and CI

The integration test project in `tests/Api.Tests` disables the auth enforcement at test-time by setting `Features:EnableAuth` to `false` through the test `WebApplicationFactory` configuration. If you need tests that validate auth behavior, add dedicated tests and configure the factory to enable auth and provide test credentials (for example, a test ApiKey or a local test token issuer).

### Quick checklist when enabling auth in an environment

- [ ] Set `Features:EnableAuth` to `true` (or leave true in production).
- [ ] Set `Authentication:Provider` to the desired provider.
- [ ] Provide secrets/IDs for the selected provider and verify they are set via environment variables or a secrets store (do not check secrets into git).
- [ ] Update firewall/CORS and downstream clients to send the appropriate header or bearer token.
- [ ] Run integration tests that exercise authenticated endpoints.

If you want, I can add a small `AUTH.md` or an `examples/` folder with ready-to-run docker-compose or scripts to mint local tokens for testing — tell me which provider you want to target and I will add it.

---

File location: `src/Api/AUTH.md`

### Production / Staging / QA

For production-like environments prefer injecting secrets via your CI/CD secret manager or platform environment variables (do not commit secrets in JSON). Use the same env var names described above (`AzureAd__*`, `Authentication__ApiKey`, etc.).

Example (CI/CD secret bindings):

- Azure: set `AZURE_WEBAPP_APPSETTING_AzureAd__ClientSecret` (or use KeyVault integration)
- Kubernetes: set env vars in the deployment manifest or use a secret-store integration

### Run locally on macOS with Swagger

To run the API locally and open the Swagger UI on a MacBook Air (zsh shell):

```bash
# 1) choose the environment (Local or Development). For Local (no auth):
export ASPNETCORE_ENVIRONMENT=Local

# 2) (optional) set any non-secret overrides. For Local you generally don't need secrets.

# 3) run the API
dotnet run --project src/Api/Api.csproj

# 4) open Swagger UI in the browser (default Kestrel URL is displayed in the terminal, commonly http://localhost:5000 or https://localhost:5001)
open "http://localhost:5000/swagger"
```

If you run with `ASPNETCORE_ENVIRONMENT=Development` ensure Azure AD env vars are set (see examples above) before starting the app.
