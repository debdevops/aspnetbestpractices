# Rate Limiting

The project configures rate limiting policies and annotates controllers. There are separate policies for public endpoints, health checks, and expensive operations.

How to test

- Make repeated requests quickly; when the rate limit is hit you will receive `429 Too Many Requests` and likely a `Retry-After` header.

Example (PowerShell):

```powershell
for ($i=0; $i -lt 20; $i++) { Invoke-RestMethod -Method Get -Uri 'http://localhost:5232/api/v1/todos' -ErrorAction SilentlyContinue }
```

Notes

- For local testing the default policies are permissive; review `RateLimitingExtensions` for specific thresholds.

Diagram

![Rate limiting flow](docs/diagrams/rate-limiting-flow.png)
- In production ensure limits are sized to match expected traffic and use distributed counters where needed.

