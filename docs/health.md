# Health Checks

This project exposes two health endpoints:

- `/health/live` - liveness probe (lightweight)
- `/health/ready` - readiness probe (includes configured health checks and a JSON response)

How to check

```powershell
Invoke-RestMethod -Method Get -Uri 'http://localhost:5232/health/live'
Invoke-RestMethod -Method Get -Uri 'http://localhost:5232/health/ready'
```

Notes

- `ready` includes a JSON payload of individual checks and their statuses. Use this in orchestration systems (Kubernetes) for readiness checks.

Screenshots

- Use `screenshots/HttpGet1.png` and `screenshots/HttpGet2.png` where appropriate.
