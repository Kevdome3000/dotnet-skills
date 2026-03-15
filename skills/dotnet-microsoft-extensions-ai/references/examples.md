# Microsoft.Extensions.AI Practical Examples

## RAG (Retrieval-Augmented Generation) Implementation

Build a complete RAG pipeline with embeddings and chat:

```csharp
public class RagService(
    IChatClient chatClient,
    IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
    IVectorStore vectorStore)
{
    public async Task IndexDocumentAsync(string documentId, string content, CancellationToken cancellationToken = default)
    {
        var chunks = ChunkDocument(content);
        var embeddings = await embeddingGenerator.GenerateAsync(chunks, cancellationToken: cancellationToken);

        for (var i = 0; i < chunks.Count; i++)
        {
            await vectorStore.UpsertAsync(new VectorDocument
            {
                Id = $"{documentId}_{i}",
                Content = chunks[i],
                Embedding = embeddings[i].Vector.ToArray()
            }, cancellationToken);
        }
    }

    public async Task<string> QueryAsync(string question, int topK = 3, CancellationToken cancellationToken = default)
    {
        // Generate embedding for the question
        var questionEmbedding = await embeddingGenerator.GenerateAsync([question], cancellationToken: cancellationToken);

        // Retrieve relevant documents
        var relevantDocs = await vectorStore.SearchAsync(
            questionEmbedding[0].Vector.ToArray(),
            topK,
            cancellationToken);

        // Build context from retrieved documents
        var context = string.Join("\n\n", relevantDocs.Select(d => d.Content));

        // Generate response with context
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, """
                You are a helpful assistant. Answer questions based on the provided context.
                If the answer is not in the context, say so clearly.
                """),
            new(ChatRole.User, $"""
                Context:
                {context}

                Question: {question}
                """)
        };

        var response = await chatClient.CompleteAsync(messages, cancellationToken: cancellationToken);
        return response.Message.Text ?? string.Empty;
    }

    private static List<string> ChunkDocument(string content, int chunkSize = 500, int overlap = 50)
    {
        var chunks = new List<string>();
        var words = content.Split(' ');

        for (var i = 0; i < words.Length; i += chunkSize - overlap)
        {
            var chunk = string.Join(' ', words.Skip(i).Take(chunkSize));
            if (!string.IsNullOrWhiteSpace(chunk))
            {
                chunks.Add(chunk);
            }
        }

        return chunks;
    }
}

public record VectorDocument
{
    public required string Id { get; init; }
    public required string Content { get; init; }
    public required float[] Embedding { get; init; }
}

public interface IVectorStore
{
    Task UpsertAsync(VectorDocument document, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<VectorDocument>> SearchAsync(float[] queryEmbedding, int topK, CancellationToken cancellationToken = default);
}
```

## Semantic Caching

Cache responses based on semantic similarity:

```csharp
public class SemanticCachingMiddleware(
    IChatClient innerClient,
    IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
    ISemanticCache cache,
    double similarityThreshold = 0.95) : IChatClient
{
    public ChatClientMetadata Metadata => innerClient.Metadata;

    public async Task<ChatCompletion> CompleteAsync(
        IList<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var lastUserMessage = chatMessages.LastOrDefault(m => m.Role == ChatRole.User)?.Text;
        if (string.IsNullOrEmpty(lastUserMessage))
        {
            return await innerClient.CompleteAsync(chatMessages, options, cancellationToken);
        }

        // Generate embedding for the query
        var queryEmbedding = await embeddingGenerator.GenerateAsync([lastUserMessage], cancellationToken: cancellationToken);

        // Check cache for similar queries
        var cachedResponse = await cache.GetSimilarAsync(
            queryEmbedding[0].Vector.ToArray(),
            similarityThreshold,
            cancellationToken);

        if (cachedResponse is not null)
        {
            return new ChatCompletion(new ChatMessage(ChatRole.Assistant, cachedResponse));
        }

        // Execute request and cache response
        var response = await innerClient.CompleteAsync(chatMessages, options, cancellationToken);

        if (response.Message.Text is not null)
        {
            await cache.SetAsync(
                queryEmbedding[0].Vector.ToArray(),
                lastUserMessage,
                response.Message.Text,
                cancellationToken);
        }

        return response;
    }

    public IAsyncEnumerable<StreamingChatCompletionUpdate> CompleteStreamingAsync(
        IList<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
        => innerClient.CompleteStreamingAsync(chatMessages, options, cancellationToken);

    public void Dispose() => innerClient.Dispose();

    public TService? GetService<TService>(object? key = null) where TService : class
        => innerClient.GetService<TService>(key);
}

public interface ISemanticCache
{
    Task<string?> GetSimilarAsync(float[] embedding, double threshold, CancellationToken cancellationToken = default);
    Task SetAsync(float[] embedding, string query, string response, CancellationToken cancellationToken = default);
}
```

## Content Moderation Pipeline

Add content moderation before and after AI responses:

```csharp
public class ContentModerationMiddleware(
    IChatClient innerClient,
    IContentModerator moderator,
    ILogger<ContentModerationMiddleware> logger) : IChatClient
{
    public ChatClientMetadata Metadata => innerClient.Metadata;

    public async Task<ChatCompletion> CompleteAsync(
        IList<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        // Moderate input
        var lastUserMessage = chatMessages.LastOrDefault(m => m.Role == ChatRole.User);
        if (lastUserMessage?.Text is not null)
        {
            var inputModeration = await moderator.ModerateAsync(lastUserMessage.Text, cancellationToken);
            if (inputModeration.IsBlocked)
            {
                logger.LogWarning("Input blocked by moderation: {Reason}", inputModeration.Reason);
                return new ChatCompletion(new ChatMessage(
                    ChatRole.Assistant,
                    "I'm unable to process that request. Please rephrase your question."));
            }
        }

        // Get response
        var response = await innerClient.CompleteAsync(chatMessages, options, cancellationToken);

        // Moderate output
        if (response.Message.Text is not null)
        {
            var outputModeration = await moderator.ModerateAsync(response.Message.Text, cancellationToken);
            if (outputModeration.IsBlocked)
            {
                logger.LogWarning("Output blocked by moderation: {Reason}", outputModeration.Reason);
                return new ChatCompletion(new ChatMessage(
                    ChatRole.Assistant,
                    "I generated a response that didn't meet content guidelines. Please try a different question."));
            }
        }

        return response;
    }

    public IAsyncEnumerable<StreamingChatCompletionUpdate> CompleteStreamingAsync(
        IList<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
        => innerClient.CompleteStreamingAsync(chatMessages, options, cancellationToken);

    public void Dispose() => innerClient.Dispose();

    public TService? GetService<TService>(object? key = null) where TService : class
        => innerClient.GetService<TService>(key);
}

public interface IContentModerator
{
    Task<ModerationResult> ModerateAsync(string content, CancellationToken cancellationToken = default);
}

public record ModerationResult(bool IsBlocked, string? Reason = null);
```

## Structured Output Extraction

Extract structured data from natural language:

```csharp
public class StructuredOutputService(IChatClient chatClient)
{
    public async Task<T?> ExtractAsync<T>(string text, CancellationToken cancellationToken = default) where T : class
    {
        var schema = GenerateJsonSchema<T>();

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, $"""
                Extract structured information from the provided text.
                Return ONLY valid JSON matching this schema:
                {schema}
                Do not include any explanation or markdown formatting.
                """),
            new(ChatRole.User, text)
        };

        var response = await chatClient.CompleteAsync(messages, cancellationToken: cancellationToken);
        var json = response.Message.Text?.Trim();

        if (string.IsNullOrEmpty(json))
        {
            return null;
        }

        return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }

    private static string GenerateJsonSchema<T>()
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        var properties = typeof(T).GetProperties()
            .ToDictionary(
                p => p.Name,
                p => new { type = GetJsonType(p.PropertyType) });

        return JsonSerializer.Serialize(new
        {
            type = "object",
            properties,
            required = properties.Keys.ToArray()
        }, options);
    }

    private static string GetJsonType(Type type) => type switch
    {
        _ when type == typeof(string) => "string",
        _ when type == typeof(int) || type == typeof(long) => "integer",
        _ when type == typeof(double) || type == typeof(float) || type == typeof(decimal) => "number",
        _ when type == typeof(bool) => "boolean",
        _ when type == typeof(DateTime) || type == typeof(DateTimeOffset) => "string",
        _ => "string"
    };
}

// Usage
public record ContactInfo(string Name, string Email, string Phone, string Company);

public class ContactExtractionExample(StructuredOutputService extractionService)
{
    public async Task<ContactInfo?> ExtractContactAsync(string emailBody, CancellationToken cancellationToken = default)
    {
        return await extractionService.ExtractAsync<ContactInfo>(emailBody, cancellationToken);
    }
}
```

## Multi-Turn Conversation with System Instructions

Build a domain-specific assistant with persistent context:

```csharp
public class DomainAssistant(IChatClient chatClient, string systemPrompt)
{
    private readonly List<ChatMessage> _messages = [new(ChatRole.System, systemPrompt)];

    public async Task<string> ChatAsync(string userMessage, CancellationToken cancellationToken = default)
    {
        _messages.Add(new ChatMessage(ChatRole.User, userMessage));

        var response = await chatClient.CompleteAsync(_messages, cancellationToken: cancellationToken);
        var assistantMessage = response.Message;

        _messages.Add(assistantMessage);

        // Trim history to prevent context overflow
        TrimHistoryIfNeeded();

        return assistantMessage.Text ?? string.Empty;
    }

    public async IAsyncEnumerable<string> ChatStreamingAsync(
        string userMessage,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _messages.Add(new ChatMessage(ChatRole.User, userMessage));

        var fullResponse = new StringBuilder();

        await foreach (var update in chatClient.CompleteStreamingAsync(_messages, cancellationToken: cancellationToken))
        {
            if (update.Text is { Length: > 0 })
            {
                fullResponse.Append(update.Text);
                yield return update.Text;
            }
        }

        _messages.Add(new ChatMessage(ChatRole.Assistant, fullResponse.ToString()));
        TrimHistoryIfNeeded();
    }

    private void TrimHistoryIfNeeded(int maxMessages = 20)
    {
        // Keep system message and trim oldest user/assistant pairs
        while (_messages.Count > maxMessages + 1)
        {
            _messages.RemoveAt(1); // Remove oldest after system message
        }
    }

    public void Reset()
    {
        var systemMessage = _messages[0];
        _messages.Clear();
        _messages.Add(systemMessage);
    }
}

// Factory for creating domain assistants
public class DomainAssistantFactory(IChatClient chatClient)
{
    public DomainAssistant CreateCodeReviewer() => new(chatClient, """
        You are an expert code reviewer. Analyze code for:
        - Bugs and potential issues
        - Performance concerns
        - Security vulnerabilities
        - Best practices and patterns
        - Readability and maintainability

        Be constructive and provide specific suggestions with code examples.
        """);

    public DomainAssistant CreateTechnicalWriter() => new(chatClient, """
        You are a technical documentation writer. Help with:
        - API documentation
        - README files
        - Code comments
        - Architecture documentation
        - User guides

        Write clearly and concisely. Use proper formatting with headers and code blocks.
        """);
}
```

## Batch Processing with Rate Limiting

Process multiple requests with proper rate limiting:

```csharp
public class BatchProcessor(IChatClient chatClient, int maxConcurrency = 5, int delayMs = 100)
{
    private readonly SemaphoreSlim _semaphore = new(maxConcurrency);

    public async Task<IReadOnlyList<BatchResult>> ProcessBatchAsync(
        IEnumerable<string> prompts,
        CancellationToken cancellationToken = default)
    {
        var tasks = prompts.Select(async (prompt, index) =>
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                await Task.Delay(delayMs, cancellationToken);

                var messages = new List<ChatMessage> { new(ChatRole.User, prompt) };
                var response = await chatClient.CompleteAsync(messages, cancellationToken: cancellationToken);

                return new BatchResult(index, prompt, response.Message.Text, null);
            }
            catch (Exception ex)
            {
                return new BatchResult(index, prompt, null, ex.Message);
            }
            finally
            {
                _semaphore.Release();
            }
        });

        var results = await Task.WhenAll(tasks);
        return results.OrderBy(r => r.Index).ToList();
    }
}

public record BatchResult(int Index, string Prompt, string? Response, string? Error)
{
    public bool IsSuccess => Error is null;
}
```

## Minimal API Integration

Expose AI capabilities through a minimal API:

```csharp
public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddSingleton<IChatClient>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            return new AzureOpenAIClient(
                    new Uri(config["AzureOpenAI:Endpoint"]!),
                    new DefaultAzureCredential())
                .AsChatClient(config["AzureOpenAI:DeploymentName"]!);
        });

        var app = builder.Build();

        app.MapPost("/chat", async (ChatRequest request, IChatClient chatClient, CancellationToken cancellationToken) =>
        {
            var messages = new List<ChatMessage> { new(ChatRole.User, request.Message) };
            var response = await chatClient.CompleteAsync(messages, cancellationToken: cancellationToken);
            return new ChatResponse(response.Message.Text ?? string.Empty);
        });

        app.MapPost("/chat/stream", async (ChatRequest request, IChatClient chatClient, HttpContext context, CancellationToken cancellationToken) =>
        {
            context.Response.ContentType = "text/event-stream";
            var messages = new List<ChatMessage> { new(ChatRole.User, request.Message) };

            await foreach (var update in chatClient.CompleteStreamingAsync(messages, cancellationToken: cancellationToken))
            {
                if (update.Text is { Length: > 0 })
                {
                    await context.Response.WriteAsync($"data: {update.Text}\n\n", cancellationToken);
                    await context.Response.Body.FlushAsync(cancellationToken);
                }
            }
        });

        app.Run();
    }
}

public record ChatRequest(string Message);
public record ChatResponse(string Response);
```

## Health Check for AI Services

Implement health checks for AI provider availability:

```csharp
public class ChatClientHealthCheck(IChatClient chatClient) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var messages = new List<ChatMessage>
            {
                new(ChatRole.User, "Respond with 'ok'")
            };

            var options = new ChatOptions { MaxOutputTokens = 10 };
            var response = await chatClient.CompleteAsync(messages, options, cancellationToken);

            if (response.Message.Text?.Contains("ok", StringComparison.OrdinalIgnoreCase) == true)
            {
                return HealthCheckResult.Healthy("AI service is responding");
            }

            return HealthCheckResult.Degraded("AI service responded but with unexpected content");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("AI service is not responding", ex);
        }
    }
}

// Registration
public static class HealthCheckConfiguration
{
    public static IServiceCollection AddAIHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck<ChatClientHealthCheck>("ai-service", tags: ["ai", "critical"]);

        return services;
    }
}
```

## Retry and Circuit Breaker Pattern

Add resilience to AI service calls:

```csharp
public class ResilientChatClient(IChatClient innerClient, ILogger<ResilientChatClient> logger) : IChatClient
{
    private readonly ResiliencePipeline _pipeline = new ResiliencePipelineBuilder()
        .AddRetry(new RetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromSeconds(1),
            BackoffType = DelayBackoffType.Exponential,
            ShouldHandle = new PredicateBuilder().Handle<HttpRequestException>()
        })
        .AddCircuitBreaker(new CircuitBreakerStrategyOptions
        {
            FailureRatio = 0.5,
            SamplingDuration = TimeSpan.FromSeconds(30),
            MinimumThroughput = 10,
            BreakDuration = TimeSpan.FromSeconds(30)
        })
        .AddTimeout(TimeSpan.FromSeconds(60))
        .Build();

    public ChatClientMetadata Metadata => innerClient.Metadata;

    public async Task<ChatCompletion> CompleteAsync(
        IList<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return await _pipeline.ExecuteAsync(async ct =>
        {
            logger.LogDebug("Executing chat completion with resilience pipeline");
            return await innerClient.CompleteAsync(chatMessages, options, ct);
        }, cancellationToken);
    }

    public IAsyncEnumerable<StreamingChatCompletionUpdate> CompleteStreamingAsync(
        IList<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
        => innerClient.CompleteStreamingAsync(chatMessages, options, cancellationToken);

    public void Dispose() => innerClient.Dispose();

    public TService? GetService<TService>(object? key = null) where TService : class
        => innerClient.GetService<TService>(key);
}
```
