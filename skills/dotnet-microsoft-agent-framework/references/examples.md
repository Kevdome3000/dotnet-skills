# Microsoft Agent Framework Examples

## Customer Support Agent

A complete customer support agent with knowledge base lookup, ticket creation, and escalation capabilities.

```csharp
// Domain models
public record SupportTicket(
    string Id,
    string CustomerId,
    string Subject,
    string Description,
    TicketPriority Priority,
    TicketStatus Status);

public enum TicketPriority { Low, Medium, High, Critical }
public enum TicketStatus { Open, InProgress, Resolved, Escalated }

public record KnowledgeArticle(string Id, string Title, string Content, string[] Tags);

// Services
public interface IKnowledgeBaseService
{
    Task<KnowledgeArticle[]> SearchAsync(string query, CancellationToken ct = default);
}

public interface ITicketService
{
    Task<SupportTicket> CreateTicketAsync(
        string customerId,
        string subject,
        string description,
        TicketPriority priority,
        CancellationToken ct = default);

    Task<SupportTicket> EscalateTicketAsync(string ticketId, string reason, CancellationToken ct = default);
}

// Tool definitions
public sealed class SupportTools(
    IKnowledgeBaseService knowledgeBase,
    ITicketService ticketService)
{
    [Description("Searches the knowledge base for relevant articles")]
    public async Task<string> SearchKnowledgeBaseAsync(
        [Description("Search query")] string query,
        CancellationToken ct = default)
    {
        var articles = await knowledgeBase.SearchAsync(query, ct);

        if (articles.Length == 0)
        {
            return "No relevant articles found.";
        }

        return string.Join("\n\n", articles.Select(a =>
            $"**{a.Title}** (ID: {a.Id})\n{a.Content}"));
    }

    [Description("Creates a new support ticket")]
    public async Task<string> CreateTicketAsync(
        [Description("Customer identifier")] string customerId,
        [Description("Brief subject line")] string subject,
        [Description("Detailed description of the issue")] string description,
        [Description("Priority: Low, Medium, High, or Critical")] string priority,
        CancellationToken ct = default)
    {
        if (!Enum.TryParse<TicketPriority>(priority, true, out var ticketPriority))
        {
            ticketPriority = TicketPriority.Medium;
        }

        var ticket = await ticketService.CreateTicketAsync(
            customerId, subject, description, ticketPriority, ct);

        return $"Ticket created: {ticket.Id} with priority {ticket.Priority}";
    }

    [Description("Escalates an existing ticket to a human agent")]
    public async Task<string> EscalateTicketAsync(
        [Description("Ticket ID to escalate")] string ticketId,
        [Description("Reason for escalation")] string reason,
        CancellationToken ct = default)
    {
        var ticket = await ticketService.EscalateTicketAsync(ticketId, reason, ct);
        return $"Ticket {ticket.Id} has been escalated. A human agent will respond shortly.";
    }
}

// Main agent
public sealed class CustomerSupportAgent(IChatClient chatClient, SupportTools tools)
{
    private readonly ChatOptions _options = new()
    {
        Tools =
        [
            AIFunctionFactory.Create(tools.SearchKnowledgeBaseAsync),
            AIFunctionFactory.Create(tools.CreateTicketAsync),
            AIFunctionFactory.Create(tools.EscalateTicketAsync)
        ]
    };

    private const string SystemPrompt = """
        You are a helpful customer support agent. Your responsibilities:

        1. First, try to resolve issues using the knowledge base
        2. If the knowledge base doesn't have an answer, create a ticket
        3. Escalate tickets only when:
           - The customer explicitly requests human assistance
           - The issue involves billing disputes
           - The issue is time-sensitive and critical

        Always be polite and professional. Acknowledge the customer's frustration when appropriate.
        """;

    public async Task<SupportResponse> HandleInquiryAsync(
        string customerId,
        string inquiry,
        CancellationToken ct = default)
    {
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, SystemPrompt),
            new(ChatRole.User, $"Customer ID: {customerId}\n\nInquiry: {inquiry}")
        };

        var response = await chatClient.GetResponseAsync(messages, _options, ct);

        return new SupportResponse(
            response.Text,
            ExtractTicketId(response),
            response.FinishReason == ChatFinishReason.ToolCalls);
    }

    private static string? ExtractTicketId(ChatResponse response) =>
        response.Messages
            .Where(m => m.Role == ChatRole.Tool)
            .SelectMany(m => m.Contents.OfType<TextContent>())
            .Select(c => ExtractTicketIdFromText(c.Text))
            .FirstOrDefault(id => id is not null);

    private static string? ExtractTicketIdFromText(string? text)
    {
        if (text is null) return null;
        var match = System.Text.RegularExpressions.Regex.Match(text, @"Ticket created: (\S+)");
        return match.Success ? match.Groups[1].Value : null;
    }
}

public record SupportResponse(string Message, string? TicketId, bool UsedTools);

// ASP.NET Core integration
public static class SupportEndpoints
{
    public static void MapSupportEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/support/inquiries", async (
            SupportInquiryRequest request,
            CustomerSupportAgent agent,
            CancellationToken ct) =>
        {
            var response = await agent.HandleInquiryAsync(
                request.CustomerId,
                request.Inquiry,
                ct);

            return Results.Ok(response);
        });
    }
}

public record SupportInquiryRequest(string CustomerId, string Inquiry);
```

## Code Review Agent

An agent that reviews code changes and provides structured feedback.

```csharp
// Domain models
public record CodeReview(
    string FilePath,
    ReviewSeverity Severity,
    string Category,
    int? LineNumber,
    string Issue,
    string Suggestion);

public enum ReviewSeverity { Info, Warning, Error, Critical }

public record ReviewSummary(
    int TotalIssues,
    int CriticalCount,
    int ErrorCount,
    int WarningCount,
    int InfoCount,
    CodeReview[] Reviews,
    string OverallAssessment);

// Agent implementation
public sealed class CodeReviewAgent(IChatClient chatClient)
{
    private const string SystemPrompt = """
        You are an expert code reviewer. Analyze code for:

        1. **Security vulnerabilities** (SQL injection, XSS, CSRF, secrets in code)
        2. **Performance issues** (N+1 queries, unnecessary allocations, blocking calls in async)
        3. **Code quality** (SOLID violations, code smells, maintainability)
        4. **Best practices** (.NET conventions, naming, error handling)
        5. **Potential bugs** (null reference risks, race conditions, edge cases)

        For each issue found, provide:
        - Severity: Critical, Error, Warning, or Info
        - Category: Security, Performance, Quality, BestPractice, or Bug
        - Line number if applicable
        - Clear explanation of the issue
        - Concrete suggestion for improvement

        Be constructive and educational in your feedback.
        """;

    public async Task<ReviewSummary> ReviewCodeAsync(
        string filePath,
        string code,
        string? context = null,
        CancellationToken ct = default)
    {
        var userPrompt = BuildPrompt(filePath, code, context);

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, SystemPrompt),
            new(ChatRole.User, userPrompt)
        };

        var reviews = await chatClient.GetResponseAsync<CodeReview[]>(messages, cancellationToken: ct);

        return CreateSummary(reviews);
    }

    public async Task<ReviewSummary> ReviewDiffAsync(
        string diffContent,
        CancellationToken ct = default)
    {
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, SystemPrompt),
            new(ChatRole.User, $"Review this diff:\n\n```diff\n{diffContent}\n```")
        };

        var reviews = await chatClient.GetResponseAsync<CodeReview[]>(messages, cancellationToken: ct);

        return CreateSummary(reviews);
    }

    private static string BuildPrompt(string filePath, string code, string? context)
    {
        var prompt = $"Review this code from `{filePath}`:\n\n```csharp\n{code}\n```";

        if (!string.IsNullOrWhiteSpace(context))
        {
            prompt += $"\n\nAdditional context:\n{context}";
        }

        return prompt;
    }

    private static ReviewSummary CreateSummary(CodeReview[] reviews)
    {
        var criticalCount = reviews.Count(r => r.Severity == ReviewSeverity.Critical);
        var errorCount = reviews.Count(r => r.Severity == ReviewSeverity.Error);
        var warningCount = reviews.Count(r => r.Severity == ReviewSeverity.Warning);
        var infoCount = reviews.Count(r => r.Severity == ReviewSeverity.Info);

        var assessment = (criticalCount, errorCount) switch
        {
            ( > 0, _) => "Critical issues found. Do not merge until resolved.",
            (_, > 2) => "Significant issues found. Review carefully before merging.",
            (_, > 0) => "Some issues found. Consider addressing before merging.",
            _ when warningCount > 3 => "Minor issues found. Acceptable for merge with follow-up.",
            _ => "Code looks good. Approved for merge."
        };

        return new ReviewSummary(
            reviews.Length,
            criticalCount,
            errorCount,
            warningCount,
            infoCount,
            reviews,
            assessment);
    }
}

// Usage in a CI/CD context
public sealed class PullRequestReviewService(
    CodeReviewAgent reviewAgent,
    IGitHubClient github)
{
    public async Task ReviewPullRequestAsync(
        string owner,
        string repo,
        int pullRequestNumber,
        CancellationToken ct = default)
    {
        var files = await github.GetPullRequestFilesAsync(owner, repo, pullRequestNumber, ct);

        var allReviews = new List<CodeReview>();

        foreach (var file in files.Where(f => f.Filename.EndsWith(".cs")))
        {
            var content = await github.GetFileContentAsync(owner, repo, file.Sha, ct);
            var summary = await reviewAgent.ReviewCodeAsync(file.Filename, content, ct: ct);
            allReviews.AddRange(summary.Reviews);
        }

        var comment = FormatReviewComment(allReviews);
        await github.CreatePullRequestCommentAsync(owner, repo, pullRequestNumber, comment, ct);
    }

    private static string FormatReviewComment(List<CodeReview> reviews)
    {
        if (reviews.Count == 0)
        {
            return "## Code Review\n\nNo issues found.";
        }

        var sb = new StringBuilder();
        sb.AppendLine("## Code Review\n");

        foreach (var group in reviews.GroupBy(r => r.FilePath))
        {
            sb.AppendLine($"### {group.Key}\n");

            foreach (var review in group.OrderByDescending(r => r.Severity))
            {
                var emoji = review.Severity switch
                {
                    ReviewSeverity.Critical => "[CRITICAL]",
                    ReviewSeverity.Error => "[ERROR]",
                    ReviewSeverity.Warning => "[WARNING]",
                    _ => "[INFO]"
                };

                var line = review.LineNumber.HasValue ? $" (line {review.LineNumber})" : "";
                sb.AppendLine($"- {emoji}{line} **{review.Category}**: {review.Issue}");
                sb.AppendLine($"  - Suggestion: {review.Suggestion}");
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }
}
```

## Document Processing Pipeline

A multi-agent pipeline for processing and analyzing documents.

```csharp
// Domain models
public record Document(string Id, string Content, DocumentMetadata Metadata);
public record DocumentMetadata(string Title, string Author, DateTime CreatedAt);

public record ExtractedEntities(
    string[] People,
    string[] Organizations,
    string[] Locations,
    string[] Dates,
    KeyValuePair<string, string>[] CustomEntities);

public record DocumentSummary(
    string ExecutiveSummary,
    string[] KeyPoints,
    string[] ActionItems,
    string Sentiment);

public record ProcessedDocument(
    Document Original,
    ExtractedEntities Entities,
    DocumentSummary Summary,
    string[] Categories);

// Extraction agent
public sealed class EntityExtractionAgent(IChatClient chatClient)
{
    private const string SystemPrompt = """
        Extract named entities from the document. Identify:
        - People: Names of individuals mentioned
        - Organizations: Company names, institutions, government bodies
        - Locations: Cities, countries, addresses, geographic features
        - Dates: Specific dates, time periods, deadlines
        - Custom: Any domain-specific entities relevant to the content
        """;

    public async Task<ExtractedEntities> ExtractEntitiesAsync(
        string content,
        CancellationToken ct = default)
    {
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, SystemPrompt),
            new(ChatRole.User, content)
        };

        return await chatClient.GetResponseAsync<ExtractedEntities>(messages, cancellationToken: ct);
    }
}

// Summarization agent
public sealed class SummarizationAgent(IChatClient chatClient)
{
    private const string SystemPrompt = """
        Analyze the document and provide:
        1. Executive summary (2-3 sentences capturing the main point)
        2. Key points (3-5 bullet points of important information)
        3. Action items (any tasks, deadlines, or follow-ups mentioned)
        4. Overall sentiment (Positive, Negative, Neutral, or Mixed)
        """;

    public async Task<DocumentSummary> SummarizeAsync(
        string content,
        CancellationToken ct = default)
    {
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, SystemPrompt),
            new(ChatRole.User, content)
        };

        return await chatClient.GetResponseAsync<DocumentSummary>(messages, cancellationToken: ct);
    }
}

// Classification agent
public sealed class ClassificationAgent(IChatClient chatClient)
{
    private readonly string[] _availableCategories =
    [
        "Legal", "Financial", "Technical", "Marketing", "HR",
        "Operations", "Strategy", "Customer", "Compliance", "Other"
    ];

    public async Task<string[]> ClassifyAsync(
        string content,
        CancellationToken ct = default)
    {
        var categoriesJson = JsonSerializer.Serialize(_availableCategories);

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, $"Classify the document into one or more categories: {categoriesJson}"),
            new(ChatRole.User, content)
        };

        return await chatClient.GetResponseAsync<string[]>(messages, cancellationToken: ct);
    }
}

// Pipeline orchestrator
public sealed class DocumentProcessingPipeline(
    EntityExtractionAgent entityAgent,
    SummarizationAgent summaryAgent,
    ClassificationAgent classificationAgent,
    ILogger<DocumentProcessingPipeline> logger)
{
    public async Task<ProcessedDocument> ProcessAsync(
        Document document,
        CancellationToken ct = default)
    {
        logger.LogInformation("Processing document {Id}", document.Id);

        // Run all agents in parallel for efficiency
        var entitiesTask = entityAgent.ExtractEntitiesAsync(document.Content, ct);
        var summaryTask = summaryAgent.SummarizeAsync(document.Content, ct);
        var categoriesTask = classificationAgent.ClassifyAsync(document.Content, ct);

        await Task.WhenAll(entitiesTask, summaryTask, categoriesTask);

        var result = new ProcessedDocument(
            document,
            await entitiesTask,
            await summaryTask,
            await categoriesTask);

        logger.LogInformation(
            "Document {Id} processed: {EntityCount} entities, {CategoryCount} categories",
            document.Id,
            CountEntities(result.Entities),
            result.Categories.Length);

        return result;
    }

    public async IAsyncEnumerable<ProcessedDocument> ProcessBatchAsync(
        IEnumerable<Document> documents,
        int maxConcurrency = 3,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var semaphore = new SemaphoreSlim(maxConcurrency);

        var tasks = documents.Select(async doc =>
        {
            await semaphore.WaitAsync(ct);
            try
            {
                return await ProcessAsync(doc, ct);
            }
            finally
            {
                semaphore.Release();
            }
        }).ToList();

        foreach (var task in tasks)
        {
            yield return await task;
        }
    }

    private static int CountEntities(ExtractedEntities entities) =>
        entities.People.Length +
        entities.Organizations.Length +
        entities.Locations.Length +
        entities.Dates.Length +
        entities.CustomEntities.Length;
}

// DI registration
public static class DocumentProcessingExtensions
{
    public static IServiceCollection AddDocumentProcessing(
        this IServiceCollection services)
    {
        services.AddScoped<EntityExtractionAgent>();
        services.AddScoped<SummarizationAgent>();
        services.AddScoped<ClassificationAgent>();
        services.AddScoped<DocumentProcessingPipeline>();

        return services;
    }
}
```

## RAG (Retrieval-Augmented Generation) Agent

An agent that combines vector search with LLM capabilities.

```csharp
// Abstractions
public interface IVectorStore
{
    Task<SearchResult[]> SearchAsync(
        float[] queryEmbedding,
        int topK = 5,
        CancellationToken ct = default);

    Task IndexAsync(string id, string content, float[] embedding, CancellationToken ct = default);
}

public record SearchResult(string Id, string Content, float Score, Dictionary<string, string> Metadata);

// Embedding service
public sealed class EmbeddingService(IEmbeddingGenerator<string, Embedding<float>> generator)
{
    public async Task<float[]> GenerateEmbeddingAsync(
        string text,
        CancellationToken ct = default)
    {
        var embedding = await generator.GenerateEmbeddingAsync(text, cancellationToken: ct);
        return embedding.Vector.ToArray();
    }

    public async Task<float[][]> GenerateEmbeddingsAsync(
        IEnumerable<string> texts,
        CancellationToken ct = default)
    {
        var embeddings = await generator.GenerateAsync(texts, cancellationToken: ct);
        return embeddings.Select(e => e.Vector.ToArray()).ToArray();
    }
}

// RAG agent
public sealed class RagAgent(
    IChatClient chatClient,
    EmbeddingService embeddingService,
    IVectorStore vectorStore,
    ILogger<RagAgent> logger)
{
    private const string SystemPrompt = """
        You are a helpful assistant with access to a knowledge base.
        Answer questions based on the provided context.
        If the context doesn't contain relevant information, say so clearly.
        Always cite your sources when possible.
        """;

    public async Task<RagResponse> QueryAsync(
        string question,
        int contextSize = 5,
        CancellationToken ct = default)
    {
        // Generate embedding for the question
        var queryEmbedding = await embeddingService.GenerateEmbeddingAsync(question, ct);

        // Search for relevant documents
        var searchResults = await vectorStore.SearchAsync(queryEmbedding, contextSize, ct);

        logger.LogInformation(
            "Found {Count} relevant documents for query",
            searchResults.Length);

        // Build context from search results
        var context = BuildContext(searchResults);

        // Generate response with context
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, SystemPrompt),
            new(ChatRole.User, $"Context:\n{context}\n\nQuestion: {question}")
        };

        var response = await chatClient.GetResponseAsync(messages, cancellationToken: ct);

        return new RagResponse(
            response.Text,
            searchResults.Select(r => new SourceReference(r.Id, r.Score, r.Metadata)).ToArray());
    }

    public async Task IndexDocumentAsync(
        string id,
        string content,
        Dictionary<string, string>? metadata = null,
        CancellationToken ct = default)
    {
        // Split content into chunks for better retrieval
        var chunks = ChunkContent(content);

        for (var i = 0; i < chunks.Length; i++)
        {
            var chunkId = $"{id}_chunk_{i}";
            var embedding = await embeddingService.GenerateEmbeddingAsync(chunks[i], ct);

            await vectorStore.IndexAsync(chunkId, chunks[i], embedding, ct);
        }

        logger.LogInformation(
            "Indexed document {Id} with {ChunkCount} chunks",
            id,
            chunks.Length);
    }

    private static string BuildContext(SearchResult[] results)
    {
        return string.Join("\n\n---\n\n", results.Select((r, i) =>
            $"[Source {i + 1}] (Relevance: {r.Score:P0})\n{r.Content}"));
    }

    private static string[] ChunkContent(string content, int chunkSize = 500, int overlap = 50)
    {
        if (content.Length <= chunkSize)
        {
            return [content];
        }

        var chunks = new List<string>();
        var position = 0;

        while (position < content.Length)
        {
            var length = Math.Min(chunkSize, content.Length - position);
            chunks.Add(content.Substring(position, length));
            position += chunkSize - overlap;
        }

        return chunks.ToArray();
    }
}

public record RagResponse(string Answer, SourceReference[] Sources);
public record SourceReference(string DocumentId, float RelevanceScore, Dictionary<string, string> Metadata);

// Minimal API endpoints
public static class RagEndpoints
{
    public static void MapRagEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/rag");

        group.MapPost("/query", async (
            QueryRequest request,
            RagAgent agent,
            CancellationToken ct) =>
        {
            var response = await agent.QueryAsync(request.Question, request.ContextSize, ct);
            return Results.Ok(response);
        });

        group.MapPost("/index", async (
            IndexRequest request,
            RagAgent agent,
            CancellationToken ct) =>
        {
            await agent.IndexDocumentAsync(request.Id, request.Content, request.Metadata, ct);
            return Results.Accepted();
        });
    }
}

public record QueryRequest(string Question, int ContextSize = 5);
public record IndexRequest(string Id, string Content, Dictionary<string, string>? Metadata);
```

## Conversational Shopping Agent

An e-commerce agent with cart management, product search, and checkout assistance.

```csharp
// Domain models
public record Product(string Id, string Name, string Description, decimal Price, int Stock);
public record CartItem(string ProductId, string ProductName, int Quantity, decimal UnitPrice);
public record Cart(string SessionId, List<CartItem> Items);

// Services
public interface IProductCatalog
{
    Task<Product[]> SearchAsync(string query, CancellationToken ct = default);
    Task<Product?> GetByIdAsync(string id, CancellationToken ct = default);
}

public interface ICartService
{
    Task<Cart> GetCartAsync(string sessionId, CancellationToken ct = default);
    Task<Cart> AddToCartAsync(string sessionId, string productId, int quantity, CancellationToken ct = default);
    Task<Cart> RemoveFromCartAsync(string sessionId, string productId, CancellationToken ct = default);
    Task<Cart> UpdateQuantityAsync(string sessionId, string productId, int quantity, CancellationToken ct = default);
}

// Shopping tools
public sealed class ShoppingTools(
    IProductCatalog catalog,
    ICartService cartService,
    string sessionId)
{
    [Description("Searches for products matching the query")]
    public async Task<string> SearchProductsAsync(
        [Description("Search terms")] string query,
        CancellationToken ct = default)
    {
        var products = await catalog.SearchAsync(query, ct);

        if (products.Length == 0)
        {
            return "No products found matching your search.";
        }

        return string.Join("\n", products.Select(p =>
            $"- {p.Name} (ID: {p.Id}) - ${p.Price:F2} - {(p.Stock > 0 ? "In Stock" : "Out of Stock")}"));
    }

    [Description("Gets details about a specific product")]
    public async Task<string> GetProductDetailsAsync(
        [Description("Product ID")] string productId,
        CancellationToken ct = default)
    {
        var product = await catalog.GetByIdAsync(productId, ct);

        if (product is null)
        {
            return "Product not found.";
        }

        return $"""
            **{product.Name}**
            Price: ${product.Price:F2}
            Stock: {product.Stock} available
            Description: {product.Description}
            """;
    }

    [Description("Adds a product to the shopping cart")]
    public async Task<string> AddToCartAsync(
        [Description("Product ID to add")] string productId,
        [Description("Quantity to add")] int quantity = 1,
        CancellationToken ct = default)
    {
        var cart = await cartService.AddToCartAsync(sessionId, productId, quantity, ct);
        return $"Added to cart. Cart now has {cart.Items.Count} item(s), total: ${cart.Items.Sum(i => i.UnitPrice * i.Quantity):F2}";
    }

    [Description("Shows the current shopping cart")]
    public async Task<string> ViewCartAsync(CancellationToken ct = default)
    {
        var cart = await cartService.GetCartAsync(sessionId, ct);

        if (cart.Items.Count == 0)
        {
            return "Your cart is empty.";
        }

        var items = string.Join("\n", cart.Items.Select(i =>
            $"- {i.ProductName} x{i.Quantity} @ ${i.UnitPrice:F2} = ${i.UnitPrice * i.Quantity:F2}"));

        var total = cart.Items.Sum(i => i.UnitPrice * i.Quantity);

        return $"""
            **Your Cart:**
            {items}

            **Total: ${total:F2}**
            """;
    }

    [Description("Removes a product from the cart")]
    public async Task<string> RemoveFromCartAsync(
        [Description("Product ID to remove")] string productId,
        CancellationToken ct = default)
    {
        await cartService.RemoveFromCartAsync(sessionId, productId, ct);
        return "Item removed from cart.";
    }
}

// Shopping agent
public sealed class ShoppingAgent(
    IChatClient chatClient,
    IProductCatalog catalog,
    ICartService cartService)
{
    private const string SystemPrompt = """
        You are a friendly shopping assistant. Help customers:
        - Find products they're looking for
        - Compare options and make recommendations
        - Manage their shopping cart
        - Answer questions about products

        Be helpful and suggest related products when appropriate.
        Always confirm before adding items to the cart.
        """;

    public async Task<string> ChatAsync(
        string sessionId,
        string userMessage,
        List<ChatMessage> conversationHistory,
        CancellationToken ct = default)
    {
        var tools = new ShoppingTools(catalog, cartService, sessionId);

        var options = new ChatOptions
        {
            Tools =
            [
                AIFunctionFactory.Create(tools.SearchProductsAsync),
                AIFunctionFactory.Create(tools.GetProductDetailsAsync),
                AIFunctionFactory.Create(tools.AddToCartAsync),
                AIFunctionFactory.Create(tools.ViewCartAsync),
                AIFunctionFactory.Create(tools.RemoveFromCartAsync)
            ]
        };

        conversationHistory.Insert(0, new ChatMessage(ChatRole.System, SystemPrompt));
        conversationHistory.Add(new ChatMessage(ChatRole.User, userMessage));

        var response = await chatClient.GetResponseAsync(conversationHistory, options, ct);

        conversationHistory.Add(new ChatMessage(ChatRole.Assistant, response.Text));

        return response.Text;
    }
}

// SignalR hub for real-time shopping
public sealed class ShoppingHub(ShoppingAgent agent, ICartService cartService) : Hub
{
    private readonly ConcurrentDictionary<string, List<ChatMessage>> _sessions = new();

    public async Task<string> SendMessage(string message)
    {
        var sessionId = Context.ConnectionId;
        var history = _sessions.GetOrAdd(sessionId, _ => new List<ChatMessage>());

        var response = await agent.ChatAsync(sessionId, message, history);

        // Notify about cart updates
        var cart = await cartService.GetCartAsync(sessionId);
        await Clients.Caller.SendAsync("CartUpdated", cart);

        return response;
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        _sessions.TryRemove(Context.ConnectionId, out _);
        return base.OnDisconnectedAsync(exception);
    }
}
```

## Workflow Automation Agent

An agent that orchestrates complex business workflows with approval chains.

```csharp
// Workflow definitions
public record WorkflowDefinition(
    string Id,
    string Name,
    WorkflowStep[] Steps);

public record WorkflowStep(
    string Id,
    string Name,
    string Description,
    StepType Type,
    string? AssignedRole,
    Dictionary<string, object>? Parameters);

public enum StepType { Automated, ManualReview, Approval, Notification }

public record WorkflowInstance(
    string Id,
    string DefinitionId,
    string CurrentStepId,
    WorkflowStatus Status,
    Dictionary<string, object> Context,
    List<WorkflowEvent> History);

public enum WorkflowStatus { Pending, InProgress, AwaitingApproval, Completed, Failed, Cancelled }

public record WorkflowEvent(
    DateTime Timestamp,
    string StepId,
    string EventType,
    string? Actor,
    string Description);

// Workflow engine
public interface IWorkflowEngine
{
    Task<WorkflowInstance> StartWorkflowAsync(string definitionId, Dictionary<string, object> input, CancellationToken ct = default);
    Task<WorkflowInstance> GetWorkflowAsync(string instanceId, CancellationToken ct = default);
    Task<WorkflowInstance> ApproveStepAsync(string instanceId, string actorId, string? comment, CancellationToken ct = default);
    Task<WorkflowInstance> RejectStepAsync(string instanceId, string actorId, string reason, CancellationToken ct = default);
}

// Workflow agent
public sealed class WorkflowAgent(
    IChatClient chatClient,
    IWorkflowEngine workflowEngine,
    ILogger<WorkflowAgent> logger)
{
    private const string SystemPrompt = """
        You are a workflow automation assistant. Help users:
        - Start new workflows with appropriate context
        - Check workflow status and history
        - Explain what approvals are needed
        - Summarize pending items

        Be clear about what actions require human approval vs automated processing.
        """;

    public async Task<WorkflowResponse> ProcessRequestAsync(
        string userId,
        string request,
        CancellationToken ct = default)
    {
        var tools = CreateWorkflowTools(userId);

        var options = new ChatOptions
        {
            Tools = tools
        };

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, SystemPrompt),
            new(ChatRole.User, request)
        };

        var response = await chatClient.GetResponseAsync(messages, options, ct);

        return new WorkflowResponse(response.Text, ExtractWorkflowIds(response));
    }

    private AIFunction[] CreateWorkflowTools(string userId)
    {
        return
        [
            AIFunctionFactory.Create(
                async ([Description("Workflow definition ID")] string definitionId,
                       [Description("Input data as JSON")] string inputJson,
                       CancellationToken ct) =>
                {
                    var input = JsonSerializer.Deserialize<Dictionary<string, object>>(inputJson) ?? [];
                    input["initiatedBy"] = userId;

                    var instance = await workflowEngine.StartWorkflowAsync(definitionId, input, ct);

                    logger.LogInformation("User {UserId} started workflow {InstanceId}", userId, instance.Id);

                    return $"Workflow started: {instance.Id}, Status: {instance.Status}";
                },
                "StartWorkflow",
                "Starts a new workflow instance"),

            AIFunctionFactory.Create(
                async ([Description("Workflow instance ID")] string instanceId, CancellationToken ct) =>
                {
                    var instance = await workflowEngine.GetWorkflowAsync(instanceId, ct);

                    return $"""
                        Workflow: {instance.Id}
                        Status: {instance.Status}
                        Current Step: {instance.CurrentStepId}
                        History:
                        {string.Join("\n", instance.History.TakeLast(5).Select(e =>
                            $"  - [{e.Timestamp:g}] {e.EventType}: {e.Description}"))}
                        """;
                },
                "GetWorkflowStatus",
                "Gets the current status of a workflow"),

            AIFunctionFactory.Create(
                async ([Description("Workflow instance ID")] string instanceId,
                       [Description("Optional approval comment")] string? comment,
                       CancellationToken ct) =>
                {
                    var instance = await workflowEngine.ApproveStepAsync(instanceId, userId, comment, ct);

                    logger.LogInformation("User {UserId} approved step in workflow {InstanceId}", userId, instance.Id);

                    return $"Step approved. Workflow status: {instance.Status}";
                },
                "ApproveWorkflowStep",
                "Approves the current pending step in a workflow")
        ];
    }

    private static string[] ExtractWorkflowIds(ChatResponse response)
    {
        var pattern = new Regex(@"Workflow(?:\sstarted)?:\s*(\S+)", RegexOptions.IgnoreCase);
        return response.Messages
            .SelectMany(m => m.Contents.OfType<TextContent>())
            .SelectMany(c => pattern.Matches(c.Text ?? ""))
            .Select(m => m.Groups[1].Value)
            .Distinct()
            .ToArray();
    }
}

public record WorkflowResponse(string Message, string[] AffectedWorkflowIds);
```
