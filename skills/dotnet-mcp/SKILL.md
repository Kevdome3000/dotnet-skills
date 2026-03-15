---
name: dotnet-mcp
version: "1.0.0"
category: "AI"
description: "Implement Model Context Protocol (MCP) servers and clients in .NET to enable secure, standardized communication between LLM applications and external tools or data sources."
compatibility: "Requires ModelContextProtocol NuGet packages (.NET 8+)."
---

# Model Context Protocol (MCP) for .NET

## Trigger On

- building MCP servers to expose tools/resources to AI agents
- integrating LLM applications with external data sources
- creating AI-enabled applications that need structured context
- implementing tool-calling interfaces for Claude, GPT, or other LLMs
- extending AI agent capabilities with custom tools

## Documentation

- [MCP C# SDK Repository](https://github.com/modelcontextprotocol/csharp-sdk)
- [Model Context Protocol Specification](https://spec.modelcontextprotocol.io/)
- [MCP Servers Documentation](https://modelcontextprotocol.io/docs/concepts/servers)
- [MCP Tools Documentation](https://modelcontextprotocol.io/docs/concepts/tools)

## References

See detailed examples in the `references/` folder:
- [`patterns.md`](references/patterns.md) — Tool, resource, and prompt patterns
- [`security.md`](references/security.md) — Input validation, auth, rate limiting, and audit patterns

## Core Concepts

| Concept | Description |
|---------|-------------|
| **Server** | Exposes tools, resources, and prompts to MCP clients |
| **Client** | Connects to MCP servers and invokes capabilities |
| **Tool** | Executable function the LLM can call |
| **Resource** | Data source the LLM can read |
| **Prompt** | Reusable prompt template |

## Package Selection

| Package | Use Case |
|---------|----------|
| `ModelContextProtocol.Core` | Minimal dependencies, low-level APIs |
| `ModelContextProtocol` | Main package with DI and hosting |
| `ModelContextProtocol.AspNetCore` | HTTP-based MCP servers |

## Workflow

1. **Define tools and resources** — what capabilities to expose
2. **Create MCP server** — host tools for AI agents
3. **Register with DI** — use hosting extensions
4. **Handle requests** — implement tool logic
5. **Test with clients** — verify with MCP Inspector or real agents

## MCP Server Setup

### Basic Server with Tools
```csharp
var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddMcpServer()
    .WithStdioTransport()
    .WithTools<WeatherTools>()
    .WithTools<FileTools>();

var host = builder.Build();
await host.RunAsync();
```

### Tool Definition
```csharp
public class WeatherTools
{
    [McpTool("get_weather")]
    [Description("Gets current weather for a city")]
    public async Task<WeatherResult> GetWeatherAsync(
        [Description("City name, e.g., 'Seattle'")] string city,
        [Description("Unit: 'celsius' or 'fahrenheit'")] string unit = "celsius",
        CancellationToken cancellationToken = default)
    {
        var weather = await FetchWeatherAsync(city, cancellationToken);
        return new WeatherResult
        {
            Temperature = unit == "celsius" ? weather.TempC : weather.TempF,
            Condition = weather.Condition,
            Unit = unit
        };
    }

    [McpTool("get_forecast")]
    [Description("Gets weather forecast for upcoming days")]
    public async Task<ForecastResult> GetForecastAsync(
        [Description("City name")] string city,
        [Description("Number of days (1-7)")] int days = 3,
        CancellationToken cancellationToken = default)
    {
        // Implementation
    }
}
```

### Resource Definition
```csharp
public class DocumentResources
{
    [McpResource("documents/{id}")]
    [Description("Gets a document by ID")]
    public async Task<DocumentContent> GetDocumentAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        var doc = await _documentService.GetAsync(id, cancellationToken);
        return new DocumentContent
        {
            MimeType = "text/plain",
            Text = doc.Content
        };
    }

    [McpResourceList("documents")]
    [Description("Lists all available documents")]
    public async Task<IEnumerable<ResourceInfo>> ListDocumentsAsync(
        CancellationToken cancellationToken = default)
    {
        var docs = await _documentService.GetAllAsync(cancellationToken);
        return docs.Select(d => new ResourceInfo
        {
            Uri = $"documents/{d.Id}",
            Name = d.Title,
            Description = d.Summary
        });
    }
}
```

## HTTP Transport (ASP.NET Core)

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMcpServer()
    .WithTools<WeatherTools>()
    .WithResources<DocumentResources>();

var app = builder.Build();

app.MapMcp("/mcp");

app.Run();
```

## MCP Client

### Connecting to Server
```csharp
var client = await McpClient.ConnectAsync(
    new StdioTransportOptions
    {
        Command = "dotnet",
        Arguments = ["run", "--project", "MyMcpServer"]
    });

// List available tools
var tools = await client.ListToolsAsync();
foreach (var tool in tools)
{
    Console.WriteLine($"Tool: {tool.Name} - {tool.Description}");
}

// Call a tool
var result = await client.CallToolAsync("get_weather", new
{
    city = "Seattle",
    unit = "celsius"
});
```

### HTTP Client
```csharp
var client = await McpClient.ConnectAsync(
    new HttpTransportOptions
    {
        BaseUrl = "https://myserver.com/mcp"
    });
```

## Tool Patterns

### Tool with Complex Input
```csharp
public class DatabaseTools(IDbConnection db)
{
    [McpTool("query_products")]
    [Description("Searches products by criteria")]
    public async Task<ProductSearchResult> SearchProductsAsync(
        [Description("Product name filter")] string? name = null,
        [Description("Minimum price")] decimal? minPrice = null,
        [Description("Maximum price")] decimal? maxPrice = null,
        [Description("Category ID")] int? categoryId = null,
        [Description("Max results (1-100)")] int limit = 10,
        CancellationToken cancellationToken = default)
    {
        // Validate inputs
        if (limit < 1 || limit > 100)
            throw new ArgumentException("Limit must be between 1 and 100");

        var products = await db.QueryProductsAsync(
            name, minPrice, maxPrice, categoryId, limit, cancellationToken);

        return new ProductSearchResult
        {
            Products = products,
            TotalCount = products.Count
        };
    }
}
```

### Tool with Error Handling
```csharp
[McpTool("create_order")]
[Description("Creates a new order")]
public async Task<OrderResult> CreateOrderAsync(
    [Description("Product ID")] string productId,
    [Description("Quantity")] int quantity,
    CancellationToken cancellationToken = default)
{
    try
    {
        if (quantity < 1)
            return OrderResult.Error("Quantity must be at least 1");

        var product = await _productService.GetAsync(productId, cancellationToken);
        if (product is null)
            return OrderResult.Error($"Product '{productId}' not found");

        if (product.Stock < quantity)
            return OrderResult.Error($"Insufficient stock. Available: {product.Stock}");

        var order = await _orderService.CreateAsync(productId, quantity, cancellationToken);
        return OrderResult.Success(order);
    }
    catch (Exception ex)
    {
        return OrderResult.Error($"Failed to create order: {ex.Message}");
    }
}
```

## Prompt Templates

```csharp
public class PromptTemplates
{
    [McpPrompt("code_review")]
    [Description("Template for code review requests")]
    public PromptContent CodeReviewPrompt(
        [Description("Programming language")] string language,
        [Description("Code to review")] string code)
    {
        return new PromptContent
        {
            Messages =
            [
                new PromptMessage
                {
                    Role = "user",
                    Content = $"""
                        Please review the following {language} code:

                        ```{language}
                        {code}
                        ```

                        Focus on:
                        - Code quality and readability
                        - Potential bugs
                        - Performance issues
                        - Security concerns
                        """
                }
            ]
        };
    }
}
```

## Anti-Patterns to Avoid

| Anti-Pattern | Why It's Bad | Better Approach |
|--------------|--------------|-----------------|
| Vague tool descriptions | LLM can't decide when to call | Be specific and actionable |
| No input validation | Hallucinated parameters crash | Validate and return errors |
| Blocking operations | Timeout issues | Use async throughout |
| Exposing sensitive data | Security risk | Filter and sanitize |
| Giant tool responses | Token waste | Return essential data only |
| Missing cancellation | Can't stop long operations | Honor CancellationToken |

## Security Best Practices

1. **Validate all inputs:**
   ```csharp
   if (!Regex.IsMatch(filename, @"^[\w\-\.]+$"))
       throw new ArgumentException("Invalid filename");
   ```

2. **Sanitize file paths:**
   ```csharp
   var safePath = Path.GetFullPath(userPath);
   if (!safePath.StartsWith(_allowedRoot))
       throw new UnauthorizedAccessException();
   ```

3. **Rate limiting:**
   ```csharp
   builder.Services.AddMcpServer()
       .WithRateLimiting(options =>
       {
           options.MaxRequestsPerMinute = 60;
       });
   ```

4. **Audit logging:**
   ```csharp
   [McpTool("delete_file")]
   public async Task<bool> DeleteFileAsync(string path)
   {
       _logger.LogInformation("Delete requested: {Path} by {Client}", path, _context.ClientId);
       // ...
   }
   ```

## Testing

```csharp
public class WeatherToolsTests
{
    [Fact]
    public async Task GetWeather_ReturnsWeatherForCity()
    {
        var tools = new WeatherTools(Mock.Of<IWeatherService>(s =>
            s.GetCurrentAsync("Seattle", default) == Task.FromResult(
                new Weather { TempC = 15, Condition = "Cloudy" })));

        var result = await tools.GetWeatherAsync("Seattle", "celsius");

        Assert.Equal(15, result.Temperature);
        Assert.Equal("Cloudy", result.Condition);
    }
}
```

## Deliver

- MCP server exposing tools for AI agent integration
- Clear, specific tool descriptions for LLM understanding
- Proper input validation and error handling
- Secure resource access patterns

## Validate

- Tools have descriptive names and documentation
- All inputs are validated before processing
- Errors return meaningful messages, not exceptions
- Sensitive operations are logged
- Server works with MCP Inspector
- Integration with target AI agent is tested
