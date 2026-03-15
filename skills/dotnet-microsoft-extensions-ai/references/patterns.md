# Microsoft.Extensions.AI Patterns

## IChatClient Patterns

### Basic IChatClient Registration

Register a chat client with dependency injection for provider-agnostic access:

```csharp
public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Register Azure OpenAI as IChatClient
        builder.Services.AddSingleton<IChatClient>(sp =>
            new AzureOpenAIClient(new Uri(builder.Configuration["AzureOpenAI:Endpoint"]!),
                    new DefaultAzureCredential())
                .AsChatClient(builder.Configuration["AzureOpenAI:DeploymentName"]!));

        var app = builder.Build();
        app.Run();
    }
}
```

### IChatClient with Middleware Pipeline

Compose middleware for logging, caching, and telemetry:

```csharp
public static class ChatClientConfiguration
{
    public static IServiceCollection AddChatClientWithMiddleware(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<IChatClient>(sp =>
        {
            var innerClient = new AzureOpenAIClient(
                    new Uri(configuration["AzureOpenAI:Endpoint"]!),
                    new DefaultAzureCredential())
                .AsChatClient(configuration["AzureOpenAI:DeploymentName"]!);

            return new ChatClientBuilder(innerClient)
                .UseDistributedCache(sp.GetRequiredService<IDistributedCache>())
                .UseOpenTelemetry(sp.GetRequiredService<ILoggerFactory>())
                .UseFunctionInvocation()
                .Build();
        });

        return services;
    }
}
```

### Streaming Chat Completions

Handle streaming responses with proper async enumeration:

```csharp
public class StreamingChatService(IChatClient chatClient)
{
    public async IAsyncEnumerable<string> StreamResponseAsync(
        string userMessage,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, userMessage)
        };

        await foreach (var update in chatClient.CompleteStreamingAsync(messages, cancellationToken: cancellationToken))
        {
            if (update.Text is { Length: > 0 })
            {
                yield return update.Text;
            }
        }
    }
}
```

### Conversation History Management

Maintain conversation context across multiple turns:

```csharp
public class ConversationService(IChatClient chatClient)
{
    private readonly List<ChatMessage> _history = [];

    public async Task<string> SendMessageAsync(string userMessage, CancellationToken cancellationToken = default)
    {
        _history.Add(new ChatMessage(ChatRole.User, userMessage));

        var response = await chatClient.CompleteAsync(_history, cancellationToken: cancellationToken);
        var assistantMessage = response.Message;

        _history.Add(assistantMessage);

        return assistantMessage.Text ?? string.Empty;
    }

    public void SetSystemPrompt(string systemPrompt)
    {
        _history.Insert(0, new ChatMessage(ChatRole.System, systemPrompt));
    }

    public void ClearHistory() => _history.Clear();
}
```

## Embedding Patterns

### Basic Embedding Generation

Generate embeddings for semantic search and similarity:

```csharp
public class EmbeddingService(IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator)
{
    public async Task<Embedding<float>> GenerateEmbeddingAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        var result = await embeddingGenerator.GenerateAsync([text], cancellationToken: cancellationToken);
        return result[0];
    }

    public async Task<IList<Embedding<float>>> GenerateBatchEmbeddingsAsync(
        IEnumerable<string> texts,
        CancellationToken cancellationToken = default)
    {
        return await embeddingGenerator.GenerateAsync(texts.ToList(), cancellationToken: cancellationToken);
    }
}
```

### Embedding with Caching

Cache embeddings for frequently accessed content:

```csharp
public static class EmbeddingConfiguration
{
    public static IServiceCollection AddCachedEmbeddingGenerator(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(sp =>
        {
            var innerGenerator = new AzureOpenAIClient(
                    new Uri(configuration["AzureOpenAI:Endpoint"]!),
                    new DefaultAzureCredential())
                .AsEmbeddingGenerator(configuration["AzureOpenAI:EmbeddingModel"]!);

            return new EmbeddingGeneratorBuilder<string, Embedding<float>>(innerGenerator)
                .UseDistributedCache(sp.GetRequiredService<IDistributedCache>())
                .UseOpenTelemetry(sp.GetRequiredService<ILoggerFactory>())
                .Build();
        });

        return services;
    }
}
```

### Semantic Similarity Calculation

Compare embeddings for similarity scoring:

```csharp
public class SimilarityService(IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator)
{
    public async Task<double> CalculateSimilarityAsync(
        string text1,
        string text2,
        CancellationToken cancellationToken = default)
    {
        var embeddings = await embeddingGenerator.GenerateAsync([text1, text2], cancellationToken: cancellationToken);
        return CosineSimilarity(embeddings[0].Vector, embeddings[1].Vector);
    }

    private static double CosineSimilarity(ReadOnlyMemory<float> a, ReadOnlyMemory<float> b)
    {
        var spanA = a.Span;
        var spanB = b.Span;

        double dotProduct = 0, magnitudeA = 0, magnitudeB = 0;

        for (var i = 0; i < spanA.Length; i++)
        {
            dotProduct += spanA[i] * spanB[i];
            magnitudeA += spanA[i] * spanA[i];
            magnitudeB += spanB[i] * spanB[i];
        }

        return dotProduct / (Math.Sqrt(magnitudeA) * Math.Sqrt(magnitudeB));
    }
}
```

## Provider Integration Patterns

### Multi-Provider Configuration

Configure multiple AI providers with fallback:

```csharp
public static class MultiProviderConfiguration
{
    public static IServiceCollection AddMultiProviderChatClients(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Primary provider
        services.AddKeyedSingleton<IChatClient>("azure", (sp, _) =>
            new AzureOpenAIClient(
                    new Uri(configuration["AzureOpenAI:Endpoint"]!),
                    new DefaultAzureCredential())
                .AsChatClient(configuration["AzureOpenAI:DeploymentName"]!));

        // Fallback provider
        services.AddKeyedSingleton<IChatClient>("openai", (sp, _) =>
            new OpenAIClient(configuration["OpenAI:ApiKey"]!)
                .AsChatClient(configuration["OpenAI:Model"]!));

        // Resilient client with fallback
        services.AddSingleton<IChatClient>(sp =>
            new FallbackChatClient(
                sp.GetRequiredKeyedService<IChatClient>("azure"),
                sp.GetRequiredKeyedService<IChatClient>("openai")));

        return services;
    }
}

public class FallbackChatClient(IChatClient primary, IChatClient fallback) : IChatClient
{
    public ChatClientMetadata Metadata => primary.Metadata;

    public async Task<ChatCompletion> CompleteAsync(
        IList<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await primary.CompleteAsync(chatMessages, options, cancellationToken);
        }
        catch (Exception)
        {
            return await fallback.CompleteAsync(chatMessages, options, cancellationToken);
        }
    }

    public IAsyncEnumerable<StreamingChatCompletionUpdate> CompleteStreamingAsync(
        IList<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        // Streaming fallback requires more complex handling
        return primary.CompleteStreamingAsync(chatMessages, options, cancellationToken);
    }

    public void Dispose()
    {
        primary.Dispose();
        fallback.Dispose();
    }

    public TService? GetService<TService>(object? key = null) where TService : class
        => primary.GetService<TService>(key);
}
```

### Custom Provider Implementation

Implement IChatClient for a custom or local model:

```csharp
public class LocalModelChatClient(HttpClient httpClient, string modelEndpoint) : IChatClient
{
    public ChatClientMetadata Metadata { get; } = new("LocalModel", new Uri(modelEndpoint));

    public async Task<ChatCompletion> CompleteAsync(
        IList<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var request = new
        {
            messages = chatMessages.Select(m => new
            {
                role = m.Role.Value,
                content = m.Text
            }),
            max_tokens = options?.MaxOutputTokens ?? 1024,
            temperature = options?.Temperature ?? 0.7f
        };

        var response = await httpClient.PostAsJsonAsync(
            modelEndpoint,
            request,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<LocalModelResponse>(cancellationToken);

        return new ChatCompletion(new ChatMessage(ChatRole.Assistant, result?.Content ?? string.Empty));
    }

    public async IAsyncEnumerable<StreamingChatCompletionUpdate> CompleteStreamingAsync(
        IList<ChatMessage> chatMessages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Non-streaming fallback for simplicity
        var completion = await CompleteAsync(chatMessages, options, cancellationToken);
        yield return new StreamingChatCompletionUpdate
        {
            Text = completion.Message.Text
        };
    }

    public void Dispose() { }

    public TService? GetService<TService>(object? key = null) where TService : class => null;

    private record LocalModelResponse(string Content);
}
```

### Tool/Function Calling Pattern

Register and invoke tools through IChatClient:

```csharp
public static class ToolConfiguration
{
    public static IServiceCollection AddChatClientWithTools(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<IChatClient>(sp =>
        {
            var innerClient = new AzureOpenAIClient(
                    new Uri(configuration["AzureOpenAI:Endpoint"]!),
                    new DefaultAzureCredential())
                .AsChatClient(configuration["AzureOpenAI:DeploymentName"]!);

            return new ChatClientBuilder(innerClient)
                .UseFunctionInvocation()
                .Build();
        });

        return services;
    }
}

public class WeatherService(IChatClient chatClient)
{
    [Description("Gets the current weather for a location")]
    public static string GetWeather(
        [Description("The city name")] string city,
        [Description("The country code")] string countryCode)
    {
        // Simulated weather data
        return $"The weather in {city}, {countryCode} is 72F and sunny.";
    }

    public async Task<string> AskAboutWeatherAsync(string question, CancellationToken cancellationToken = default)
    {
        var options = new ChatOptions
        {
            Tools = [AIFunctionFactory.Create(GetWeather)]
        };

        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, question)
        };

        var response = await chatClient.CompleteAsync(messages, options, cancellationToken);
        return response.Message.Text ?? string.Empty;
    }
}
```

## Testing Patterns

### Mock IChatClient for Unit Tests

Create testable services with mock chat clients:

```csharp
public class MockChatClient(string responseText) : IChatClient
{
    public ChatClientMetadata Metadata { get; } = new("Mock");

    public Task<ChatCompletion> CompleteAsync(
        IList<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ChatCompletion(
            new ChatMessage(ChatRole.Assistant, responseText)));
    }

    public async IAsyncEnumerable<StreamingChatCompletionUpdate> CompleteStreamingAsync(
        IList<ChatMessage> chatMessages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        yield return new StreamingChatCompletionUpdate { Text = responseText };
        await Task.CompletedTask;
    }

    public void Dispose() { }

    public TService? GetService<TService>(object? key = null) where TService : class => null;
}

// Usage in tests
public class ChatServiceTests
{
    [Fact]
    public async Task SendMessage_ReturnsExpectedResponse()
    {
        var mockClient = new MockChatClient("Hello, how can I help you?");
        var service = new ConversationService(mockClient);

        var result = await service.SendMessageAsync("Hi there");

        Assert.Equal("Hello, how can I help you?", result);
    }
}
```

### Integration Test Setup

Configure integration tests with real providers:

```csharp
public class ChatClientIntegrationTests : IClassFixture<ChatClientFixture>
{
    private readonly IChatClient _chatClient;

    public ChatClientIntegrationTests(ChatClientFixture fixture)
    {
        _chatClient = fixture.ChatClient;
    }

    [Fact]
    public async Task CompleteAsync_ReturnsValidResponse()
    {
        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, "Say 'test successful' and nothing else.")
        };

        var response = await _chatClient.CompleteAsync(messages);

        Assert.NotNull(response.Message.Text);
        Assert.Contains("test successful", response.Message.Text, StringComparison.OrdinalIgnoreCase);
    }
}

public class ChatClientFixture : IDisposable
{
    public IChatClient ChatClient { get; }

    public ChatClientFixture()
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets<ChatClientFixture>()
            .AddEnvironmentVariables()
            .Build();

        ChatClient = new AzureOpenAIClient(
                new Uri(configuration["AzureOpenAI:Endpoint"]!),
                new DefaultAzureCredential())
            .AsChatClient(configuration["AzureOpenAI:DeploymentName"]!);
    }

    public void Dispose() => ChatClient.Dispose();
}
```
