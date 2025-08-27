# Content Security Policy (CSP) Tutorial

This project adds a configurable CSP header using `CspOptions` and `UseSecurityHeaders()`.

Key points

- CSP is built from `Security:Csp` configuration and applied as `Content-Security-Policy` (or `-Report-Only`).
- In `Local` environment the code adds `unsafe-inline`/`unsafe-eval` to allow Swagger UI to operate without CSP issues.

Configuration

- Check `appsettings.json` and environment-specific files for `Security:Csp` settings.
- `CspOptions` fields: `DefaultSrc`, `ScriptSrc`, `StyleSrc`, `ImgSrc`, `FontSrc`, `ConnectSrc`, `FrameAncestors`, `ObjectSrc`, `BaseUri`, `FormAction`.

How to test and adjust

1. Run the API locally and open Swagger at `http://localhost:5232/swagger`.

2. If you see CSP violations for inline styles/scripts, either:

   - Keep the `Local` behavior (convenient), or
   - Add a nonce in responses and update Swagger UI to use that nonce (more complex).

Example minimal CSP in appsettings (`appsettings.Production.json`):

```json
"Security": {
  "Csp": {
    "DefaultSrc": "'self'",
    "ScriptSrc": "'self' https://trusted.cdn",
    "StyleSrc": "'self' https://trusted.cdn",
    "ImgSrc": "'self' data:",
    "ConnectSrc": "'self' https://api.example.com"
  }
}
```

Mermaid diagram (CSP flow)

```mermaid
sequenceDiagram
  participant B as Browser
  participant A as API
  B->>A: GET /swagger
  A-->>B: 200 + Content-Security-Policy header
  B->>B: Browser enforces CSP; forbids inline styles unless allowed
```

Screenshots to include

- `screenshots/HttpGet1.png`, `screenshots/HttpGet2.png` (Swagger pages and example requests).

Diagram

![CSP flow](docs/diagrams/csp-flow.png)
