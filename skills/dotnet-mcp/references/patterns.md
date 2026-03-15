# MCP Patterns Reference

## Tool Patterns

### CRUD Tool Set
```csharp
public class ProductTools(IProductService products)
{
    [McpTool("list_products")]
    [Description("Lists all products with optional filtering")]
    public async Task<ProductListResult> ListProductsAsync(
        [Description("Filter by category")] string? category = null,
        [Description("Filter by minimum price")] decimal? minPrice = null,
        [Description("Maximum results (1-100)")] int limit = 20,
        CancellationToken ct = default)
    {
        if (limit < 1 || limit > 100)
            return ProductListResult.Error("Limit must be between 1 and 100");

        var products = await products.ListAsync(category, minPrice, limit, ct);
        return ProductListResult.Success(products);
    }

    [McpTool("get_product")]
    [Description("Gets a single product by ID")]
    public async Task<ProductResult> GetProductAsync(
        [Description("Product ID")] string id,
        CancellationToken ct = default)
    {
        var product = await products.GetAsync(id, ct);
        return product is null
            ? ProductResult.NotFound(id)
            : ProductResult.Success(product);
    }

    [McpTool("create_product")]
    [Description("Creates a new product")]
    public async Task<ProductResult> CreateProductAsync(
        [Description("Product name")] string name,
        [Description("Price in USD")] decimal price,
        [Description("Category")] string category,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            return ProductResult.Error("Name is required");
        if (price < 0)
            return ProductResult.Error("Price cannot be negative");

        var product = await products.CreateAsync(name, price, category, ct);
        return ProductResult.Created(product);
    }

    [McpTool("update_product")]
    [Description("Updates an existing product")]
    public async Task<ProductResult> UpdateProductAsync(
        [Description("Product ID")] string id,
        [Description("New name (optional)")] string? name = null,
        [Description("New price (optional)")] decimal? price = null,
        CancellationToken ct = default)
    {
        var product = await products.GetAsync(id, ct);
        if (product is null)
            return ProductResult.NotFound(id);

        if (price < 0)
            return ProductResult.Error("Price cannot be negative");

        var updated = await products.UpdateAsync(id, name, price, ct);
        return ProductResult.Success(updated);
    }

    [McpTool("delete_product")]
    [Description("Deletes a product")]
    public async Task<DeleteResult> DeleteProductAsync(
        [Description("Product ID")] string id,
        CancellationToken ct = default)
    {
        var exists = await products.ExistsAsync(id, ct);
        if (!exists)
            return DeleteResult.NotFound(id);

        await products.DeleteAsync(id, ct);
        return DeleteResult.Success(id);
    }
}
```

### Search Tool
```csharp
public class SearchTools(ISearchService search)
{
    [McpTool("search")]
    [Description("Searches across all content types")]
    public async Task<SearchResult> SearchAsync(
        [Description("Search query")] string query,
        [Description("Content types to search: products, orders, customers")] string[]? types = null,
        [Description("Maximum results per type (1-50)")] int limit = 10,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return SearchResult.Error("Query is required");

        if (query.Length < 2)
            return SearchResult.Error("Query must be at least 2 characters");

        types ??= ["products", "orders", "customers"];
        limit = Math.Clamp(limit, 1, 50);

        var results = await search.SearchAsync(query, types, limit, ct);
        return SearchResult.Success(results);
    }
}
```

### Batch Operations Tool
```csharp
public class BatchTools(IProductService products)
{
    [McpTool("batch_update_prices")]
    [Description("Updates prices for multiple products")]
    public async Task<BatchResult> BatchUpdatePricesAsync(
        [Description("Product IDs to update")] string[] productIds,
        [Description("Price adjustment (positive or negative)")] decimal adjustment,
        [Description("Adjustment type: 'fixed' or 'percentage'")] string adjustmentType = "fixed",
        CancellationToken ct = default)
    {
        if (productIds.Length == 0)
            return BatchResult.Error("No product IDs provided");

        if (productIds.Length > 100)
            return BatchResult.Error("Maximum 100 products per batch");

        if (adjustmentType != "fixed" && adjustmentType != "percentage")
            return BatchResult.Error("Adjustment type must be 'fixed' or 'percentage'");

        var results = new List<BatchItemResult>();
        foreach (var id in productIds)
        {
            try
            {
                var product = await products.GetAsync(id, ct);
                if (product is null)
                {
                    results.Add(BatchItemResult.NotFound(id));
                    continue;
                }

                var newPrice = adjustmentType == "percentage"
                    ? product.Price * (1 + adjustment / 100)
                    : product.Price + adjustment;

                if (newPrice < 0)
                {
                    results.Add(BatchItemResult.Error(id, "Resulting price would be negative"));
                    continue;
                }

                await products.UpdateAsync(id, price: newPrice, ct: ct);
                results.Add(BatchItemResult.Success(id, newPrice));
            }
            catch (Exception ex)
            {
                results.Add(BatchItemResult.Error(id, ex.Message));
            }
        }

        return BatchResult.Success(results);
    }
}
```

## Resource Patterns

### File Resource
```csharp
public class FileResources(IFileService files)
{
    [McpResource("files/{path}")]
    [Description("Gets file content by path")]
    public async Task<ResourceContent> GetFileAsync(
        string path,
        CancellationToken ct = default)
    {
        // Validate path
        if (path.Contains(".."))
            throw new UnauthorizedAccessException("Path traversal not allowed");

        var content = await files.ReadAsync(path, ct);
        var mimeType = GetMimeType(path);

        return new ResourceContent
        {
            MimeType = mimeType,
            Text = content
        };
    }

    [McpResourceList("files")]
    [Description("Lists available files")]
    public async Task<IEnumerable<ResourceInfo>> ListFilesAsync(
        CancellationToken ct = default)
    {
        var files = await files.ListAsync(ct);
        return files.Select(f => new ResourceInfo
        {
            Uri = $"files/{f.Path}",
            Name = f.Name,
            Description = $"{f.Size} bytes, modified {f.ModifiedAt:g}"
        });
    }

    private static string GetMimeType(string path) => Path.GetExtension(path) switch
    {
        ".json" => "application/json",
        ".xml" => "application/xml",
        ".md" => "text/markdown",
        ".txt" => "text/plain",
        ".csv" => "text/csv",
        _ => "application/octet-stream"
    };
}
```

### Database Resource
```csharp
public class DatabaseResources(IDbConnection db)
{
    [McpResource("tables/{tableName}/schema")]
    [Description("Gets table schema")]
    public async Task<ResourceContent> GetTableSchemaAsync(
        string tableName,
        CancellationToken ct = default)
    {
        var schema = await db.GetSchemaAsync(tableName, ct);
        return new ResourceContent
        {
            MimeType = "application/json",
            Text = JsonSerializer.Serialize(schema)
        };
    }

    [McpResource("tables/{tableName}/sample")]
    [Description("Gets sample data from table (first 10 rows)")]
    public async Task<ResourceContent> GetTableSampleAsync(
        string tableName,
        CancellationToken ct = default)
    {
        var sample = await db.QueryAsync(
            $"SELECT TOP 10 * FROM {tableName}", ct);
        return new ResourceContent
        {
            MimeType = "application/json",
            Text = JsonSerializer.Serialize(sample)
        };
    }

    [McpResourceList("tables")]
    [Description("Lists all database tables")]
    public async Task<IEnumerable<ResourceInfo>> ListTablesAsync(
        CancellationToken ct = default)
    {
        var tables = await db.GetTablesAsync(ct);
        return tables.Select(t => new ResourceInfo
        {
            Uri = $"tables/{t.Name}/schema",
            Name = t.Name,
            Description = $"{t.RowCount} rows"
        });
    }
}
```

## Prompt Patterns

### Multi-Step Prompt
```csharp
public class AnalysisPrompts
{
    [McpPrompt("code_analysis")]
    [Description("Comprehensive code analysis prompt")]
    public PromptContent CodeAnalysisPrompt(
        [Description("Programming language")] string language,
        [Description("Code to analyze")] string code,
        [Description("Focus areas: security, performance, style, all")] string focus = "all")
    {
        var focusInstructions = focus switch
        {
            "security" => "Focus specifically on security vulnerabilities and best practices.",
            "performance" => "Focus specifically on performance optimizations and bottlenecks.",
            "style" => "Focus specifically on code style, readability, and maintainability.",
            _ => "Analyze all aspects: security, performance, style, and correctness."
        };

        return new PromptContent
        {
            Messages =
            [
                new PromptMessage
                {
                    Role = "system",
                    Content = $"""
                        You are an expert {language} code reviewer.
                        {focusInstructions}
                        Provide specific, actionable feedback with code examples.
                        """
                },
                new PromptMessage
                {
                    Role = "user",
                    Content = $"""
                        Please analyze the following {language} code:

                        ```{language}
                        {code}
                        ```
                        """
                }
            ]
        };
    }
}
```

## Error Handling Patterns

### Result Types
```csharp
public record ToolResult
{
    public bool Success { get; init; }
    public string? Error { get; init; }
    public object? Data { get; init; }

    public static ToolResult Ok(object data) => new() { Success = true, Data = data };
    public static ToolResult Fail(string error) => new() { Success = false, Error = error };
}

public record ProductResult : ToolResult
{
    public Product? Product { get; init; }

    public static ProductResult Success(Product product) =>
        new() { Success = true, Product = product, Data = product };

    public static ProductResult NotFound(string id) =>
        new() { Success = false, Error = $"Product '{id}' not found" };

    public static ProductResult Error(string message) =>
        new() { Success = false, Error = message };

    public static ProductResult Created(Product product) =>
        new() { Success = true, Product = product, Data = product };
}
```

### Graceful Error Handling
```csharp
[McpTool("dangerous_operation")]
[Description("Performs an operation that might fail")]
public async Task<OperationResult> DangerousOperationAsync(
    string target,
    CancellationToken ct = default)
{
    try
    {
        // Validate
        if (string.IsNullOrEmpty(target))
            return OperationResult.Error("Target is required");

        // Execute
        var result = await _service.PerformAsync(target, ct);
        return OperationResult.Success(result);
    }
    catch (NotFoundException)
    {
        return OperationResult.Error($"Target '{target}' not found");
    }
    catch (UnauthorizedException)
    {
        return OperationResult.Error("Access denied to target");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Operation failed for {Target}", target);
        return OperationResult.Error("Operation failed. Please try again.");
    }
}
```
