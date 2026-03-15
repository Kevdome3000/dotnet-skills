---
name: dotnet-aspire
version: "1.0.0"
category: "Cloud"
description: "Use .NET Aspire to orchestrate distributed .NET applications locally with service discovery, telemetry, dashboards, and cloud-ready composition for cloud-native development."
compatibility: "Requires .NET 8+ and Aspire workload installed."
---

# .NET Aspire

## Trigger On

- orchestrating multiple .NET services locally
- setting up service discovery between microservices
- configuring telemetry, health checks, and dashboards
- building cloud-native .NET applications
- managing dependencies like Redis, PostgreSQL, RabbitMQ in development

## Documentation

- [.NET Aspire Overview](https://learn.microsoft.com/en-us/dotnet/aspire/get-started/aspire-overview)
- [Service Discovery](https://learn.microsoft.com/en-us/dotnet/aspire/service-discovery/overview)
- [Aspire Components](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/components-overview)
- [Aspire Dashboard](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/dashboard/overview)
- [Deployment](https://learn.microsoft.com/en-us/dotnet/aspire/deployment/overview)

### References

- [patterns.md](references/patterns.md) - Detailed AppHost patterns, service discovery, integrations, health checks, resilience, and testing patterns
- [deployment.md](references/deployment.md) - Azure Container Apps deployment, manifest generation, CI/CD integration, and production configuration

## Core Concepts

### Three Pillars of Aspire

1. **Orchestration** — Define your app's architecture in code
2. **Integrations** — Pre-configured connections to services (Redis, SQL, etc.)
3. **Service Discovery** — Automatic resolution of service URLs

### Project Structure

```
MyApp/
├── MyApp.AppHost/           # Orchestration project
│   └── Program.cs           # Defines all services and dependencies
├── MyApp.ServiceDefaults/   # Shared service configuration
│   └── Extensions.cs        # OpenTelemetry, health checks, resilience
├── MyApp.Api/               # Your API project
└── MyApp.Web/               # Your web frontend
```

## Workflow

1. **Start with the AppHost:**
   - Add all services, databases, and dependencies
   - Define relationships and references
   - Configure environment-specific settings

2. **Use ServiceDefaults for cross-cutting concerns:**
   - OpenTelemetry (logging, tracing, metrics)
   - Health checks
   - Resilience policies (Polly)

3. **Use built-in integrations:**
   - Redis, PostgreSQL, SQL Server, RabbitMQ, Azure services
   - Each integration auto-configures connection strings

4. **Run with the dashboard:**
   - `dotnet run --project MyApp.AppHost`
   - View logs, traces, and metrics in one place

## AppHost Patterns

### Basic Service Orchestration
```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Add infrastructure
var redis = builder.AddRedis("cache");
var postgres = builder.AddPostgres("db")
    .AddDatabase("orders");

// Add services with dependencies
var api = builder.AddProject<Projects.MyApp_Api>("api")
    .WithReference(postgres)
    .WithReference(redis);

var web = builder.AddProject<Projects.MyApp_Web>("web")
    .WithReference(api);  // Service discovery automatic

builder.Build().Run();
```

### Service Discovery Usage
```csharp
// In your API client configuration
builder.Services.AddHttpClient<OrdersClient>(client =>
{
    // "api" is resolved automatically via service discovery
    client.BaseAddress = new Uri("https+http://api");
});
```

### Named Endpoints
```csharp
// AppHost
var api = builder.AddProject<Projects.Api>("api")
    .WithHttpEndpoint(port: 5001, name: "public")
    .WithHttpEndpoint(port: 5002, name: "internal");

// Client usage
client.BaseAddress = new Uri("https+http://_internal.api");
```

## ServiceDefaults Pattern

```csharp
public static class Extensions
{
    public static IHostApplicationBuilder AddServiceDefaults(
        this IHostApplicationBuilder builder)
    {
        // OpenTelemetry
        builder.ConfigureOpenTelemetry();

        // Health checks
        builder.AddDefaultHealthChecks();

        // Resilience
        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            http.AddStandardResilienceHandler();
        });

        // Service discovery
        builder.Services.AddServiceDiscovery();

        return builder;
    }
}
```

## Built-in Integrations

| Integration | Package | Usage |
|-------------|---------|-------|
| Redis | `Aspire.StackExchange.Redis` | Caching, pub/sub |
| PostgreSQL | `Aspire.Npgsql` | Relational data |
| SQL Server | `Aspire.Microsoft.Data.SqlClient` | Relational data |
| RabbitMQ | `Aspire.RabbitMQ.Client` | Messaging |
| Azure Storage | `Aspire.Azure.Storage.Blobs` | Blob storage |
| MongoDB | `Aspire.MongoDB.Driver` | Document data |

## Anti-Patterns to Avoid

| Anti-Pattern | Why It's Bad | Better Approach |
|--------------|--------------|-----------------|
| Hardcoded URLs | Breaks service discovery | Use `WithReference()` |
| Manual connection strings | Error-prone, not portable | Use integrations |
| Skipping ServiceDefaults | No telemetry or resilience | Always include |
| One giant AppHost | Hard to maintain | Split by domain |

## Dashboard Features

- **Structured Logs** — All services in one view
- **Distributed Tracing** — Request flow across services
- **Metrics** — CPU, memory, custom metrics
- **Resources** — Service health and endpoints

## Deployment

### Azure Container Apps
```bash
azd init
azd up
```

### Docker Compose Export
```bash
dotnet run --project MyApp.AppHost -- --publisher manifest
```

## Best Practices

1. **Always use ServiceDefaults** — Get telemetry for free
2. **Use integrations over manual config** — Connection strings are managed
3. **Reference services explicitly** — `WithReference()` enables discovery
4. **Keep AppHost simple** — Orchestration only, no business logic
5. **Use environment-specific config** — Dev vs production settings
6. **Health checks everywhere** — Required for orchestration

## Deliver

- working multi-service development environment
- automatic service discovery between services
- centralized telemetry and logging
- cloud-deployment-ready architecture

## Validate

- all services start via AppHost
- service discovery resolves correctly
- dashboard shows traces and logs
- health checks pass for all services
- integrations connect without manual config
