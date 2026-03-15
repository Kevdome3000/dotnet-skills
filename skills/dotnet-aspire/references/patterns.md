# .NET Aspire Patterns Reference

This reference provides detailed patterns for AppHost orchestration, service discovery, and integrations in .NET Aspire applications.

## AppHost Patterns

### Basic Application Structure

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Infrastructure first
var cache = builder.AddRedis("cache");
var db = builder.AddPostgres("postgres").AddDatabase("appdb");
var messaging = builder.AddRabbitMQ("messaging");

// Services with dependencies
var catalogApi = builder.AddProject<Projects.Catalog_Api>("catalog-api")
    .WithReference(db)
    .WithReference(cache);

var orderApi = builder.AddProject<Projects.Order_Api>("order-api")
    .WithReference(db)
    .WithReference(messaging)
    .WithReference(catalogApi);

var webApp = builder.AddProject<Projects.Web_App>("webapp")
    .WithReference(catalogApi)
    .WithReference(orderApi)
    .WithExternalHttpEndpoints();

builder.Build().Run();
```

### Resource Configuration Patterns

#### PostgreSQL with Volumes and Parameters

```csharp
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume("postgres-data")
    .WithPgAdmin();

var catalogDb = postgres.AddDatabase("catalog");
var orderDb = postgres.AddDatabase("orders");
```

#### Redis with Persistence

```csharp
var redis = builder.AddRedis("cache")
    .WithDataVolume("redis-data")
    .WithPersistence(TimeSpan.FromMinutes(5), 100);
```

#### SQL Server with Custom Configuration

```csharp
var sqlServer = builder.AddSqlServer("sql")
    .WithDataVolume("sql-data")
    .AddDatabase("appdb");
```

#### RabbitMQ with Management UI

```csharp
var rabbitmq = builder.AddRabbitMQ("messaging")
    .WithManagementPlugin()
    .WithDataVolume("rabbitmq-data");
```

### Multi-Project Orchestration

#### Domain-Separated Services

```csharp
// Shared infrastructure
var redis = builder.AddRedis("cache");
var postgres = builder.AddPostgres("db");

// Catalog domain
var catalogDb = postgres.AddDatabase("catalog");
var catalogApi = builder.AddProject<Projects.Catalog_Api>("catalog-api")
    .WithReference(catalogDb)
    .WithReference(redis);

// Inventory domain
var inventoryDb = postgres.AddDatabase("inventory");
var inventoryApi = builder.AddProject<Projects.Inventory_Api>("inventory-api")
    .WithReference(inventoryDb)
    .WithReference(catalogApi);

// Orders domain
var ordersDb = postgres.AddDatabase("orders");
var ordersApi = builder.AddProject<Projects.Orders_Api>("orders-api")
    .WithReference(ordersDb)
    .WithReference(inventoryApi)
    .WithReference(catalogApi);
```

#### External Service References

```csharp
// Reference external APIs
var weatherApi = builder.AddConnectionString("weather-api");

var dashboard = builder.AddProject<Projects.Dashboard>("dashboard")
    .WithReference(weatherApi);
```

### Environment and Configuration

#### Environment Variables

```csharp
var api = builder.AddProject<Projects.Api>("api")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithEnvironment("FeatureFlags__NewUI", "true")
    .WithEnvironment(context =>
    {
        context.EnvironmentVariables["CUSTOM_VAR"] = "value";
    });
```

#### Conditional Configuration

```csharp
var isDevelopment = builder.Environment.IsDevelopment();

var api = builder.AddProject<Projects.Api>("api");

if (isDevelopment)
{
    api.WithEnvironment("EnableSwagger", "true");
}
```

#### Secrets and Parameters

```csharp
var apiKey = builder.AddParameter("api-key", secret: true);

var api = builder.AddProject<Projects.Api>("api")
    .WithEnvironment("ApiKey", apiKey);
```

### Container and Executable Resources

#### Custom Container Images

```csharp
var maildev = builder.AddContainer("maildev", "maildev/maildev")
    .WithHttpEndpoint(port: 1080, targetPort: 1080, name: "ui")
    .WithEndpoint(port: 1025, targetPort: 1025, name: "smtp");

var api = builder.AddProject<Projects.Api>("api")
    .WithReference(maildev.GetEndpoint("smtp"));
```

#### Dockerfile Build

```csharp
var customService = builder.AddDockerfile("custom", "../path/to/dockerfile")
    .WithHttpEndpoint(port: 8080);
```

#### External Executables

```csharp
var localTool = builder.AddExecutable("tool", "mytool", ".")
    .WithArgs("--mode", "server");
```

## Service Discovery Patterns

### Basic Service Discovery

```csharp
// AppHost configuration
var api = builder.AddProject<Projects.Api>("api");
var web = builder.AddProject<Projects.Web>("web")
    .WithReference(api);
```

```csharp
// Client service registration
builder.Services.AddHttpClient<IApiClient, ApiClient>(client =>
{
    // "api" resolves via service discovery
    client.BaseAddress = new Uri("https+http://api");
});
```

### Named Endpoints

```csharp
// AppHost with named endpoints
var api = builder.AddProject<Projects.Api>("api")
    .WithHttpEndpoint(port: 5000, name: "public")
    .WithHttpEndpoint(port: 5001, name: "internal");
```

```csharp
// Client targeting specific endpoint
builder.Services.AddHttpClient<IPublicApiClient, PublicApiClient>(client =>
{
    client.BaseAddress = new Uri("https+http://_public.api");
});

builder.Services.AddHttpClient<IInternalApiClient, InternalApiClient>(client =>
{
    client.BaseAddress = new Uri("https+http://_internal.api");
});
```

### Service Discovery with Load Balancing

```csharp
// Register service discovery with selection strategy
builder.Services.AddServiceDiscovery()
    .AddConfigurationServiceEndpointProvider();

builder.Services.ConfigureHttpClientDefaults(http =>
{
    http.AddServiceDiscovery();
    http.AddStandardResilienceHandler();
});
```

### gRPC Service Discovery

```csharp
// AppHost
var grpcService = builder.AddProject<Projects.GrpcService>("grpc-service")
    .WithHttpEndpoint(port: 5001, name: "grpc");
```

```csharp
// Client registration
builder.Services.AddGrpcClient<Greeter.GreeterClient>(options =>
{
    options.Address = new Uri("https://_grpc.grpc-service");
});
```

## Integration Patterns

### Caching with Redis

#### AppHost Configuration

```csharp
var redis = builder.AddRedis("cache")
    .WithRedisCommander();

var api = builder.AddProject<Projects.Api>("api")
    .WithReference(redis);
```

#### Service Configuration

```csharp
// Program.cs
builder.AddRedisClient("cache");

// Or with output caching
builder.AddRedisOutputCache("cache");

// Or with distributed caching
builder.AddRedisDistributedCache("cache");
```

#### Usage Pattern

```csharp
public class CatalogService
{
    private readonly IDistributedCache _cache;
    private readonly IConnectionMultiplexer _redis;

    public CatalogService(
        IDistributedCache cache,
        IConnectionMultiplexer redis)
    {
        _cache = cache;
        _redis = redis;
    }

    public async Task<Product?> GetProductAsync(int id)
    {
        var cacheKey = $"product:{id}";
        var cached = await _cache.GetStringAsync(cacheKey);

        if (cached is not null)
        {
            return JsonSerializer.Deserialize<Product>(cached);
        }

        // Fetch from database and cache
        var product = await FetchFromDatabase(id);
        await _cache.SetStringAsync(cacheKey,
            JsonSerializer.Serialize(product),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
            });

        return product;
    }
}
```

### Database Integrations

#### PostgreSQL with Entity Framework Core

```csharp
// AppHost
var postgres = builder.AddPostgres("postgres")
    .AddDatabase("catalog");

var api = builder.AddProject<Projects.Api>("api")
    .WithReference(postgres);
```

```csharp
// Service Program.cs
builder.AddNpgsqlDbContext<CatalogDbContext>("catalog");
```

#### SQL Server with Entity Framework Core

```csharp
// AppHost
var sql = builder.AddSqlServer("sql")
    .AddDatabase("orders");

var api = builder.AddProject<Projects.Api>("api")
    .WithReference(sql);
```

```csharp
// Service Program.cs
builder.AddSqlServerDbContext<OrdersDbContext>("orders");
```

### Messaging with RabbitMQ

#### AppHost Configuration

```csharp
var rabbitmq = builder.AddRabbitMQ("messaging")
    .WithManagementPlugin();

var producer = builder.AddProject<Projects.Producer>("producer")
    .WithReference(rabbitmq);

var consumer = builder.AddProject<Projects.Consumer>("consumer")
    .WithReference(rabbitmq);
```

#### Service Configuration

```csharp
// Program.cs
builder.AddRabbitMQClient("messaging");
```

#### Publisher Pattern

```csharp
public class OrderPublisher
{
    private readonly IConnection _connection;

    public OrderPublisher(IConnection connection)
    {
        _connection = connection;
    }

    public async Task PublishOrderCreatedAsync(Order order)
    {
        using var channel = await _connection.CreateChannelAsync();

        await channel.ExchangeDeclareAsync("orders", ExchangeType.Topic);

        var body = JsonSerializer.SerializeToUtf8Bytes(order);

        await channel.BasicPublishAsync(
            exchange: "orders",
            routingKey: "order.created",
            body: body);
    }
}
```

#### Consumer Pattern

```csharp
public class OrderConsumer : BackgroundService
{
    private readonly IConnection _connection;

    public OrderConsumer(IConnection connection)
    {
        _connection = connection;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var channel = await _connection.CreateChannelAsync();

        await channel.ExchangeDeclareAsync("orders", ExchangeType.Topic);
        var queueName = await channel.QueueDeclareAsync();
        await channel.QueueBindAsync(queueName, "orders", "order.*");

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (model, ea) =>
        {
            var order = JsonSerializer.Deserialize<Order>(ea.Body.Span);
            // Process order
            await channel.BasicAckAsync(ea.DeliveryTag, false);
        };

        await channel.BasicConsumeAsync(queueName, false, consumer);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}
```

### Azure Service Integrations

#### Azure Storage

```csharp
// AppHost
var storage = builder.AddAzureStorage("storage");
var blobs = storage.AddBlobs("blobs");
var queues = storage.AddQueues("queues");
var tables = storage.AddTables("tables");

var api = builder.AddProject<Projects.Api>("api")
    .WithReference(blobs)
    .WithReference(queues);
```

```csharp
// Service Program.cs
builder.AddAzureBlobClient("blobs");
builder.AddAzureQueueClient("queues");
```

#### Azure Service Bus

```csharp
// AppHost
var serviceBus = builder.AddAzureServiceBus("messaging");

var api = builder.AddProject<Projects.Api>("api")
    .WithReference(serviceBus);
```

```csharp
// Service Program.cs
builder.AddAzureServiceBusClient("messaging");
```

#### Azure Key Vault

```csharp
// AppHost
var keyVault = builder.AddAzureKeyVault("secrets");

var api = builder.AddProject<Projects.Api>("api")
    .WithReference(keyVault);
```

```csharp
// Service Program.cs
builder.AddAzureKeyVaultClient("secrets");
```

## Health Check Patterns

### ServiceDefaults Health Configuration

```csharp
public static IHostApplicationBuilder AddDefaultHealthChecks(
    this IHostApplicationBuilder builder)
{
    builder.Services.AddHealthChecks()
        .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"]);

    return builder;
}
```

### Custom Health Checks

```csharp
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"])
    .AddNpgSql(connectionString, name: "postgres", tags: ["ready"])
    .AddRedis(redisConnectionString, name: "redis", tags: ["ready"])
    .AddRabbitMQ(rabbitConnectionString, name: "rabbitmq", tags: ["ready"]);
```

### Health Check Endpoints

```csharp
app.MapHealthChecks("/health");
app.MapHealthChecks("/alive", new HealthCheckOptions
{
    Predicate = r => r.Tags.Contains("live")
});
app.MapHealthChecks("/ready", new HealthCheckOptions
{
    Predicate = r => r.Tags.Contains("ready")
});
```

## Resilience Patterns

### Standard Resilience Handler

```csharp
builder.Services.ConfigureHttpClientDefaults(http =>
{
    http.AddStandardResilienceHandler();
});
```

### Custom Resilience Policies

```csharp
builder.Services.AddHttpClient<ICatalogClient, CatalogClient>()
    .AddResilienceHandler("catalog", builder =>
    {
        builder
            .AddRetry(new HttpRetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromMilliseconds(500),
                BackoffType = DelayBackoffType.Exponential
            })
            .AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
            {
                SamplingDuration = TimeSpan.FromSeconds(10),
                FailureRatio = 0.5,
                MinimumThroughput = 10,
                BreakDuration = TimeSpan.FromSeconds(30)
            })
            .AddTimeout(TimeSpan.FromSeconds(5));
    });
```

## OpenTelemetry Configuration

### Standard Configuration

```csharp
public static IHostApplicationBuilder ConfigureOpenTelemetry(
    this IHostApplicationBuilder builder)
{
    builder.Logging.AddOpenTelemetry(logging =>
    {
        logging.IncludeFormattedMessage = true;
        logging.IncludeScopes = true;
    });

    builder.Services.AddOpenTelemetry()
        .WithMetrics(metrics =>
        {
            metrics.AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation();
        })
        .WithTracing(tracing =>
        {
            tracing.AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddGrpcClientInstrumentation();
        });

    builder.AddOpenTelemetryExporters();

    return builder;
}
```

### Custom Metrics

```csharp
public class OrderMetrics
{
    private readonly Counter<int> _ordersCreated;
    private readonly Histogram<double> _orderProcessingTime;

    public OrderMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create("Orders");
        _ordersCreated = meter.CreateCounter<int>("orders.created");
        _orderProcessingTime = meter.CreateHistogram<double>("orders.processing_time");
    }

    public void OrderCreated() => _ordersCreated.Add(1);

    public void RecordProcessingTime(double milliseconds) =>
        _orderProcessingTime.Record(milliseconds);
}
```

## Testing Patterns

### Integration Testing with Aspire

```csharp
public class IntegrationTests : IClassFixture<DistributedApplicationFixture>
{
    private readonly DistributedApplication _app;

    public IntegrationTests(DistributedApplicationFixture fixture)
    {
        _app = fixture.App;
    }

    [Fact]
    public async Task ApiReturnsProducts()
    {
        var httpClient = _app.CreateHttpClient("api");

        var response = await httpClient.GetAsync("/products");

        response.EnsureSuccessStatusCode();
        var products = await response.Content.ReadFromJsonAsync<Product[]>();
        Assert.NotEmpty(products);
    }
}
```

### Test Fixture

```csharp
public class DistributedApplicationFixture : IAsyncLifetime
{
    public DistributedApplication App { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.MyApp_AppHost>();

        App = await appHost.BuildAsync();
        await App.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await App.DisposeAsync();
    }
}
```
