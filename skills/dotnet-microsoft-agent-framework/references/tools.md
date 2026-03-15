# Agent Framework Tools

## Tool Types

| Tool Type | Description |
|-----------|-------------|
| Function Tools | Custom code agents can call |
| Tool Approval | Human-in-the-loop approval |
| Code Interpreter | Execute code in sandbox |
| File Search | Search uploaded files |
| Web Search | Search the web |
| Hosted MCP Tools | MCP tools hosted by Microsoft Foundry |
| Local MCP Tools | MCP tools running locally |

## Function Tools

### Basic Function Tool

```csharp
[Description("Gets current weather for a location")]
static string GetWeather(
    [Description("City name")] string city,
    [Description("Temperature unit")] string unit = "celsius")
{
    return $"Weather in {city}: 22°{unit[0].ToString().ToUpper()}";
}

// Create agent with tool
AIAgent agent = chatClient.AsAIAgent(
    instructions: "You are a weather assistant.",
    tools: [AIFunctionFactory.Create(GetWeather)]);

var response = await agent.RunAsync("What's the weather in Seattle?");
```

### Tool with DI

```csharp
public class WeatherService(IHttpClientFactory httpFactory)
{
    [Description("Gets weather from API")]
    public async Task<WeatherData> GetWeatherAsync(
        [Description("City name")] string city)
    {
        var client = httpFactory.CreateClient("weather");
        return await client.GetFromJsonAsync<WeatherData>($"/weather/{city}");
    }
}

// Register and use
services.AddSingleton<WeatherService>();
var weatherService = serviceProvider.GetRequiredService<WeatherService>();

AIAgent agent = chatClient.AsAIAgent(
    tools: [AIFunctionFactory.Create(weatherService.GetWeatherAsync)]);
```

### Multiple Tools

```csharp
AIAgent agent = chatClient.AsAIAgent(
    instructions: "You help with travel planning.",
    tools: [
        AIFunctionFactory.Create(GetWeather),
        AIFunctionFactory.Create(SearchFlights),
        AIFunctionFactory.Create(BookHotel),
        AIFunctionFactory.Create(GetRestaurants)
    ]);
```

## Tool Approval (Human-in-the-Loop)

Require approval before executing sensitive tools:

```csharp
// Python example - approval_mode parameter
@tool(approval_mode="always_require")
def transfer_money(
    amount: Annotated[float, Field(description="Amount to transfer")],
    to_account: Annotated[str, Field(description="Target account")]
) -> str:
    return f"Transferred ${amount} to {to_account}"

# Never require approval (use for safe tools only)
@tool(approval_mode="never_require")
def get_balance() -> str:
    return "Balance: $1,234.56"
```

## Agent as Function Tool

Convert an agent to a tool for another agent:

```csharp
// Create inner agent
AIAgent weatherAgent = chatClient.AsAIAgent(
    instructions: "You answer questions about the weather.",
    name: "WeatherAgent",
    description: "An agent that answers questions about the weather.",
    tools: [AIFunctionFactory.Create(GetWeather)]);

// Create main agent with inner agent as tool
AIAgent mainAgent = chatClient.AsAIAgent(
    instructions: "You are a helpful assistant.",
    tools: [weatherAgent.AsAIFunction()]);

// Main agent can now call weather agent as needed
Console.WriteLine(await mainAgent.RunAsync("What is the weather in Amsterdam?"));
```

## Expose Agent as MCP Server

Make your agent available as an MCP tool:

```csharp
using ModelContextProtocol.Server;

// Create the agent
AIAgent agent = chatClient.AsAIAgent(
    instructions: "You are good at telling jokes.",
    name: "Joker");

// Convert to MCP tool
McpServerTool tool = McpServerTool.Create(agent.AsAIFunction());

// Set up MCP server over stdio
HostApplicationBuilder builder = Host.CreateEmptyApplicationBuilder(settings: null);
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithTools([tool]);

await builder.Build().RunAsync();
```

## Provider Support Matrix

| Tool Type | Chat Completion | Responses | Assistants | Foundry | Anthropic |
|-----------|-----------------|-----------|------------|---------|-----------|
| Function Tools | ✅ | ✅ | ✅ | ✅ | ✅ |
| Tool Approval | ❌ | ✅ | ❌ | ✅ | ❌ |
| Code Interpreter | ❌ | ✅ | ✅ | ✅ | ❌ |
| File Search | ❌ | ✅ | ✅ | ✅ | ❌ |
| Web Search | ✅ | ✅ | ❌ | ❌ | ❌ |
| Hosted MCP | ❌ | ✅ | ❌ | ✅ | ✅ |
| Local MCP | ✅ | ✅ | ✅ | ✅ | ✅ |

## Structured Output

Return strongly-typed responses:

```csharp
public record WeatherReport(
    string Location,
    double Temperature,
    string Condition,
    string[] Recommendations);

var response = await chatClient.GetResponseAsync<WeatherReport>(
    messages: [new ChatMessage(ChatRole.User, "What's the weather?")],
    options: new ChatOptions { ResponseFormat = typeof(WeatherReport) });

Console.WriteLine($"Temp: {response.Temperature}°C");
```

### With JSON Schema

```csharp
var options = new ChatOptions
{
    ResponseFormat = ChatResponseFormat.ForJsonSchema(
        JsonSchema.FromType<WeatherReport>(),
        jsonSchemaName: "weather_report")
};
```
