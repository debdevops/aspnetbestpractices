# Features Overview (Consolidated)

This file consolidates the main features of the sample API, includes screenshots and diagrams, and provides step-by-step validation instructions.

## Table of contents

- Idempotency
- Authentication
- CSP (Security)
- Caching & ETags
- Versioning
- Rate Limiting
- Error Logging
- OpenTelemetry
- Health Checks

---

## Idempotency

Description

The application implements idempotency for POST operations using `IdempotencyMiddleware` which requires `Idempotency-Key` headers and caches successful 2xx responses for a TTL.

Validate locally

1. Run the API locally.
2. POST with a unique `Idempotency-Key` and note `Idempotency-Cache: miss`.
3. Repeat the POST with same key and body to get `Idempotency-Cache: hit`.

Screenshot

![POST success screenshot](screenshots/HttpPost1.png)

Diagram

![Idempotency detailed flow](docs/diagrams/idempotency-detailed.png)

Notes & production hints

- Use a distributed cache (Redis) to share idempotency state across instances.
- Consider size limits and TTLs to protect memory.

---

## Authentication

Description

Supports multiple auth modes: `ApiKey`, `JWT`, `Azure AD`, and `NoAuth` for local development. A `Dynamic` policy chooses the provider using configuration and feature flags.

Validate locally

- Local mode (NoAuth): start with `http` profile. Protected endpoints return success without tokens.
- ApiKey: set `Authentication:Provider` to `ApiKey` and `Authentication:ApiKey` to the value; provide header `X-API-Key`.
- JWT/Azure: configure authority/audience or Azure settings and present a bearer token.

Screenshot

![POST success screenshot](screenshots/HttpPost1.png)

Diagram

![Auth dynamic flow](docs/diagrams/auth-flow.png)

Auth token sequence (JWT/interactive)

![Auth token sequence](docs/diagrams/auth-token-sequence.png)

Notes

- For production enable the feature flag `Features:EnableAuth` and configure a provider.

---

## CSP (Content Security Policy)

Description

CSP header is configured via `Security:Csp`. In `Local` environment the app relaxes CSP to allow inline styles/scripts for Swagger. For production configure strict sources.

Screenshot

![Swagger screenshot](screenshots/HttpGet1.png)

Diagram

![CSP flow](docs/diagrams/csp-flow.png)

---

## Caching & ETags

Description

Resources expose `ETag` headers for optimistic concurrency. Update requests should use `If-Match`.

Screenshot

![GET resource screenshot](screenshots/HttpGet2.png)

---

## Versioning

Description

Controller routes are versioned with `v{version:apiVersion}` and `Asp.Versioning` is used.

---

## Rate Limiting

Description

Rate limiting policies are applied for public endpoints, health, and expensive operations. Hits beyond limits return `429`.

Diagram

![Rate limiting flow](docs/diagrams/rate-limiting-flow.png)

---

## Error Logging

Description

Centralized `ExceptionHandlingMiddleware` produces ProblemDetails for errors and logs exceptions.

---

## OpenTelemetry

Description

OpenTelemetry is configured for tracing and metrics. Configure an OTLP exporter and a collector to capture traces.

Diagram

![OpenTelemetry flow](docs/diagrams/opentelemetry-flow.png)

---

## Health Checks

Endpoints

- `/health/live`
- `/health/ready`

Screenshot

![Health screenshot](screenshots/HttpGet1.png)

---

If you want I can:

- Embed the generated PNGs into the repo `docs/` directory (I already referenced them) and commit the rendered PNGs if you prefer to store them in the repo.
- Create a Postman collection for validation flows.
- Add a GitHub Action to render mermaid diagrams on push and commit the PNGs.
