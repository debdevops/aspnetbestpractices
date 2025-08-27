# API - Features and How to Test

This document explains the production-ready features included in the `Api` project and gives quick, copyable examples to test them locally (PowerShell and curl). It intentionally focuses on Auth, Idempotency, Cache/ETags, and Security (CSP) for developer testing and verification.

## Quick: run locally

From the `src/Api` folder run the http profile (this sets `ASPNETCORE_ENVIRONMENT=Local` in the provided launch profile):

```powershell
cd C:\Users\debas\source\repos\aspnetbestpractices\src\Api
dotnet run --launch-profile http
```

On startup you should see a log line indicating whether authentication is disabled for local/testing.

Swagger UI: http://localhost:5232/swagger

## 1) Authentication

- What: The app supports multiple schemes and a policy scheme called `Dynamic` that selects the active provider at runtime. Supported providers: `ApiKey`, `Jwt`, `AzureAd`. A `NoAuth` scheme exists for no-op authentication.
- How it's controlled:
  - `ASPNETCORE_ENVIRONMENT=Local` (launch profile) will skip authentication/authorization middleware for easier local testing.
  - `Features:EnableAuth` feature flag in configuration can also disable auth when `false`.
  - The active provider is read from `Authentication:Provider` in configuration.

Local testing notes:
- For convenience the project is configured to skip auth when running with the `Local` profile or when `Features:EnableAuth=false` (see `Properties/launchSettings.json`).
- The project uses a `NoAuth` handler in Local to provide a lightweight principal so `[Authorize]` endpoints succeed during local testing (no production credentials required).

ApiKey sample (when auth is enabled):

curl:
```bash
curl -X POST "http://localhost:5232/api/v1/todos" \
  -H "Content-Type: application/json" \
  -H "X-API-Key: YOUR_API_KEY_HERE" \
  -d '{"title":"task","notes":"notes"}'
```

PowerShell:
```powershell
$headers = @{ 'X-API-Key' = 'YOUR_API_KEY_HERE' }
$body = @{ title = 'task'; notes = 'notes' } | ConvertTo-Json
Invoke-RestMethod -Method Post -Uri 'http://localhost:5232/api/v1/todos' -ContentType 'application/json' -Body $body -Headers $headers
```

## 2) Idempotency

- What: Idempotency is implemented as a middleware (`IdempotencyMiddleware`). It:
  - Only applies to `POST` requests with an `Idempotency-Key` header.
  - Validates that the key is a GUID and <= 128 chars.
  - Hashes the request body and stores successful 2xx responses in memory for a TTL (12 hours by default).
  - Uses a short lock (SemaphoreSlim) per key/path to avoid concurrent duplicate work.

How to test (PowerShell):
```powershell
$body = @{ title = 'idem test'; notes = 'idem' } | ConvertTo-Json
$headers = @{ 'Idempotency-Key' = [guid]::NewGuid().ToString() }
Invoke-RestMethod -Method Post -Uri 'http://localhost:5232/api/v1/todos' -ContentType 'application/json' -Body $body -Headers $headers
# Run again with same Idempotency-Key and same body - middleware should return cached response
```

Notes:
- If a cached response is returned you will see the response header `Idempotency-Cache: hit` (or `miss` on first request).
- Responses are only cached for successful 2xx responses and subject to a max payload size in memory.

## 3) Cache & ETags

- The repository returns and sets `ETag` response headers for resources. Consumers should use `If-Match` when updating (PUT) to ensure optimistic concurrency.
- Example update flow:
  1. GET `/api/v1/todos/{id}` -> inspect `ETag` response header.
  2. PUT `/api/v1/todos/{id}` with header `If-Match: <etag-value>` to update. A 412 Precondition Failed is returned if ETag doesn't match.

## 4) Security (CSP + headers)

- The app sets a set of security headers via `UseSecurityHeaders()` including:
  - `Content-Security-Policy` (configurable), `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, `Cross-Origin-*` headers, and cache-control headers.
- CSP is configured via the `Security:Csp` configuration section. Defaults live in `CspOptions`.
- For developer convenience `Local` environment adds `unsafe-inline` and `unsafe-eval` to `script-src` and `unsafe-inline` to `style-src` so Swagger UI can run without CSP blocking inline CSS/JS.

If you need to run the app with strict CSP locally, remove or change the `Local` branch in `SecurityHeaderExtensions.cs`.

## 5) Rate limiting

- Rate limiting policies exist and are applied to controllers and health endpoints. The `TodosController` uses a public policy for general endpoints and an `ExpensivePolicy` for write operations.
- To test quickly, use single requests; hitting rate limits will return 429 with Retry-After headers.

## 6) Observability & Logging

- HTTP logging is enabled for a selected set of headers (User-Agent, Content-Type, Referer, traceparent).
- OpenTelemetry is configured (collector/exporter settings are controlled via configuration).

## Troubleshooting

- If you see `401 Unauthorized` for a POST locally:
  1. Confirm the app started with Local profile: startup log should say `Authentication/authorization disabled for local/testing (IsLocal=True, EnableAuth=False)`.
  2. Ensure you are not sending an invalid `Authorization` header from your client which could trigger auth handlers.
  3. Use the PowerShell examples above to make direct requests.

## Example requests (copy/paste)

GET all todos (PowerShell):
```powershell
Invoke-RestMethod -Method Get -Uri 'http://localhost:5232/api/v1/todos'
```

POST create todo (PowerShell):
```powershell
$body = @{ title = 'test'; notes = 'test' } | ConvertTo-Json
Invoke-RestMethod -Method Post -Uri 'http://localhost:5232/api/v1/todos' -ContentType 'application/json' -Body $body
```

POST with idempotency key (PowerShell):
```powershell
$body = @{ title = 'idem test'; notes = 'idem' } | ConvertTo-Json
$headers = @{ 'Idempotency-Key' = [guid]::NewGuid().ToString() }
Invoke-RestMethod -Method Post -Uri 'http://localhost:5232/api/v1/todos' -ContentType 'application/json' -Body $body -Headers $headers
```

### Try it (PowerShell quick steps)

Run the http launch profile, open Swagger, and verify NoAuth behavior:

```powershell
cd C:\Users\debas\source\repos\aspnetbestpractices\src\Api
dotnet run --launch-profile http
# Wait for startup log, then open Swagger in default browser (Windows PowerShell):
Start-Process 'http://localhost:5232/swagger'
```

Look for a startup log like: `Authentication/authorization disabled for local/testing` indicating `NoAuth` is active.

cURL equivalents are provided in the sections above.

## Next steps I can help with

- Expand this into a repository-level `docs/` page or root `README.md` with screenshots and flow diagrams.
- Add a short Postman collection or OpenAPI examples that include authentication flows.
- Revert the temporary `[AllowAnonymous]` change and add a secure local test token/provider if you prefer.

---

Completion summary:
- Added `src/Api/README.md` documenting Auth, Idempotency, Cache/ETags, CSP, Rate limiting, and examples.
- You already confirmed `POST /api/v1/todos` returns 201 locally.

If you want, I will also add a `docs/FEATURES.md` at repo root and a small Postman (JSON) collection for the main flows — confirm and I’ll create them.
