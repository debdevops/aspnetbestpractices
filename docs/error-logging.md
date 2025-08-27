# Error Handling & Logging

This project includes:

- `ExceptionHandlingMiddleware` to centralize error responses.
- ProblemDetails configuration for 400-level responses.
- HTTP logging for selected headers.

How to test

- Trigger a validation error by sending invalid model (e.g., missing required `title`) and observe ProblemDetails response.
- Trigger an unhandled exception (temporary throw in a controller) to confirm 500 is captured and sanitized.

Recommendations

- Ensure sensitive details are not exposed in ProblemDetails in production.
- Configure a structured log sink (Seq/Elastic) and set appropriate log levels for production.
