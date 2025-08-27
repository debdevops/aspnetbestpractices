# Idempotency (Tutorial)

This project implements idempotency via `IdempotencyMiddleware`.

## What it does

- Applies when clients send an `Idempotency-Key` header (GUID, ≤ 128 chars).
- Hashes the request body (if present) and caches successful 2xx responses for 12 hours.
- Replays with `Idempotency-Cache: hit` when there is a match.

## Why use it

- Protects downstream systems from duplicate side-effects.
- Allows safe retries for clients.

## How to test locally

1) Run the API:

```bash
cd src/Api
dotnet run --launch-profile http
```

2) Create an idempotent POST (first request will be a miss):
```powershell
$body = @{ title = 'idem test'; notes = 'first' } | ConvertTo-Json
$key = [guid]::NewGuid().ToString()
Invoke-RestMethod -Method Post `
  -Uri 'http://localhost:5232/api/v1/todos?api-version=1.0' `
  -ContentType 'application/json' `
  -Body $body `
  -Headers @{ 'Idempotency-Key' = $key } `
  -Verbose
```

3) Repeat the same request with the same `Idempotency-Key` and same body. The response should come from the cache and the response header `Idempotency-Cache` will be `hit`.

Notes & edge cases
- If the request body changes but the same `Idempotency-Key` is used, the middleware computes a different body hash and treats it as distinct.
- Only 2xx responses are cached. 4xx/5xx responses are not cached.
- The middleware uses an in-memory `IMemoryCache` — for production use a distributed cache (Redis) is recommended so idempotency keys are shared across instances.

Suggested production improvements

- Move cache to a distributed store with appropriate TTL and eviction strategy.
- Consider storing a small response fingerprint rather than full body if responses are large.

Screenshots

- See `screenshots/HttpPost1.png` for a successful create and `screens/HttpPost1.png` (repo `screenshots/`) for a sample run.

Diagram

![Idempotency detailed flow](docs/diagrams/idempotency-detailed.png)
