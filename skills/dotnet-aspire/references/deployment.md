# .NET Aspire Deployment Reference

This reference provides detailed patterns for deploying .NET Aspire applications to Azure and other cloud environments.

## Azure Container Apps Deployment

### Prerequisites

- Azure CLI installed
- Azure Developer CLI (azd) installed
- Azure subscription
- Docker installed (for local builds)

### Quick Start with azd

```bash
# Initialize Azure Developer CLI in your project
azd init

# Deploy to Azure Container Apps
azd up
```

### Understanding azd init

When you run `azd init` in an Aspire project, it:

1. Detects the AppHost project
2. Analyzes resource definitions
3. Creates `azure.yaml` configuration
4. Sets up infrastructure templates in `infra/`

### Generated Infrastructure

```
infra/
├── main.bicep              # Main deployment template
├── main.parameters.json    # Environment parameters
├── resources.bicep         # Resource definitions
└── abbreviations.json      # Naming conventions
```

### Environment Configuration

```bash
# Create a new environment
azd env new dev

# Set environment variables
azd env set AZURE_LOCATION eastus
azd env set AZURE_SUBSCRIPTION_ID <subscription-id>

# Switch environments
azd env select production
```

## Manifest Generation

### Generate Deployment Manifest

```bash
# Generate manifest for deployment tooling
dotnet run --project MyApp.AppHost -- --publisher manifest --output-path ./manifest.json
```

### Manifest Structure

```json
{
  "resources": {
    "cache": {
      "type": "container.v0",
      "connectionString": "{cache.bindings.tcp.host}:{cache.bindings.tcp.port}",
      "image": "redis:latest"
    },
    "api": {
      "type": "project.v0",
      "path": "../Api/Api.csproj",
      "env": {
        "ConnectionStrings__cache": "{cache.connectionString}"
      },
      "bindings": {
        "http": {
          "scheme": "http",
          "protocol": "tcp",
          "transport": "http"
        }
      }
    }
  }
}
```

### Custom Manifest Publishers

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Configure for specific deployment target
if (builder.ExecutionContext.IsPublishMode)
{
    // Production configuration
    builder.AddAzureKeyVault("secrets");
}
else
{
    // Development configuration
    builder.AddConnectionString("secrets");
}
```

## Azure Container Apps Configuration

### Ingress Configuration

```csharp
// External ingress (public endpoint)
var api = builder.AddProject<Projects.Api>("api")
    .WithExternalHttpEndpoints();

// Internal only (no public endpoint)
var worker = builder.AddProject<Projects.Worker>("worker");
```

### Scaling Configuration

```csharp
// Configure scaling rules in Azure
var api = builder.AddProject<Projects.Api>("api")
    .WithEnvironment("AZURE_CONTAINER_APPS_MIN_REPLICAS", "1")
    .WithEnvironment("AZURE_CONTAINER_APPS_MAX_REPLICAS", "10");
```

### Custom Container Configuration

```csharp
var api = builder.AddProject<Projects.Api>("api")
    .WithAnnotation(new ContainerImageAnnotation
    {
        Registry = "myregistry.azurecr.io",
        Image = "api",
        Tag = "latest"
    });
```

## Azure Resource Integration

### Azure Redis Cache

```csharp
var redis = builder.AddAzureRedis("cache");

var api = builder.AddProject<Projects.Api>("api")
    .WithReference(redis);
```

### Azure PostgreSQL

```csharp
var postgres = builder.AddAzurePostgresFlexibleServer("postgres")
    .AddDatabase("appdb");

var api = builder.AddProject<Projects.Api>("api")
    .WithReference(postgres);
```

### Azure SQL Database

```csharp
var sql = builder.AddAzureSqlServer("sql")
    .AddDatabase("appdb");

var api = builder.AddProject<Projects.Api>("api")
    .WithReference(sql);
```

### Azure Service Bus

```csharp
var serviceBus = builder.AddAzureServiceBus("messaging")
    .AddQueue("orders")
    .AddTopic("notifications");

var api = builder.AddProject<Projects.Api>("api")
    .WithReference(serviceBus);
```

### Azure Storage

```csharp
var storage = builder.AddAzureStorage("storage")
    .AddBlobs("blobs")
    .AddQueues("queues")
    .AddTables("tables");

var api = builder.AddProject<Projects.Api>("api")
    .WithReference(storage);
```

### Azure Key Vault

```csharp
var keyVault = builder.AddAzureKeyVault("secrets");

var api = builder.AddProject<Projects.Api>("api")
    .WithReference(keyVault);
```

### Azure Application Insights

```csharp
var insights = builder.AddAzureApplicationInsights("monitoring");

var api = builder.AddProject<Projects.Api>("api")
    .WithReference(insights);
```

## Production vs Development Configuration

### Conditional Resource Selection

```csharp
var builder = DistributedApplication.CreateBuilder(args);

IResourceBuilder<IResourceWithConnectionString> cache;
IResourceBuilder<IResourceWithConnectionString> database;

if (builder.ExecutionContext.IsPublishMode)
{
    // Production: Use Azure managed services
    cache = builder.AddAzureRedis("cache");
    database = builder.AddAzurePostgresFlexibleServer("postgres")
        .AddDatabase("appdb");
}
else
{
    // Development: Use containers
    cache = builder.AddRedis("cache");
    database = builder.AddPostgres("postgres")
        .AddDatabase("appdb");
}

var api = builder.AddProject<Projects.Api>("api")
    .WithReference(cache)
    .WithReference(database);
```

### Environment-Specific Parameters

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var apiKey = builder.AddParameter("api-key", secret: true);

var api = builder.AddProject<Projects.Api>("api")
    .WithEnvironment("ApiKey", apiKey);
```

## Docker Compose Export

### Generate Docker Compose

```bash
# Export to Docker Compose format
dotnet run --project MyApp.AppHost -- --publisher docker-compose --output-path ./docker-compose.yml
```

### Sample Output

```yaml
version: '3.8'
services:
  api:
    build:
      context: .
      dockerfile: Api/Dockerfile
    ports:
      - "5000:8080"
    environment:
      - ConnectionStrings__cache=cache:6379
    depends_on:
      - cache

  cache:
    image: redis:latest
    ports:
      - "6379:6379"
```

## Kubernetes Deployment

### Generate Kubernetes Manifests

```bash
# Generate Kubernetes YAML
dotnet run --project MyApp.AppHost -- --publisher kubernetes --output-path ./k8s/
```

### Sample Kubernetes Resources

```yaml
# deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: api
spec:
  replicas: 3
  selector:
    matchLabels:
      app: api
  template:
    metadata:
      labels:
        app: api
    spec:
      containers:
        - name: api
          image: myregistry/api:latest
          ports:
            - containerPort: 8080
          env:
            - name: ConnectionStrings__cache
              valueFrom:
                secretKeyRef:
                  name: app-secrets
                  key: cache-connection
```

### Helm Chart Generation

```bash
# Generate Helm chart structure
dotnet run --project MyApp.AppHost -- --publisher helm --output-path ./charts/
```

## CI/CD Pipeline Integration

### GitHub Actions Workflow

```yaml
name: Deploy to Azure

on:
  push:
    branches: [main]

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Install azd
        uses: Azure/setup-azd@v1

      - name: Log in to Azure
        uses: azure/login@v2
        with:
          creds: ${{ secrets.AZURE_CREDENTIALS }}

      - name: Deploy with azd
        run: azd up --no-prompt
        env:
          AZURE_ENV_NAME: production
          AZURE_LOCATION: eastus
          AZURE_SUBSCRIPTION_ID: ${{ secrets.AZURE_SUBSCRIPTION_ID }}
```

### Azure DevOps Pipeline

```yaml
trigger:
  - main

pool:
  vmImage: 'ubuntu-latest'

steps:
  - task: AzureCLI@2
    inputs:
      azureSubscription: 'MyAzureSubscription'
      scriptType: 'bash'
      scriptLocation: 'inlineScript'
      inlineScript: |
        # Install azd
        curl -fsSL https://aka.ms/install-azd.sh | bash

        # Deploy
        azd up --no-prompt
    env:
      AZURE_ENV_NAME: production
```

## Monitoring and Observability in Production

### Azure Monitor Integration

```csharp
var insights = builder.AddAzureApplicationInsights("monitoring");

var api = builder.AddProject<Projects.Api>("api")
    .WithReference(insights);
```

### Custom Metrics Export

```csharp
// ServiceDefaults configuration for Azure Monitor
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics.AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation()
            .AddMeter("MyApp.*");
    })
    .WithTracing(tracing =>
    {
        tracing.AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation();
    })
    .UseAzureMonitor();
```

### Health Checks for Container Apps

```csharp
// Liveness probe
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = r => r.Tags.Contains("live")
});

// Readiness probe
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = r => r.Tags.Contains("ready")
});
```

## Secrets Management

### Azure Key Vault Integration

```csharp
var keyVault = builder.AddAzureKeyVault("secrets");

var api = builder.AddProject<Projects.Api>("api")
    .WithReference(keyVault);
```

### Using Secrets in Services

```csharp
// Program.cs
builder.AddAzureKeyVaultClient("secrets");

// Configure to use Key Vault for configuration
builder.Configuration.AddAzureKeyVault(
    new Uri(builder.Configuration["KeyVaultUri"]!),
    new DefaultAzureCredential());
```

### Parameter-based Secrets

```csharp
// AppHost
var dbPassword = builder.AddParameter("db-password", secret: true);

var postgres = builder.AddPostgres("postgres")
    .WithEnvironment("POSTGRES_PASSWORD", dbPassword);
```

## Troubleshooting Deployment

### Common Issues

1. **Container startup failures**
   - Check container logs in Azure Portal
   - Verify health check endpoints
   - Check connection strings

2. **Service discovery not working**
   - Verify ingress configuration
   - Check DNS resolution
   - Verify service references in AppHost

3. **Database connection issues**
   - Check firewall rules
   - Verify managed identity configuration
   - Check connection string format

### Debugging Commands

```bash
# View deployment status
azd show

# View logs
azd logs

# Open Azure Portal to resource group
azd portal

# Restart services
az containerapp revision restart --name api --resource-group rg-myapp
```

### Local Debugging of Production Config

```bash
# Run with production manifest but local containers
dotnet run --project MyApp.AppHost -- --launch-profile production
```

## Cost Optimization

### Right-sizing Resources

```csharp
// Configure minimal resources for non-production
if (!builder.ExecutionContext.IsPublishMode)
{
    var postgres = builder.AddPostgres("postgres");
}
else
{
    // Use appropriate SKU for production
    var postgres = builder.AddAzurePostgresFlexibleServer("postgres")
        .WithParameter("sku", "Standard_B1ms");
}
```

### Auto-scaling Configuration

Configure auto-scaling in Azure Container Apps:

```bash
az containerapp update \
  --name api \
  --resource-group rg-myapp \
  --min-replicas 1 \
  --max-replicas 10 \
  --scale-rule-name http-rule \
  --scale-rule-type http \
  --scale-rule-http-concurrency 100
```

## Multi-Region Deployment

### Traffic Manager Setup

```csharp
// Deploy to multiple regions with traffic distribution
var api = builder.AddProject<Projects.Api>("api")
    .WithExternalHttpEndpoints()
    .WithAnnotation(new DeploymentAnnotation
    {
        Regions = ["eastus", "westeurope"],
        TrafficDistribution = "geographic"
    });
```

### Regional Resource Configuration

```csharp
// Configure region-specific resources
var eastUsStorage = builder.AddAzureStorage("storage-eastus")
    .WithParameter("location", "eastus");

var westEuStorage = builder.AddAzureStorage("storage-westeu")
    .WithParameter("location", "westeurope");
```
