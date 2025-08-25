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
