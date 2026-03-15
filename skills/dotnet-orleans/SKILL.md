---
name: dotnet-orleans
version: "1.0.0"
category: "Distributed"
description: "Build or review distributed .NET applications with Orleans grains, silos, streams, persistence, versioning, and cloud-native hosting patterns."
compatibility: "Requires Orleans 7+ (preferably 8.x for latest features)."
---

# Microsoft Orleans

## Trigger On

- building distributed systems with the actor model
- managing stateful entities at scale (millions of concurrent users)
- real-time applications (games, IoT, collaborative tools)
- replacing manual distributed state management
- scaling beyond single-server architectures

## Documentation

- [Orleans Overview](https://learn.microsoft.com/en-us/dotnet/orleans/overview)
- [Best Practices](https://learn.microsoft.com/en-us/dotnet/orleans/resources/best-practices)
- [Grain Persistence](https://learn.microsoft.com/en-us/dotnet/orleans/grains/grain-persistence)
- [Streams](https://learn.microsoft.com/en-us/dotnet/orleans/streaming/)
- [Deployment](https://learn.microsoft.com/en-us/dotnet/orleans/deployment/)

### References

- [Patterns](references/patterns.md) - Detailed grain patterns, persistence strategies, streaming patterns, coordination patterns, and performance patterns
- [Anti-Patterns](references/anti-patterns.md) - Common Orleans mistakes and how to avoid them

## Core Concepts

### Virtual Actor Model

| Concept | Description |
|---------|-------------|
| **Grain** | A virtual actor with identity, state, and behavior |
| **Silo** | A host process that runs grains |
| **Cluster** | Multiple silos working together |
| **Activation** | A grain instance running in memory |

### Key Benefits

- **Always available** — Grains are virtual, created on demand
- **Location transparent** — Call any grain from anywhere
- **Single-threaded** — No locks needed within a grain
- **Automatic scaling** — Runtime manages placement

## Workflow

1. **Define grain interfaces** with async methods
2. **Implement grain classes** with state and behavior
3. **Configure silo hosting** with persistence and clustering
4. **Call grains from clients** using grain factory
5. **Add persistence** for durable state
6. **Use streams** for pub/sub scenarios

## Grain Patterns

### Basic Grain Interface
```csharp
public interface IPlayerGrain : IGrainWithStringKey
{
    Task<PlayerState> GetState();
    Task UpdateScore(int points);
    Task JoinGame(Guid gameId);
}
```

### Grain Implementation
```csharp
public class PlayerGrain(
    [PersistentState("player", "playerStore")]
    IPersistentState<PlayerState> state) : Grain, IPlayerGrain
{
    public Task<PlayerState> GetState() => Task.FromResult(state.State);

    public async Task UpdateScore(int points)
    {
        state.State.Score += points;
        state.State.LastPlayed = DateTime.UtcNow;
        await state.WriteStateAsync();
    }

    public async Task JoinGame(Guid gameId)
    {
        var game = GrainFactory.GetGrain<IGameGrain>(gameId);
        await game.AddPlayer(this.GetPrimaryKeyString());
    }
}
```

### State Class
```csharp
[GenerateSerializer]
public class PlayerState
{
    [Id(0)] public int Score { get; set; }
    [Id(1)] public DateTime LastPlayed { get; set; }
    [Id(2)] public List<Guid> GameHistory { get; set; } = [];
}
```

## Silo Configuration

### Basic Host Setup
```csharp
var builder = Host.CreateApplicationBuilder(args);

builder.UseOrleans(silo =>
{
    silo.UseLocalhostClustering()  // Dev only
        .AddMemoryGrainStorage("playerStore")  // Dev only
        .ConfigureLogging(logging => logging.AddConsole());
});

var host = builder.Build();
await host.RunAsync();
```

### Production Setup (Azure)
```csharp
builder.UseOrleans(silo =>
{
    silo.UseAzureStorageClustering(options =>
        options.ConfigureTableServiceClient(connectionString))
    .AddAzureTableGrainStorage("playerStore", options =>
        options.ConfigureTableServiceClient(connectionString))
    .Configure<ClusterOptions>(options =>
    {
        options.ClusterId = "prod-cluster";
        options.ServiceId = "MyGame";
    });
});
```

## Calling Grains

### From ASP.NET Core
```csharp
app.MapGet("/player/{id}", async (string id, IGrainFactory grains) =>
{
    var player = grains.GetGrain<IPlayerGrain>(id);
    return await player.GetState();
});

app.MapPost("/player/{id}/score", async (
    string id, int points, IGrainFactory grains) =>
{
    var player = grains.GetGrain<IPlayerGrain>(id);
    await player.UpdateScore(points);
    return Results.Ok();
});
```

### From Client (External)
```csharp
var client = new ClientBuilder()
    .UseAzureStorageClustering(options =>
        options.ConfigureTableServiceClient(connectionString))
    .Build();

await client.Connect();

var player = client.GetGrain<IPlayerGrain>("player123");
await player.UpdateScore(100);
```

## Timers and Reminders

### Timer (Non-Persistent)
```csharp
public override Task OnActivateAsync(CancellationToken ct)
{
    RegisterGrainTimer(
        callback: UpdateStats,
        state: default,
        dueTime: TimeSpan.FromMinutes(1),
        period: TimeSpan.FromMinutes(5));

    return base.OnActivateAsync(ct);
}
```

### Reminder (Persistent)
```csharp
public class PlayerGrain : Grain, IPlayerGrain, IRemindable
{
    public async Task ReceiveReminder(string reminderName, TickStatus status)
    {
        if (reminderName == "daily-reward")
        {
            await GiveReward();
        }
    }

    public async Task SetupDailyReward()
    {
        await this.RegisterOrUpdateReminder(
            "daily-reward",
            dueTime: TimeSpan.FromHours(24),
            period: TimeSpan.FromHours(24));
    }
}
```

## Anti-Patterns to Avoid

| Anti-Pattern | Why It's Bad | Better Approach |
|--------------|--------------|-----------------|
| Blocking calls in grains | Deadlocks, poor throughput | Always use `async/await` |
| Large grain state | Slow serialization | Split into multiple grains |
| Chatty grain communication | High latency | Batch operations |
| Single bottleneck grain | Scalability limit | Use fan-out pattern |
| Ignoring activation overhead | Poor performance | Reuse grains, avoid short-lived |

## Scaling Patterns

### Fan-Out Aggregation
```csharp
public interface IAggregatorGrain : IGrainWithIntegerKey
{
    Task<int> GetTotalScore();
}

public class AggregatorGrain : Grain, IAggregatorGrain
{
    public async Task<int> GetTotalScore()
    {
        // Distribute to intermediate aggregators
        var tasks = Enumerable.Range(0, 10)
            .Select(i => GrainFactory
                .GetGrain<IIntermediateAggregator>(i)
                .GetPartialSum());

        var results = await Task.WhenAll(tasks);
        return results.Sum();
    }
}
```

### Grain Size Guidelines

| Requests/Second | Recommendation |
|-----------------|----------------|
| < 100 | Single grain is fine |
| 100-1000 | Consider partitioning |
| > 1000 | Must split into multiple grains |

## Orleans + Aspire

```csharp
// AppHost
var orleans = builder.AddOrleans("default")
    .WithClustering(redis)
    .WithGrainStorage("Default", redis);

builder.AddProject<Projects.Silo>("silo")
    .WithReference(orleans.AsClient());

builder.AddProject<Projects.Api>("api")
    .WithReference(orleans.AsClient());
```

## Deliver

- properly designed grains with clear responsibilities
- correct persistence and clustering configuration
- scalable patterns for high-throughput scenarios
- integration with ASP.NET Core or Aspire

## Validate

- grains are single-threaded (no locks needed)
- state is persisted correctly
- no blocking calls in async methods
- grain activation/deactivation works correctly
- cluster membership is stable
- performance meets requirements
