# API Versioning

This project uses `Asp.Versioning` and configures API versioning for controllers.

Key points

- Controller routes contain `v{version:apiVersion}` placeholder.
- Swagger shows available API versions.

How to call a specific version

- Example: `GET /api/v1/todos`

Notes

- When adding new versions create new controller namespace (e.g. `Controllers.v2`) and mark with `[ApiVersion("2.0")]`.
- Keep backwards compatible contracts where possible; otherwise use a major version bump.

Sample swagger link

- `http://localhost:5232/swagger` shows available versions and schemas.
