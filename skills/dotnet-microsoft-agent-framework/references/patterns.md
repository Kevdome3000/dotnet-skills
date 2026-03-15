# Microsoft Agent Framework Patterns

## Agent Patterns

### Single Agent with Tools

The most common pattern: one agent with access to tools that extend its capabilities.

```csharp
public sealed class WeatherAgent(IChatClient chatClient, IWeatherService weatherService)
{
    private readonly ChatOptions _options = new()
    {
        Tools = [AIFunctionFactory.Create(weatherService.GetCurrentWeather)]
    };

    public async Task<string> GetWeatherInsightAsync(string location, CancellationToken ct = default)
    {
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, "You are a weather assistant. Use available tools to provide weather information."),
            new(ChatRole.User, $"What is the weather like in {location}?")
        };

        var response = await chatClient.GetResponseAsync(messages, _options, ct);
        return response.Text;
    }
}

public interface IWeatherService
{
    [Description("Gets current weather for a location")]
    WeatherResult GetCurrentWeather([Description("City name")] string city);
}
```

### Stateful Conversation Agent

Agent that maintains conversation history across interactions.

```csharp
public sealed class ConversationalAgent(IChatClient chatClient)
{
    private readonly List<ChatMessage> _history = [
        new(ChatRole.System, "You are a helpful assistant.")
    ];

    public async Task<string> ChatAsync(string userMessage, CancellationToken ct = default)
    {
        _history.Add(new ChatMessage(ChatRole.User, userMessage));

        var response = await chatClient.GetResponseAsync(_history, cancellationToken: ct);

        _history.Add(new ChatMessage(ChatRole.Assistant, response.Text));

        return response.Text;
    }

    public void ClearHistory() => _history.RemoveRange(1, _history.Count - 1);
}
```

### Agent with Structured Output

Agent that returns strongly-typed responses.

```csharp
public sealed class AnalysisAgent(IChatClient chatClient)
{
    public async Task<SentimentResult> AnalyzeSentimentAsync(string text, CancellationToken ct = default)
    {
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, "Analyze the sentiment of the provided text."),
            new(ChatRole.User, text)
        };

        return await chatClient.GetResponseAsync<SentimentResult>(messages, cancellationToken: ct);
    }
}

public record SentimentResult(
    string Sentiment,
    double Confidence,
    string[] KeyPhrases);
```

## Multi-Agent Orchestration Patterns

### Sequential Pipeline

Agents process in sequence, each building on the previous output.

```csharp
public sealed class ContentPipeline(
    ResearchAgent researcher,
    WriterAgent writer,
    EditorAgent editor)
{
    public async Task<Article> CreateArticleAsync(string topic, CancellationToken ct = default)
    {
        var research = await researcher.GatherFactsAsync(topic, ct);
        var draft = await writer.WriteDraftAsync(topic, research, ct);
        var final = await editor.RefineAsync(draft, ct);

        return new Article(topic, final, research.Sources);
    }
}

public sealed class ResearchAgent(IChatClient chatClient, ISearchService search)
{
    public async Task<ResearchResult> GatherFactsAsync(string topic, CancellationToken ct = default)
    {
        var searchResults = await search.SearchAsync(topic, ct);

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, "Synthesize the search results into key facts."),
            new(ChatRole.User, $"Topic: {topic}\n\nResults:\n{string.Join("\n", searchResults)}")
        };

        var synthesis = await chatClient.GetResponseAsync(messages, cancellationToken: ct);
        return new ResearchResult(synthesis.Text, searchResults.Select(r => r.Url).ToArray());
    }
}

public sealed class WriterAgent(IChatClient chatClient)
{
    public async Task<string> WriteDraftAsync(string topic, ResearchResult research, CancellationToken ct = default)
    {
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, "Write a well-structured article based on the provided research."),
            new(ChatRole.User, $"Topic: {topic}\n\nResearch:\n{research.Facts}")
        };

        var response = await chatClient.GetResponseAsync(messages, cancellationToken: ct);
        return response.Text;
    }
}

public sealed class EditorAgent(IChatClient chatClient)
{
    public async Task<string> RefineAsync(string draft, CancellationToken ct = default)
    {
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, "Edit and improve the article for clarity, grammar, and flow."),
            new(ChatRole.User, draft)
        };

        var response = await chatClient.GetResponseAsync(messages, cancellationToken: ct);
        return response.Text;
    }
}
```

### Coordinator Pattern

A central coordinator routes tasks to specialized agents.

```csharp
public sealed class SupportCoordinator(
    IChatClient routerClient,
    TechnicalAgent technical,
    BillingAgent billing,
    GeneralAgent general)
{
    public async Task<SupportResponse> HandleRequestAsync(string request, CancellationToken ct = default)
    {
        var category = await ClassifyRequestAsync(request, ct);

        return category switch
        {
            RequestCategory.Technical => await technical.HandleAsync(request, ct),
            RequestCategory.Billing => await billing.HandleAsync(request, ct),
            _ => await general.HandleAsync(request, ct)
        };
    }

    private async Task<RequestCategory> ClassifyRequestAsync(string request, CancellationToken ct)
    {
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, "Classify the support request. Respond with only: Technical, Billing, or General"),
            new(ChatRole.User, request)
        };

        var response = await routerClient.GetResponseAsync(messages, cancellationToken: ct);

        return Enum.TryParse<RequestCategory>(response.Text.Trim(), true, out var result)
            ? result
            : RequestCategory.General;
    }
}

public enum RequestCategory { Technical, Billing, General }
```

### Debate Pattern

Multiple agents with different perspectives reach consensus.

```csharp
public sealed class DebateOrchestrator(
    IChatClient optimistClient,
    IChatClient pessimistClient,
    IChatClient judgeClient)
{
    public async Task<DebateResult> DebateAsync(string topic, int rounds = 3, CancellationToken ct = default)
    {
        var arguments = new List<DebateArgument>();
        string? lastOptimist = null;
        string? lastPessimist = null;

        for (var round = 0; round < rounds; round++)
        {
            var optimistArg = await GetArgumentAsync(
                optimistClient,
                "You argue the optimistic case.",
                topic,
                lastPessimist,
                ct);
            arguments.Add(new DebateArgument("Optimist", round, optimistArg));
            lastOptimist = optimistArg;

            var pessimistArg = await GetArgumentAsync(
                pessimistClient,
                "You argue the pessimistic/critical case.",
                topic,
                lastOptimist,
                ct);
            arguments.Add(new DebateArgument("Pessimist", round, pessimistArg));
            lastPessimist = pessimistArg;
        }

        var verdict = await JudgeDebateAsync(topic, arguments, ct);
        return new DebateResult(topic, arguments, verdict);
    }

    private async Task<string> GetArgumentAsync(
        IChatClient client,
        string role,
        string topic,
        string? counterArgument,
        CancellationToken ct)
    {
        var prompt = counterArgument is null
            ? $"Topic: {topic}"
            : $"Topic: {topic}\n\nRespond to this argument:\n{counterArgument}";

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, role),
            new(ChatRole.User, prompt)
        };

        var response = await client.GetResponseAsync(messages, cancellationToken: ct);
        return response.Text;
    }

    private async Task<string> JudgeDebateAsync(
        string topic,
        List<DebateArgument> arguments,
        CancellationToken ct)
    {
        var transcript = string.Join("\n\n", arguments.Select(a =>
            $"[{a.Side} - Round {a.Round + 1}]\n{a.Content}"));

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, "You are an impartial judge. Synthesize a balanced conclusion."),
            new(ChatRole.User, $"Topic: {topic}\n\nDebate transcript:\n{transcript}")
        };

        var response = await judgeClient.GetResponseAsync(messages, cancellationToken: ct);
        return response.Text;
    }
}

public record DebateArgument(string Side, int Round, string Content);
public record DebateResult(string Topic, List<DebateArgument> Arguments, string Verdict);
```

### Human-in-the-Loop Pattern

Agent pauses for human approval on critical decisions.

```csharp
public sealed class ApprovalAgent(IChatClient chatClient, IApprovalService approvalService)
{
    public async Task<ExecutionResult> ExecuteWithApprovalAsync(
        string task,
        CancellationToken ct = default)
    {
        var plan = await GeneratePlanAsync(task, ct);

        if (plan.RequiresApproval)
        {
            var approval = await approvalService.RequestApprovalAsync(
                plan.Description,
                plan.EstimatedImpact,
                ct);

            if (!approval.Approved)
            {
                return new ExecutionResult(false, "Rejected by human reviewer", approval.Reason);
            }
        }

        return await ExecutePlanAsync(plan, ct);
    }

    private async Task<ExecutionPlan> GeneratePlanAsync(string task, CancellationToken ct)
    {
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, """
                Generate an execution plan. Include:
                - Description of actions
                - Whether approval is required (true for destructive/expensive operations)
                - Estimated impact
                """),
            new(ChatRole.User, task)
        };

        return await chatClient.GetResponseAsync<ExecutionPlan>(messages, cancellationToken: ct);
    }

    private Task<ExecutionResult> ExecutePlanAsync(ExecutionPlan plan, CancellationToken ct)
    {
        // Execute the approved plan
        return Task.FromResult(new ExecutionResult(true, plan.Description, null));
    }
}

public record ExecutionPlan(string Description, bool RequiresApproval, string EstimatedImpact);
public record ExecutionResult(bool Success, string Description, string? Reason);

public interface IApprovalService
{
    Task<ApprovalResponse> RequestApprovalAsync(string description, string impact, CancellationToken ct);
}

public record ApprovalResponse(bool Approved, string? Reason);
```

## Provider Patterns

### Provider Abstraction with DI

Configure providers through dependency injection for testability.

```csharp
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAgentServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register the chat client with the configured provider
        services.AddSingleton<IChatClient>(sp =>
        {
            var provider = configuration["AI:Provider"];

            return provider switch
            {
                "AzureOpenAI" => new AzureOpenAIClient(
                    new Uri(configuration["AI:AzureOpenAI:Endpoint"]!),
                    new DefaultAzureCredential())
                    .GetChatClient(configuration["AI:AzureOpenAI:Deployment"]!),

                "OpenAI" => new OpenAIClient(configuration["AI:OpenAI:ApiKey"]!)
                    .GetChatClient(configuration["AI:OpenAI:Model"]!),

                _ => throw new InvalidOperationException($"Unknown provider: {provider}")
            };
        });

        // Register agents
        services.AddScoped<ConversationalAgent>();
        services.AddScoped<AnalysisAgent>();

        return services;
    }
}
```

### Middleware Pipeline Pattern

Wrap chat clients with cross-cutting concerns.

```csharp
public sealed class LoggingChatClient(IChatClient inner, ILogger<LoggingChatClient> logger) : IChatClient
{
    public ChatClientMetadata Metadata => inner.Metadata;

    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken ct = default)
    {
        var messageList = messages.ToList();
        logger.LogInformation("Sending {Count} messages to chat client", messageList.Count);

        var stopwatch = Stopwatch.StartNew();
        var response = await inner.GetResponseAsync(messageList, options, ct);
        stopwatch.Stop();

        logger.LogInformation(
            "Received response in {Elapsed}ms. Tokens: {Input}/{Output}",
            stopwatch.ElapsedMilliseconds,
            response.Usage?.InputTokenCount,
            response.Usage?.OutputTokenCount);

        return response;
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken ct = default) =>
        inner.GetStreamingResponseAsync(messages, options, ct);

    public TService? GetService<TService>(object? key = null) where TService : class =>
        inner.GetService<TService>(key);

    public void Dispose() => inner.Dispose();
}

public sealed class RetryChatClient(IChatClient inner, int maxRetries = 3) : IChatClient
{
    public ChatClientMetadata Metadata => inner.Metadata;

    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken ct = default)
    {
        var messageList = messages.ToList();

        for (var attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                return await inner.GetResponseAsync(messageList, options, ct);
            }
            catch (Exception) when (attempt < maxRetries - 1)
            {
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)), ct);
            }
        }

        return await inner.GetResponseAsync(messageList, options, ct);
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken ct = default) =>
        inner.GetStreamingResponseAsync(messages, options, ct);

    public TService? GetService<TService>(object? key = null) where TService : class =>
        inner.GetService<TService>(key);

    public void Dispose() => inner.Dispose();
}

// Compose middleware
public static IChatClient CreatePipelinedClient(IChatClient baseClient, ILoggerFactory loggerFactory)
{
    return new LoggingChatClient(
        new RetryChatClient(baseClient),
        loggerFactory.CreateLogger<LoggingChatClient>());
}
```

### Multi-Provider Failover

Automatically fail over between providers.

```csharp
public sealed class FailoverChatClient(IChatClient[] clients, ILogger<FailoverChatClient> logger) : IChatClient
{
    public ChatClientMetadata Metadata => clients[0].Metadata;

    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken ct = default)
    {
        var messageList = messages.ToList();
        var lastException = default(Exception);

        for (var i = 0; i < clients.Length; i++)
        {
            try
            {
                return await clients[i].GetResponseAsync(messageList, options, ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Provider {Index} failed, trying next", i);
                lastException = ex;
            }
        }

        throw new AggregateException("All providers failed", lastException!);
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken ct = default) =>
        clients[0].GetStreamingResponseAsync(messages, options, ct);

    public TService? GetService<TService>(object? key = null) where TService : class =>
        clients[0].GetService<TService>(key);

    public void Dispose()
    {
        foreach (var client in clients)
        {
            client.Dispose();
        }
    }
}
```

## Telemetry Patterns

### OpenTelemetry Integration

Instrument agents for distributed tracing.

```csharp
public sealed class InstrumentedAgent(
    IChatClient chatClient,
    ActivitySource activitySource,
    IMeterFactory meterFactory)
{
    private readonly Counter<long> _requestCounter = meterFactory
        .Create("Agent.Metrics")
        .CreateCounter<long>("agent.requests");

    private readonly Histogram<double> _latencyHistogram = meterFactory
        .Create("Agent.Metrics")
        .CreateHistogram<double>("agent.latency", "ms");

    public async Task<string> ProcessAsync(string input, CancellationToken ct = default)
    {
        using var activity = activitySource.StartActivity("Agent.Process");
        activity?.SetTag("input.length", input.Length);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var messages = new List<ChatMessage>
            {
                new(ChatRole.User, input)
            };

            var response = await chatClient.GetResponseAsync(messages, cancellationToken: ct);

            activity?.SetTag("output.length", response.Text.Length);
            activity?.SetTag("tokens.input", response.Usage?.InputTokenCount);
            activity?.SetTag("tokens.output", response.Usage?.OutputTokenCount);

            _requestCounter.Add(1, new KeyValuePair<string, object?>("status", "success"));

            return response.Text;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _requestCounter.Add(1, new KeyValuePair<string, object?>("status", "error"));
            throw;
        }
        finally
        {
            _latencyHistogram.Record(stopwatch.Elapsed.TotalMilliseconds);
        }
    }
}

// Registration
public static IServiceCollection AddInstrumentedAgent(this IServiceCollection services)
{
    services.AddSingleton(new ActivitySource("Agent.Tracing"));
    services.AddSingleton<InstrumentedAgent>();
    return services;
}
```

## Tool Registration Patterns

### Declarative Tool Definition

Define tools using attributes for clean separation.

```csharp
public sealed class CalculatorTools
{
    [Description("Adds two numbers together")]
    public static double Add(
        [Description("First number")] double a,
        [Description("Second number")] double b) => a + b;

    [Description("Subtracts the second number from the first")]
    public static double Subtract(
        [Description("First number")] double a,
        [Description("Second number")] double b) => a - b;

    [Description("Multiplies two numbers")]
    public static double Multiply(
        [Description("First number")] double a,
        [Description("Second number")] double b) => a * b;

    [Description("Divides the first number by the second")]
    public static double Divide(
        [Description("Numerator")] double a,
        [Description("Denominator (non-zero)")] double b) =>
        b == 0 ? throw new ArgumentException("Cannot divide by zero") : a / b;
}

// Registration
var options = new ChatOptions
{
    Tools =
    [
        AIFunctionFactory.Create(CalculatorTools.Add),
        AIFunctionFactory.Create(CalculatorTools.Subtract),
        AIFunctionFactory.Create(CalculatorTools.Multiply),
        AIFunctionFactory.Create(CalculatorTools.Divide)
    ]
};
```

### Async Tool with External Services

Tools that integrate with external services.

```csharp
public sealed class DatabaseTools(IDbConnection connection)
{
    [Description("Executes a read-only SQL query and returns results as JSON")]
    public async Task<string> QueryDatabaseAsync(
        [Description("SQL SELECT query to execute")] string query,
        CancellationToken ct = default)
    {
        if (!query.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Only SELECT queries are allowed");
        }

        var results = await connection.QueryAsync(query);
        return JsonSerializer.Serialize(results);
    }
}

public sealed class HttpTools(HttpClient httpClient)
{
    [Description("Fetches content from a URL")]
    public async Task<string> FetchUrlAsync(
        [Description("URL to fetch")] string url,
        CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(ct);
    }
}
```

## Error Handling Patterns

### Graceful Degradation

Handle failures without crashing the entire workflow.

```csharp
public sealed class ResilientAgent(
    IChatClient primaryClient,
    IChatClient? fallbackClient,
    ILogger<ResilientAgent> logger)
{
    public async Task<AgentResponse> ProcessAsync(string input, CancellationToken ct = default)
    {
        try
        {
            var response = await ExecuteWithClientAsync(primaryClient, input, ct);
            return new AgentResponse(response, ResponseSource.Primary, null);
        }
        catch (Exception primaryEx)
        {
            logger.LogWarning(primaryEx, "Primary client failed");

            if (fallbackClient is not null)
            {
                try
                {
                    var response = await ExecuteWithClientAsync(fallbackClient, input, ct);
                    return new AgentResponse(response, ResponseSource.Fallback, primaryEx.Message);
                }
                catch (Exception fallbackEx)
                {
                    logger.LogError(fallbackEx, "Fallback client also failed");
                    return new AgentResponse(
                        "I'm experiencing technical difficulties. Please try again later.",
                        ResponseSource.Default,
                        $"Primary: {primaryEx.Message}, Fallback: {fallbackEx.Message}");
                }
            }

            return new AgentResponse(
                "Service temporarily unavailable.",
                ResponseSource.Default,
                primaryEx.Message);
        }
    }

    private async Task<string> ExecuteWithClientAsync(
        IChatClient client,
        string input,
        CancellationToken ct)
    {
        var messages = new List<ChatMessage> { new(ChatRole.User, input) };
        var response = await client.GetResponseAsync(messages, cancellationToken: ct);
        return response.Text;
    }
}

public record AgentResponse(string Content, ResponseSource Source, string? ErrorContext);
public enum ResponseSource { Primary, Fallback, Default }
```
