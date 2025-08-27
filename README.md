# ASP.NET Core Best Practices API

[![.NET 8](https://img.shields.io/badge/.NET-8-blue.svg)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-8.0-blueviolet.svg)](https://docs.microsoft.com/en-us/aspnet/core/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

Production-ready template showcasing **secure, observable, resilient REST APIs** built with ASP.NET Core 8, implementing enterprise-grade patterns and best practices.

## Key Features

### Security & Authentication
- **Dynamic Authentication**: Runtime selection between `ApiKey`, `JWT`, `Azure AD`, or `NoAuth` (development)
- **Comprehensive Security Headers**: CSP, HSTS, XSS protection, frame options, and CORS policies
- **Development-Friendly**: Automatic auth bypass for local development environments
- **Feature Toggles**: Runtime authentication control via `Features:EnableAuth` configuration

### Resilience & Performance
- **Smart Idempotency**: All HTTP methods with SHA-256 body hashing and 12-hour caching
- **Polly Integration**: Jittered retry policies, per-request timeouts, and circuit breakers
- **Rate Limiting**: Tiered policies (public: 100/min, expensive: 10/min, health: 30/min)
- **ETag Support**: Optimistic concurrency with `If-Match`/`If-None-Match` headers
- **Compression Pipeline**: Brotli/Gzip response compression + transparent request decompression

### Observability & Monitoring
- **OpenTelemetry**: Complete tracing, metrics, and structured logging to Azure Monitor
- **Health Endpoints**: Separate liveness (`/health/live`) and readiness (`/health/ready`) checks
- **Header Propagation**: Automatic correlation ID and trace context forwarding
- **Structured Logging**: JSON console formatting with UTC timestamps and scopes

### API Standards
- **API Versioning**: URL segment-based versioning (`/api/v1.0/...`)
- **JSON-First**: Enforced content type validation with RFC 7807 ProblemDetails
- **Swagger Integration**: Development-only documentation with CSP-compliant inline styles
- **FluentValidation**: Comprehensive request validation with detailed error responses

---

## Quick Start

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later
- (Optional) [Azure Application Insights](https://docs.microsoft.com/azure/azure-monitor/app/) for observability

### Local Development
```bash
# Clone and navigate to repository
git clone https://github.com/debdevops/aspnetbestpractices.git
cd aspnetbestpractices

# Restore dependencies and build
dotnet restore
dotnet build AspNetBestPractice.sln

# Run with local development profile (auth disabled)
dotnet run --project src/Api --launch-profile Local

# Or run with authentication enabled
ASPNETCORE_ENVIRONMENT=Development dotnet run --project src/Api
```

### Development URLs
- **API**: `https://localhost:5001` (HTTPS) / `http://localhost:5000` (HTTP)
- **Swagger UI**: `https://localhost:5001/swagger` (Development/Local only)
- **Health Checks**:
  - Liveness: `/health/live` (simple 200 OK)
  - Readiness: `/health/ready` (detailed JSON status)

---

## Architecture

```mermaid
graph TB
    subgraph "Client Layer"
        Client[HTTP Clients]
        Browser[Web Browser]
    end
    
    subgraph "API Gateway / Load Balancer"
        LB[Load Balancer<br/>Proxy/Gateway]
    end
    
    Client --> LB
    Browser --> LB
    
    subgraph "ASP.NET Core API"
        LB --> MW[Middleware Pipeline]
        MW --> Auth[Authentication<br/>Authorization]
        Auth --> Controllers[API Controllers<br/>v1.0]
        Controllers --> Services[Business Services]
        Services --> Repo[Repository Layer]
    end
    
    subgraph "Data & External Services"
        Repo --> Memory[(In-Memory Store)]
        Repo --> DB[(Database)]
        Services --> HTTP[HttpClient + Polly]
        HTTP --> External[External APIs]
    end
    
    subgraph "Observability Stack"
        MW --> Logs[Structured Logging]
        MW --> Metrics[Metrics Collection]
        MW --> Traces[Distributed Tracing]
        Logs --> Monitor[Azure Monitor /<br/>Application Insights]
        Metrics --> Monitor
        Traces --> Monitor
    end
```

### Middleware Pipeline (Execution Order)

```mermaid
flowchart TD
    Request[Incoming Request] --> Forward[ForwardedHeaders]
    Forward --> Security[SecurityHeaders + HSTS]
    Security --> HTTPS[HTTPS Redirection]
    HTTPS --> RateLimit[Rate Limiting]
    RateLimit --> Logging[HTTP Logging]
    Logging --> Decompress[Request Decompression]
    Decompress --> JsonOnly[JSON-Only Validation]
    JsonOnly --> Idempotency[Idempotency Check]
    Idempotency --> Exception[Exception Handling]
    Exception --> Compress[Response Compression]
    Compress --> Swagger[Swagger UI<br/>Dev/Local Only]
    Swagger --> CORS[CORS Policy]
    CORS --> AuthN[Authentication]
    AuthN --> AuthZ[Authorization]
    AuthZ --> Route[Controller Routing]
    Route --> Response[HTTP Response]
```

---

## Authentication & Authorization

The API implements a dynamic authentication system that selects the appropriate scheme at runtime:

### Configuration Structure
```json
{
  "Authentication": {
    "Provider": "ApiKey",
    "ApiKey": "your-secure-api-key-here",
    "JwtAuthority": "https://your-jwt-issuer.com",
    "JwtAudience": "your-api-audience"
  },
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "your-tenant-id",
    "ClientId": "your-client-id"
  },
  "Features": {
    "EnableAuth": true
  }
}
```

### Supported Authentication Schemes

#### 1. API Key Authentication
```bash
curl -X GET "https://localhost:5001/api/v1/todos" \
  -H "X-API-Key: your-secure-api-key-here" \
  -H "Content-Type: application/json"
```

#### 2. JWT Bearer Token
```bash
curl -X GET "https://localhost:5001/api/v1/todos" \
  -H "Authorization: Bearer eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9..." \
  -H "Content-Type: application/json"
```

#### 3. Azure AD Integration
Supports Microsoft Identity platform with standard OIDC/OAuth2 flows.

#### 4. Development Mode (NoAuth)
When `ASPNETCORE_ENVIRONMENT=Local` or `Features:EnableAuth=false`:
```bash
# No authentication required for development
curl -X GET "https://localhost:5001/api/v1/todos"
```

---

## Idempotency Implementation

### How It Works
The `IdempotencyMiddleware` provides comprehensive idempotency support:

- **Scope**: POST requests with `Idempotency-Key` header
- **Key Format**: Must be a valid GUID, maximum 128 characters
- **Body Hashing**: SHA-256 hash of JSON request body (max 512KB processed)
- **Caching**: Successful responses (2xx) ≤256KB cached for 12 hours
- **Concurrency**: SemaphoreSlim-based locking prevents race conditions
- **Headers**: Response includes `Idempotency-Cache: hit|miss`

### Sequence Flow
```mermaid
sequenceDiagram
    participant C as Client
    participant M as IdempotencyMiddleware
    participant Cache as MemoryCache
    participant API as Controller

    C->>M: POST /todos + Idempotency-Key
    M->>M: Validate GUID format
    M->>M: Hash request body (SHA-256)
    M->>Cache: Check cache with key+path+hash
    
    alt Cache Hit
        Cache-->>M: Return cached response
        M-->>C: Cached response (Idempotency-Cache: hit)
    else Cache Miss
        M->>M: Acquire semaphore lock
        M->>API: Forward request
        API-->>M: Process and return response
        alt Success Response (2xx)
            M->>Cache: Store response (if ≤256KB)
        end
        M-->>C: Fresh response (Idempotency-Cache: miss)
        M->>M: Release semaphore lock
    end
```

### Usage Example
```powershell
# PowerShell example with proper headers
$headers = @{
    'Content-Type' = 'application/json'
    'X-API-Key' = 'your-secure-api-key-here'
    'Idempotency-Key' = [System.Guid]::NewGuid().ToString()
}

$body = @{
    Title = 'Idempotent Task Creation'
    Notes = 'This request can be safely retried'
} | ConvertTo-Json

# First request - processes normally
$response1 = Invoke-RestMethod -Uri "https://localhost:5001/api/v1/todos" -Method Post -Headers $headers -Body $body
Write-Host "First request - Status: $($response1.StatusCode), Cache: $($response1.Headers.'Idempotency-Cache')"

# Second request with same key - returns cached result
$response2 = Invoke-RestMethod -Uri "https://localhost:5001/api/v1/todos" -Method Post -Headers $headers -Body $body
Write-Host "Second request - Status: $($response2.StatusCode), Cache: $($response2.Headers.'Idempotency-Cache')"
```

---

## ETag & Conditional Requests

### Optimistic Concurrency Control
```http
# 1. Get resource with ETag
GET /api/v1/todos/550e8400-e29b-41d4-a716-446655440000 HTTP/1.1
Host: localhost:5001
X-API-Key: your-secure-api-key-here

# Response includes ETag
HTTP/1.1 200 OK
ETag: W/"638625748123456789"
Content-Type: application/json

{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "title": "Sample Todo",
  "notes": "Original notes",
  "isComplete": false
}

# 2. Update with If-Match (optimistic concurrency)
PUT /api/v1/todos/550e8400-e29b-41d4-a716-446655440000 HTTP/1.1
Host: localhost:5001
Content-Type: application/json
X-API-Key: your-secure-api-key-here
If-Match: W/"638625748123456789"

{
  "title": "Updated Todo",
  "notes": "Modified notes",
  "isComplete": true
}

# 3. Conditional GET (cache validation)
GET /api/v1/todos/550e8400-e29b-41d4-a716-446655440000 HTTP/1.1
Host: localhost:5001
X-API-Key: your-secure-api-key-here
If-None-Match: W/"638625748123456789"

# Returns 304 Not Modified if unchanged
HTTP/1.1 304 Not Modified
```

### Concurrency Conflict Handling
```bash
# If ETag doesn't match current resource state
curl -X PUT "https://localhost:5001/api/v1/todos/550e8400-e29b-41d4-a716-446655440000" \
  -H "Content-Type: application/json" \
  -H "X-API-Key: your-secure-api-key-here" \
  -H "If-Match: W/\"outdated-etag\"" \
  -d '{"title":"This will fail","isComplete":true}'

# Response: 412 Precondition Failed
{
  "type": "https://httpstatuses.io/412",
  "title": "Precondition Failed",
  "status": 412,
  "detail": "ETag does not match the current resource state."
}
```

---

## Rate Limiting

### Policy Configuration
The API implements three distinct rate limiting policies:

```csharp
// From RateLimitingExtensions.cs
public const string PublicPolicy = "public";        // 100 req/min
public const string ExpensivePolicy = "expensive";  // 10 req/min  
public const string HealthPolicy = "health";        // 30 req/min
```

### Policy Application
- **GET endpoints**: Public policy (100 requests/minute)
- **POST, PUT, DELETE**: Expensive policy (10 requests/minute)
- **Health endpoints**: Health policy (30 requests/minute)

### Rate Limit Response
```http
HTTP/1.1 429 Too Many Requests
Retry-After: 45
Content-Type: application/json

{
  "error": "Rate limit exceeded",
  "retryAfter": 45
}
```

---

## Health Checks

### Endpoints Overview

#### Liveness Probe: `/health/live`
Simple health check that returns 200 OK if the process is running:
```bash
curl -X GET "https://localhost:5001/health/live"
# Response: 200 OK (empty body)
```

#### Readiness Probe: `/health/ready`
Detailed health status with dependency checks:
```bash
curl -X GET "https://localhost:5001/health/ready" | jq
```

```json
{
  "status": "Healthy",
  "totalDuration": "00:00:00.0123456",
  "results": {
    "self": {
      "status": "Healthy",
      "description": "OK"
    }
  }
}
```

### Custom Health Checks
Extend health checks by modifying `HealthCheckExtensions.cs`:
```csharp
services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy("OK"))
    .AddCheck("database", () => {
        // Add database connectivity check
        return HealthCheckResult.Healthy("Database accessible");
    })
    .AddCheck("external_api", async () => {
        // Add external service dependency check
        return HealthCheckResult.Healthy("External API responsive");
    });
```

---

## API Examples

### Complete CRUD Operations

#### List All Todos (Anonymous)
```bash
curl -X GET "https://localhost:5001/api/v1/todos?api-version=1.0"
```

#### Get Specific Todo (Anonymous)
```bash
curl -X GET "https://localhost:5001/api/v1/todos/550e8400-e29b-41d4-a716-446655440000" \
  -H "X-API-Key: your-secure-api-key-here"
```

#### Create New Todo (Authenticated + Idempotent)
```bash
curl -X POST "https://localhost:5001/api/v1/todos?api-version=1.0" \
  -H "Content-Type: application/json" \
  -H "X-API-Key: your-secure-api-key-here" \
  -H "Idempotency-Key: $(uuidgen)" \
  -d '{
    "title": "Complete project documentation",
    "notes": "Add comprehensive API examples and deployment guides"
  }'
```

#### Update Todo (Optimistic Concurrency)
```bash
curl -X PUT "https://localhost:5001/api/v1/todos/550e8400-e29b-41d4-a716-446655440000" \
  -H "Content-Type: application/json" \
  -H "X-API-Key: your-secure-api-key-here" \
  -H "If-Match: W/\"638625748123456789\"" \
  -d '{
    "title": "Documentation completed",
    "notes": "All examples added successfully",
    "isComplete": true
  }'
```

#### Delete Todo (Authenticated)
```bash
curl -X DELETE "https://localhost:5001/api/v1/todos/550e8400-e29b-41d4-a716-446655440000" \
  -H "X-API-Key: your-secure-api-key-here"
```

#### Compressed Request Example
```bash
# Send compressed JSON payload
echo '{"title":"Compressed todo","notes":"Sent with gzip compression"}' | gzip | \
curl -X POST "https://localhost:5001/api/v1/todos?api-version=1.0" \
  -H "Content-Type: application/json" \
  -H "Content-Encoding: gzip" \
  -H "X-API-Key: your-secure-api-key-here" \
  -H "Idempotency-Key: $(uuidgen)" \
  --data-binary @-
```

---

## Configuration

### Environment-Specific Settings

#### appsettings.json (Base Configuration)
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "System.Net.Http.HttpClient": "Warning"
    }
  },
  "Authentication": {
    "Provider": "ApiKey",
    "ApiKey": "development-key-change-in-production"
  },
  "Features": {
    "EnableAuth": true
  },
  "Security": {
    "Csp": {
      "DefaultSrc": "'self'",
      "ScriptSrc": "'self'",
      "StyleSrc": "'self'",
      "ImgSrc": "'self' data:",
      "FontSrc": "'self' data:",
      "ConnectSrc": "'self'",
      "FrameAncestors": "'none'",
      "ObjectSrc": "'none'",
      "BaseUri": "'self'",
      "FormAction": "'self'",
      "EnableReportOnly": false
    }
  },
  "Cors": {
    "AllowedOrigins": ["https://yourdomain.com"]
  },
  "Observability": {
    "ServiceName": "AspNetBestPractice.Api",
    "ServiceVersion": "1.0.0",
    "AzureMonitor": {
      "ConnectionString": ""
    }
  },
  "Downstream": {
    "BaseUrl": "https://jsonplaceholder.typicode.com/"
  },
  "HeaderPropagation": {
    "HeadersToPropagate": [
      "traceparent",
      "tracestate",
      "Authorization",
      "X-Request-ID",
      "X-Correlation-ID"
    ]
  }
}
```

#### appsettings.Development.json
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug"
    }
  },
  "Features": {
    "EnableAuth": true
  }
}
```

#### appsettings.Local.json (No Authentication)
```json
{
  "Features": {
    "EnableAuth": false
  },
  "Security": {
    "Csp": {
      "ScriptSrc": "'self' 'unsafe-inline' 'unsafe-eval'",
      "StyleSrc": "'self' 'unsafe-inline'"
    }
  }
}
```

#### appsettings.Production.json
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning"
    }
  },
  "Authentication": {
    "Provider": "AzureAd"
  },
  "Observability": {
    "AzureMonitor": {
      "ConnectionString": "InstrumentationKey=your-key;IngestionEndpoint=https://your-region.in.applicationinsights.azure.com/"
    }
  }
}
```

---

## Observability & Monitoring

### OpenTelemetry Integration

The application provides comprehensive observability through OpenTelemetry:

#### Traces
- ASP.NET Core request/response flows
- HttpClient outbound calls with correlation
- Custom business operation spans

#### Metrics
- HTTP request duration and status codes
- .NET runtime metrics (GC, threading, etc.)
- Custom business metrics

#### Logs
- Structured JSON logging with correlation IDs
- Request/response logging (selected headers only)
- Application events and errors

### Azure Application Insights Configuration
```json
{
  "Observability": {
    "ServiceName": "AspNetBestPractice.Api",
    "ServiceVersion": "1.0.0",
    "AzureMonitor": {
      "ConnectionString": "InstrumentationKey=your-instrumentation-key;IngestionEndpoint=https://your-region.in.applicationinsights.azure.com/"
    }
  }
}
```

### Log Output Example
```json
{
  "timestamp": "2024-01-15 14:30:22.123",
  "level": "Information",
  "message": "Created Todo {Id}",
  "properties": {
    "Id": "550e8400-e29b-41d4-a716-446655440000",
    "RequestPath": "/api/v1/todos",
    "RequestMethod": "POST",
    "TraceId": "0af7651916cd43dd8448eb211c80319c",
    "SpanId": "b9c7c989f97918e1"
  }
}
```

---

## Security Features

### Security Headers
Automatically applied via `UseSecurityHeaders()` middleware:

```http
# Security headers added to all responses
Content-Security-Policy: default-src 'self'; script-src 'self'; style-src 'self'
X-Content-Type-Options: nosniff
X-Frame-Options: DENY
Referrer-Policy: no-referrer
X-XSS-Protection: 0
Cross-Origin-Resource-Policy: same-site
Cross-Origin-Opener-Policy: same-origin
Cross-Origin-Embedder-Policy: require-corp
Cache-Control: no-store
Pragma: no-cache
```

### Content Security Policy (CSP)
- **Production**: Strict CSP with 'self' origins only
- **Development**: Relaxed CSP allowing inline scripts/styles for Swagger
- **Configurable**: All CSP directives customizable via `appsettings.json`

### Kestrel Hardening
```csharp
// Applied in Program.cs
builder.WebHost.UseKestrel(options => {
    options.AddServerHeader = false;                    // Remove server header
    options.Limits.MaxRequestBodySize = 10 * 1024 * 1024; // 10 MB limit
    options.Limits.KeepAliveTimeout = TimeSpan.FromSeconds(60);
    options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(15);
    options.Limits.MaxRequestLineSize = 8 * 1024;       // 8 KB
    options.Limits.MaxRequestHeadersTotalSize = 32 * 1024; // 32 KB
});
```

---

## Project Structure

```
src/
├── Api/
│   ├── Controllers/
│   │   └── v1/
│   │       └── TodosController.cs      # API endpoints with versioning
│   ├── Extensions/
│   │   ├── ApiVersioningExtensions.cs  # API versioning configuration
│   │   ├── HealthCheckExtensions.cs    # Health check setup
│   │   ├── OpenTelemetryExtensions.cs  # Observability configuration
│   │   ├── ProblemDetailsExtensions.cs # RFC 7807 error handling
│   │   ├── RateLimitingExtensions.cs   # Rate limiting policies
│   │   ├── SecurityHeaderExtensions.cs # Security headers middleware
│   │   └── SwaggerExtensions.cs        # API documentation
│   ├── Middleware/
│   │   ├── ExceptionHandlingMiddleware.cs # Global exception handling
│   │   ├── IdempotencyMiddleware.cs    # Request idempotency
│   │   └── JsonOnlyMiddleware.cs       # Content-type validation
│   ├── Security/
│   │   ├── ApiKeyAuthenticationHandler.cs # API key authentication
│   │   ├── AuthenticationOptions.cs    # Authentication configuration
│   │   ├── CspPolicies.cs             # Content Security Policy
│   │   └── NoAuthHandler.cs           # Development authentication bypass
│   ├── Services/
│   │   ├── DownstreamClient.cs        # External API client
│   │   ├── HeaderPropagationOptions.cs # Header forwarding config
│   │   └── HttpContextHeadersDelegatingHandler.cs # HTTP context integration
│   ├── Dtos/                          # Request/Response DTOs
│   ├── Models/                        # Domain models
│   ├── Repositories/                  # Data access layer
│   └── Program.cs                     # Application startup
├── Application/                       # Business logic (if using Clean Architecture)
├── Domain/                           # Domain entities (if using Clean Architecture)
└── Infrastructure/                   # External integrations (if using Clean Architecture)

tests/
├── Api.Tests/                        # Integration tests
├── Application.Tests/                # Unit tests
└── Infrastructure.Tests/             # Infrastructure tests
```

---

## Development Workflow

### Local Development
1. **Clone Repository**: `git clone https://github.com/debdevops/aspnetbestpractices.git`
2. **Restore Packages**: `dotnet restore`
3. **Run Locally**: `dotnet run --project src/Api --launch-profile Local`
4. **Access Swagger**: Navigate to `https://localhost:5001/swagger`

### Testing Authentication
```bash
# Test with no authentication (Local profile)
curl -X GET "https://localhost:5001/api/v1/todos"

# Test with API key
curl -X GET "https://localhost:5001/api/v1/todos" \
  -H "X-API-Key: development-key-change-in-production"

# Test rate limiting
for i in {1..12}; do
  curl -X POST "https://localhost:5001/api/v1/todos" \
    -H "Content-Type: application/json" \
    -H "X-API-Key: development-key-change-in-production" \
    -d '{"title":"Rate limit test '$i'"}' \
    -w "Response: %{http_code}\n"
done
```

### Production Deployment Checklist

#### Security
- [ ] Replace development API keys with secure production keys
- [ ] Configure proper CORS origins
- [ ] Enable HSTS in production environment
- [ ] Review and tighten CSP policies
- [ ] Store secrets in Azure Key Vault or secure environment variables

#### Performance
- [ ] Replace `IMemoryCache` with `IDistributedCache` (Redis) for multi-instance deployments
- [ ] Configure connection pooling for database connections
- [ ] Adjust rate limiting thresholds based on expected traffic
- [ ] Enable response caching for appropriate endpoints

#### Observability
- [ ] Configure Azure Application Insights connection string
- [ ] Set appropriate log levels (Warning+ for production)
- [ ] Configure OpenTelemetry sampling rates to control costs
- [ ] Set up health check monitoring and alerting
- [ ] Configure dashboards for key metrics

#### Infrastructure
- [ ] Disable Swagger in production (`ASPNETCORE_ENVIRONMENT=Production`)
- [ ] Configure proper forwarded headers for load balancers
- [ ] Set up container health checks for orchestrators
- [ ] Configure graceful shutdown handling

---

## Contributing

1. **Fork** the repository
2. **Create** a feature branch (`git checkout -b feature/amazing-improvement`)
3. **Commit** changes (`git commit -m 'Add amazing improvement'`)
4. **Add tests** for new functionality
5. **Push** to branch (`git push origin feature/amazing-improvement`)
6. **Open** a Pull Request

### Development Guidelines
- Follow existing code style and patterns
- Add unit tests for business logic
- Add integration tests for API endpoints
- Update documentation for API changes
- Ensure all tests pass before submitting PR
- Update this README if adding new features

---

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

## Resources & References

- [ASP.NET Core Documentation](https://docs.microsoft.com/en-us/aspnet/core/)
- [OpenTelemetry for .NET](https://opentelemetry.io/docs/instrumentation/net/)
- [Polly Resilience Framework](https://github.com/App-vNext/Polly)
- [Azure Application Insights](https://docs.microsoft.com/en-us/azure/azure-monitor/app/)
- [RFC 7807 - Problem Details](https://tools.ietf.org/html/rfc7807)
- [Content Security Policy](https://developer.mozilla.org/en-US/docs/Web/HTTP/CSP)

For questions, issues, or contributions, please [open an issue](https://github.com/debdevops/aspnetbestpractices/issues) on GitHub.