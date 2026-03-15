# Model Context Protocol (MCP) Integration

## Overview

MCP is an open standard for providing tools and contextual data to LLMs. Agent Framework supports both consuming and exposing MCP tools.

## Using MCP Tools

### Local MCP Server (Stdio)

```csharp
using ModelContextProtocol;
using ModelContextProtocol.Client;

// Connect to MCP server
await using var mcpClient = await McpClientFactory.CreateAsync(new StdioClientTransport(new()
{
    Name = "MCPServer",
    Command = "npx",
    Arguments = ["-y", "@modelcontextprotocol/server-github"]
}));

// Get available tools
var mcpTools = await mcpClient.ListToolsAsync();

// Create agent with MCP tools
AIAgent agent = chatClient.AsAIAgent(
    instructions: "You answer questions about GitHub repositories.",
    tools: [.. mcpTools.Cast<AITool>()]);

// Agent can now use GitHub tools
var response = await agent.RunAsync(
    "Summarize the last 4 commits to microsoft/semantic-kernel");
```

### HTTP/SSE MCP Server

```csharp
// Python example
async with MCPStreamableHTTPTool(
    name="Microsoft Learn MCP",
    url="https://learn.microsoft.com/api/mcp",
    headers={"Authorization": f"Bearer {token}"}
) as mcp_server:

    async with Agent(
        client=chat_client,
        tools=mcp_server
    ) as agent:
        result = await agent.run("How to create Azure storage?")
```

### WebSocket MCP Server

```csharp
// Python example
async with MCPWebsocketTool(
    name="realtime-data",
    url="wss://api.example.com/mcp"
) as mcp_server:

    async with Agent(
        client=chat_client,
        tools=mcp_server
    ) as agent:
        result = await agent.run("Current market status?")
```

## Popular MCP Servers

| Server | Command | Purpose |
|--------|---------|---------|
| GitHub | `npx @modelcontextprotocol/server-github` | Repository access |
| Filesystem | `npx @modelcontextprotocol/server-filesystem` | File operations |
| SQLite | `npx @modelcontextprotocol/server-sqlite` | Database access |
| Calculator | `uvx mcp-server-calculator` | Math operations |

## Exposing Agent as MCP Server

Make your agent available to any MCP client:

```csharp
using Microsoft.Extensions.Hosting;
using ModelContextProtocol.Server;

// Create the agent
AIAgent agent = chatClient.AsAIAgent(
    instructions: "You are good at telling jokes.",
    name: "Joker");

// Convert to MCP tool
McpServerTool tool = McpServerTool.Create(agent.AsAIFunction());

// Set up MCP server
HostApplicationBuilder builder = Host.CreateEmptyApplicationBuilder(settings: null);
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithTools([tool]);

await builder.Build().RunAsync();
```

### Required Packages

```bash
dotnet add package Microsoft.Extensions.Hosting --prerelease
dotnet add package ModelContextProtocol --prerelease
```

## Authentication

### API Key

```csharp
// Python example
auth_headers = {
    "Authorization": f"Bearer {api_key}",
    # Or: "X-API-Key": api_key
}

http_client = AsyncClient(headers=auth_headers)

async with MCPStreamableHTTPTool(
    name="MCP tool",
    url=mcp_server_url,
    http_client=http_client
) as mcp_tool:
    # Use the tool
```

### Security Considerations

- Review all MCP servers before adding to your application
- Use servers from trusted providers only
- Log all data shared with MCP servers for auditing
- Pass credentials via `tool_resources` at runtime (not persisted)
- See [MCP Security Best Practices](https://modelcontextprotocol.io/specification/draft/basic/security_best_practices)

## MCP Tool with Agent

```csharp
// Full example: Agent with GitHub MCP tools
var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
var deploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME");

// Set up MCP client for GitHub
await using var mcpClient = await McpClientFactory.CreateAsync(new StdioClientTransport(new()
{
    Name = "GitHub",
    Command = "npx",
    Arguments = ["-y", "--verbose", "@modelcontextprotocol/server-github"]
}));

// Get tools
var tools = await mcpClient.ListToolsAsync();

// Create agent
AIAgent agent = new AzureOpenAIClient(new Uri(endpoint), new DefaultAzureCredential())
    .GetChatClient(deploymentName)
    .AsAIAgent(
        instructions: "You answer questions about GitHub repositories only.",
        tools: [.. tools.Cast<AITool>()]);

// Use
Console.WriteLine(await agent.RunAsync("List open issues in dotnet/runtime"));
```

## Combining MCP with Local Tools

```csharp
// Local tool
[Description("Get current time")]
static string GetTime() => DateTime.Now.ToString("HH:mm:ss");

// MCP tools
var mcpTools = await mcpClient.ListToolsAsync();

// Combine
var allTools = mcpTools.Cast<AITool>()
    .Append(AIFunctionFactory.Create(GetTime))
    .ToArray();

AIAgent agent = chatClient.AsAIAgent(
    instructions: "You have access to GitHub and time tools.",
    tools: allTools);
```
