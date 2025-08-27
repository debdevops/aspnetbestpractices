# Caching & ETags

This sample uses ETags to implement optimistic concurrency and to enable client caching.

ETag usage

- GET `/api/v1/todos/{id}` returns an `ETag` header.
- PUT `/api/v1/todos/{id}` should include `If-Match: <etag>` header. If the value does not match the current resource ETag, the API returns `412 Precondition Failed`.

How to test

1) Create a todo (POST).
2) GET the resource and note `ETag` response header.
3) Modify and PUT with `If-Match` header set. If the ETag mismatches you will receive 412.

Example (PowerShell)

```powershell
# Create
$body = @{ title = 'cache test'; notes = 'cache' } | ConvertTo-Json
$created = Invoke-RestMethod -Method Post -Uri 'http://localhost:5232/api/v1/todos' -ContentType 'application/json' -Body $body
# Get and extract ETag
$resp = Invoke-WebRequest -Method Get -Uri "http://localhost:5232/api/v1/todos/$($created.Id)"
$etag = $resp.Headers['ETag']
# Update using If-Match
$update = @{ title = 'updated'; notes = 'updated'; isComplete = $false } | ConvertTo-Json
Invoke-RestMethod -Method Put -Uri "http://localhost:5232/api/v1/todos/$($created.Id)" -ContentType 'application/json' -Body $update -Headers @{ 'If-Match' = $etag }
```

Production suggestions

- Use Cache-Control headers and an appropriate CDN or proxy for public caches.
- For distributed deployments, ensure ETag generation is stable across instances (e.g., based on timestamps or version field).
