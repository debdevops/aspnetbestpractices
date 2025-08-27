# AspNetBestPractices

Template ASP.NET Core API project with sensible defaults for security, logging, and observability.

Quick start

1. Configure secrets via environment variables or `dotnet user-secrets` (do not store secrets in source).
2. Build and run:

```bash
dotnet build AspNetBestPractice.sln
dotnet run --project src/Api
```

Security notes

- Do not commit connection strings or API keys. Use environment variables or GitHub Secrets for CI.
- Configure `Downstream:BaseUrl` and allowed CORS origins in configuration for production.

## Features (detailed docs)

This repository implements a set of production-oriented best-practice features for ASP.NET Core APIs. A detailed set of tutorials and diagrams is available under the `docs/` folder — start at `docs/README.md`.

Key features implemented in this sample API:

- Authentication: multiple providers supported (`ApiKey`, `Jwt`, `AzureAd`) plus a `NoAuth` handler for local development. See `docs/auth.md` for examples and configuration.
- Idempotency: POST idempotency middleware requiring an `Idempotency-Key` header, body hashing, and caching of successful responses. See `docs/idempotency.md`.
- Cache & ETags: ETag generation and optimistic concurrency via `If-Match` headers for resource updates. See `docs/caching.md`.
- Security headers & CSP: configurable security headers including a Content-Security-Policy. Local profile relaxes CSP to allow Swagger to work during development. See `docs/csp.md`.
- Rate limiting: built-in policies to protect expensive operations and public endpoints. See `docs/rate-limiting.md`.
- Observability: OpenTelemetry instrumentation and logging guidance. See `docs/opentelemetry.md`.
- Error logging and Problem Details: consistent error handling middleware and problem-details responses. See `docs/error-logging.md`.
- Health checks: readiness and liveness endpoints and how to integrate them in platform deployments. See `docs/health.md`.

Screenshots and rendered flow diagrams are present under `screenshots/` and `docs/diagrams/` respectively. A CI workflow (`.github/workflows/render-diagrams.yml`) renders diagrams automatically on push.

If you want a short developer quick-start for the API itself, see `src/Api/README.md` which includes runnable examples for PowerShell and curl.
