# OpenTelemetry (observability)

OpenTelemetry is configured in the project. This provides tracing and metrics to your collector/exporter.

Local checks

- Run the API and observe console logs for traces if an OTLP/console exporter is configured.
- Configure your `appsettings.Development.json` to point to a local collector or use the console exporter for quick verification.

Example config snippet (appsettings):

```json
"OpenTelemetry": {
  "Exporters": {
    "Otlp": {
      "Endpoint": "http://localhost:4317"
    }
  }
}
```

Notes

- For a production-ready setup use a collector (Tempo/Jaeger) and a metrics backend (Prometheus + Grafana).

Diagram

![OpenTelemetry flow](docs/diagrams/opentelemetry-flow.png)

- If you need I can add a minimal docker-compose that runs an otel-collector + Jaeger. (Removed for now; you requested docker-related code be removed.)
