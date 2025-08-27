# Idempotency (Tutorial)

This project implements idempotency for POST requests via `IdempotencyMiddleware`.

What it does
- Applies only to `POST` requests.
- Requires an `Idempotency-Key` header (GUID, <= 128 chars) to activate.
- Hashes the request body and caches successful 2xx responses for 12 hours.
- Returns cached response with header `Idempotency-Cache: hit` when available.

Why use it
- Protects downstream systems from duplicate side-effects.
- Allows safe retries for clients.

How to test locally (PowerShell)

1) Run the API (you already do this):
```powershell
cd C:\Users\debas\source\repos\aspnetbestpractices\src\Api
dotnet run --launch-profile http
```

2) Create an idempotent POST (first request will be a miss):
```powershell
$body = @{ title = 'idem test'; notes = 'first' } | ConvertTo-Json
$headers = @{ 'Idempotency-Key' = [guid]::NewGuid().ToString() }
Invoke-RestMethod -Method Post -Uri 'http://localhost:5232/api/v1/todos' -ContentType 'application/json' -Body $body -Headers $headers -Verbose
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
