# MCP Security Reference

## Input Validation

### Path Validation
```csharp
public class FileTools(IFileService files, string allowedRoot)
{
    [McpTool("read_file")]
    [Description("Reads a file from the allowed directory")]
    public async Task<FileResult> ReadFileAsync(
        [Description("Relative file path")] string path,
        CancellationToken ct = default)
    {
        // Prevent path traversal
        if (path.Contains(".."))
            return FileResult.Error("Path traversal not allowed");

        // Normalize and validate path
        var fullPath = Path.GetFullPath(Path.Combine(allowedRoot, path));
        if (!fullPath.StartsWith(allowedRoot))
            return FileResult.Error("Access denied: path outside allowed directory");

        // Check file extension whitelist
        var extension = Path.GetExtension(path).ToLowerInvariant();
        if (!IsAllowedExtension(extension))
            return FileResult.Error($"File type '{extension}' not allowed");

        var content = await files.ReadAsync(fullPath, ct);
        return FileResult.Success(content);
    }

    private static bool IsAllowedExtension(string ext) =>
        ext is ".txt" or ".json" or ".xml" or ".md" or ".csv";
}
```

### SQL Injection Prevention
```csharp
public class DatabaseTools(IDbConnection db)
{
    [McpTool("query_data")]
    [Description("Queries data from a table")]
    public async Task<QueryResult> QueryDataAsync(
        [Description("Table name")] string tableName,
        [Description("Column to filter")] string? filterColumn = null,
        [Description("Filter value")] string? filterValue = null,
        CancellationToken ct = default)
    {
        // Whitelist table names
        var allowedTables = new[] { "products", "orders", "customers" };
        if (!allowedTables.Contains(tableName.ToLowerInvariant()))
            return QueryResult.Error($"Table '{tableName}' not accessible");

        // Use parameterized queries
        var sql = $"SELECT * FROM {tableName}";
        var parameters = new DynamicParameters();

        if (!string.IsNullOrEmpty(filterColumn) && !string.IsNullOrEmpty(filterValue))
        {
            // Whitelist column names
            var columns = await db.GetColumnsAsync(tableName, ct);
            if (!columns.Contains(filterColumn))
                return QueryResult.Error($"Column '{filterColumn}' not found");

            sql += $" WHERE {filterColumn} = @value";
            parameters.Add("value", filterValue);
        }

        var results = await db.QueryAsync<dynamic>(sql, parameters);
        return QueryResult.Success(results);
    }
}
```

### Command Injection Prevention
```csharp
public class SystemTools
{
    private static readonly Regex SafeCommandPattern = new(@"^[a-zA-Z0-9_\-\.]+$");

    [McpTool("get_system_info")]
    [Description("Gets system information")]
    public Task<SystemInfo> GetSystemInfoAsync(
        [Description("Info type: cpu, memory, disk")] string infoType,
        CancellationToken ct = default)
    {
        // Never execute arbitrary commands
        // Use predefined safe operations only
        return infoType.ToLowerInvariant() switch
        {
            "cpu" => GetCpuInfoAsync(ct),
            "memory" => GetMemoryInfoAsync(ct),
            "disk" => GetDiskInfoAsync(ct),
            _ => Task.FromResult(SystemInfo.Error("Invalid info type"))
        };
    }

    // NEVER do this:
    // [McpTool("execute_command")]
    // public Task<string> ExecuteCommand(string command) =>
    //     Process.Start(command); // SECURITY VULNERABILITY
}
```

## Authentication & Authorization

### API Key Validation
```csharp
public class AuthenticatedTools
{
    private readonly IApiKeyValidator _validator;

    public AuthenticatedTools(IApiKeyValidator validator)
    {
        _validator = validator;
    }

    [McpTool("sensitive_operation")]
    [Description("Performs a sensitive operation requiring authentication")]
    public async Task<OperationResult> SensitiveOperationAsync(
        [Description("API key for authentication")] string apiKey,
        [Description("Operation to perform")] string operation,
        CancellationToken ct = default)
    {
        // Validate API key
        var keyInfo = await _validator.ValidateAsync(apiKey, ct);
        if (!keyInfo.IsValid)
            return OperationResult.Error("Invalid API key");

        // Check permissions
        if (!keyInfo.HasPermission(operation))
            return OperationResult.Error($"API key does not have permission for '{operation}'");

        // Log the operation
        _logger.LogInformation(
            "Sensitive operation {Operation} by key {KeyId}",
            operation, keyInfo.KeyId);

        // Perform operation
        return await PerformOperationAsync(operation, ct);
    }
}
```

### Role-Based Access
```csharp
public class AdminTools
{
    [McpTool("admin_delete_user")]
    [Description("Deletes a user (admin only)")]
    public async Task<DeleteResult> DeleteUserAsync(
        [Description("Admin token")] string adminToken,
        [Description("User ID to delete")] string userId,
        CancellationToken ct = default)
    {
        var admin = await _authService.ValidateAdminAsync(adminToken, ct);
        if (admin is null)
            return DeleteResult.Error("Admin authentication required");

        if (admin.Role != "SuperAdmin")
            return DeleteResult.Error("Insufficient permissions");

        // Audit log
        await _auditService.LogAsync(new AuditEntry
        {
            Action = "DeleteUser",
            PerformedBy = admin.Id,
            TargetId = userId,
            Timestamp = DateTime.UtcNow
        }, ct);

        await _userService.DeleteAsync(userId, ct);
        return DeleteResult.Success(userId);
    }
}
```

## Rate Limiting

### Per-Client Rate Limiting
```csharp
public class RateLimitedTools
{
    private readonly IRateLimiter _rateLimiter;

    [McpTool("expensive_operation")]
    [Description("Performs an expensive operation (rate limited)")]
    public async Task<OperationResult> ExpensiveOperationAsync(
        string input,
        CancellationToken ct = default)
    {
        var clientId = GetClientId();

        // Check rate limit
        if (!await _rateLimiter.TryAcquireAsync(clientId, "expensive_operation", ct))
        {
            var retryAfter = await _rateLimiter.GetRetryAfterAsync(clientId, ct);
            return OperationResult.RateLimited(retryAfter);
        }

        return await PerformExpensiveOperationAsync(input, ct);
    }
}

// Rate limiter configuration
builder.Services.AddMcpServer()
    .WithRateLimiting(options =>
    {
        options.DefaultLimit = new RateLimit
        {
            Requests = 100,
            Window = TimeSpan.FromMinutes(1)
        };

        options.ToolLimits["expensive_operation"] = new RateLimit
        {
            Requests = 10,
            Window = TimeSpan.FromMinutes(1)
        };
    });
```

## Audit Logging

### Comprehensive Audit Trail
```csharp
public class AuditedTools(IAuditService audit, ILogger<AuditedTools> logger)
{
    [McpTool("modify_data")]
    [Description("Modifies data with full audit trail")]
    public async Task<ModifyResult> ModifyDataAsync(
        [Description("Record ID")] string recordId,
        [Description("New value")] string newValue,
        CancellationToken ct = default)
    {
        var context = GetMcpContext();

        // Log before operation
        logger.LogInformation(
            "Data modification requested: Record={RecordId}, Client={ClientId}",
            recordId, context.ClientId);

        try
        {
            var oldValue = await _dataService.GetAsync(recordId, ct);

            await _dataService.UpdateAsync(recordId, newValue, ct);

            // Audit success
            await audit.LogAsync(new AuditEntry
            {
                Action = "ModifyData",
                RecordId = recordId,
                OldValue = oldValue,
                NewValue = newValue,
                ClientId = context.ClientId,
                Timestamp = DateTime.UtcNow,
                Success = true
            }, ct);

            return ModifyResult.Success();
        }
        catch (Exception ex)
        {
            // Audit failure
            await audit.LogAsync(new AuditEntry
            {
                Action = "ModifyData",
                RecordId = recordId,
                ClientId = context.ClientId,
                Timestamp = DateTime.UtcNow,
                Success = false,
                Error = ex.Message
            }, ct);

            logger.LogError(ex, "Data modification failed: Record={RecordId}", recordId);
            return ModifyResult.Error("Modification failed");
        }
    }
}
```

## Secrets Management

### Never Expose Secrets
```csharp
// BAD - exposes secrets
[McpTool("get_config")]
public Task<string> GetConfigAsync()
{
    return Task.FromResult(JsonSerializer.Serialize(new
    {
        ApiKey = Environment.GetEnvironmentVariable("API_KEY"), // WRONG
        DatabasePassword = _config["Database:Password"] // WRONG
    }));
}

// GOOD - safe configuration exposure
[McpTool("get_config")]
[Description("Gets safe configuration values")]
public Task<ConfigResult> GetConfigAsync()
{
    return Task.FromResult(new ConfigResult
    {
        Environment = _config["Environment"],
        Region = _config["Region"],
        FeatureFlags = _config.GetSection("Features").Get<Dictionary<string, bool>>()
        // No secrets exposed
    });
}
```

### Secure Secret Access
```csharp
public class SecureTools(ISecretManager secrets)
{
    [McpTool("call_external_api")]
    [Description("Calls an external API")]
    public async Task<ApiResult> CallExternalApiAsync(
        [Description("API endpoint")] string endpoint,
        CancellationToken ct = default)
    {
        // Get secret securely - never log or return it
        var apiKey = await secrets.GetSecretAsync("external-api-key", ct);

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", apiKey);

        var response = await client.GetAsync(endpoint, ct);
        var content = await response.Content.ReadAsStringAsync(ct);

        // Return response without exposing the key
        return new ApiResult
        {
            StatusCode = (int)response.StatusCode,
            Content = content
        };
    }
}
```

## Data Sanitization

### Output Sanitization
```csharp
[McpTool("get_user_data")]
[Description("Gets user data")]
public async Task<UserData> GetUserDataAsync(
    [Description("User ID")] string userId,
    CancellationToken ct = default)
{
    var user = await _userService.GetAsync(userId, ct);
    if (user is null)
        return UserData.NotFound(userId);

    // Sanitize sensitive fields
    return new UserData
    {
        Id = user.Id,
        Name = user.Name,
        Email = MaskEmail(user.Email), // Partially masked
        Phone = MaskPhone(user.Phone), // Partially masked
        // Do NOT include: Password, SSN, PaymentInfo
    };
}

private static string MaskEmail(string email)
{
    var parts = email.Split('@');
    if (parts.Length != 2) return "***@***";
    var name = parts[0].Length > 2
        ? parts[0][..2] + new string('*', parts[0].Length - 2)
        : "**";
    return $"{name}@{parts[1]}";
}
```
