# Agent Framework Middleware

## Overview

Middleware intercepts and modifies agent interactions at various stages. Use for logging, security, error handling, and result transformation.

## Middleware Types

| Type | Purpose | Intercepts |
|------|---------|------------|
| Agent Run | Inspect/modify agent input/output | All agent runs |
| Function Calling | Inspect/modify tool calls | Function invocations |
| IChatClient | Inspect/modify LLM requests | Chat completions |

## Agent Run Middleware

### Basic Middleware

```csharp
async Task<AgentResponse> LoggingMiddleware(
    IEnumerable<ChatMessage> messages,
    AgentSession? session,
    AgentRunOptions? options,
    AIAgent innerAgent,
    CancellationToken ct)
{
    Console.WriteLine($"Input: {messages.Count()} messages");

    var response = await innerAgent.RunAsync(messages, session, options, ct);

    Console.WriteLine($"Output: {response.Messages.Count} messages");
    return response;
}
```

### Streaming Middleware

```csharp
async IAsyncEnumerable<AgentResponseUpdate> StreamingMiddleware(
    IEnumerable<ChatMessage> messages,
    AgentSession? session,
    AgentRunOptions? options,
    AIAgent innerAgent,
    [EnumeratorCancellation] CancellationToken ct)
{
    Console.WriteLine("Streaming started");

    await foreach (var update in innerAgent.RunStreamingAsync(messages, session, options, ct))
    {
        yield return update;
    }

    Console.WriteLine("Streaming completed");
}
```

### Register Middleware

```csharp
var middlewareAgent = originalAgent
    .AsBuilder()
    .Use(runFunc: LoggingMiddleware, runStreamingFunc: StreamingMiddleware)
    .Build();
```

## Function Calling Middleware

Intercept tool invocations:

```csharp
async ValueTask<object?> FunctionMiddleware(
    AIAgent agent,
    FunctionInvocationContext context,
    Func<FunctionInvocationContext, CancellationToken, ValueTask<object?>> next,
    CancellationToken ct)
{
    Console.WriteLine($"Calling: {context.Function.Name}");

    var result = await next(context, ct);

    Console.WriteLine($"Result: {result}");
    return result;
}

// Register
var agent = originalAgent
    .AsBuilder()
    .Use(FunctionMiddleware)
    .Build();
```

### Terminate Function Loop

```csharp
async ValueTask<object?> SecurityMiddleware(
    AIAgent agent,
    FunctionInvocationContext context,
    Func<FunctionInvocationContext, CancellationToken, ValueTask<object?>> next,
    CancellationToken ct)
{
    // Block sensitive operations
    if (context.Function.Name == "DeleteFile")
    {
        context.Terminate = true;  // Stop function calling loop
        return "Operation blocked by security policy";
    }

    return await next(context, ct);
}
```

## IChatClient Middleware

Intercept LLM requests:

```csharp
async Task<ChatResponse> ChatMiddleware(
    IEnumerable<ChatMessage> messages,
    ChatOptions? options,
    IChatClient innerClient,
    CancellationToken ct)
{
    // Modify messages before sending
    var modifiedMessages = messages.Prepend(
        new ChatMessage(ChatRole.System, "Always be helpful."));

    var response = await innerClient.GetResponseAsync(modifiedMessages, options, ct);

    // Modify response before returning
    return response;
}

// Register on chat client
var middlewareClient = chatClient
    .AsBuilder()
    .Use(getResponseFunc: ChatMiddleware, getStreamingResponseFunc: null)
    .Build();

var agent = new ChatClientAgent(middlewareClient, instructions: "...");
```

### Via Agent Factory

```csharp
var agent = new AzureOpenAIClient(new Uri(endpoint), new DefaultAzureCredential())
    .GetChatClient(deploymentName)
    .AsAIAgent("You are a helpful assistant.", clientFactory: chatClient => chatClient
        .AsBuilder()
        .Use(getResponseFunc: ChatMiddleware, getStreamingResponseFunc: null)
        .Build());
```

## Security Middleware

```csharp
// Python class-based example
class SecurityAgentMiddleware(AgentMiddleware):
    async def process(
        self,
        context: AgentContext,
        call_next: Callable[[], Awaitable[None]]
    ) -> None:
        last_message = context.messages[-1] if context.messages else None

        if last_message and last_message.text:
            if "password" in last_message.text.lower():
                context.result = AgentResponse(
                    messages=[Message("assistant", ["Request blocked."])]
                )
                return  # Don't call call_next()

        await call_next()
```

## Logging Middleware

```csharp
// Python function-based example
async def timing_middleware(
    context: FunctionInvocationContext,
    next: Callable[[FunctionInvocationContext], Awaitable[None]]
) -> None:
    start = time.time()
    print(f"Calling {context.function.name}")

    await next(context)

    duration = time.time() - start
    print(f"{context.function.name} completed in {duration:.3f}s")
```

## Result Override Middleware

```csharp
// Python streaming override example
async def override_middleware(
    context: AgentContext,
    next: Callable[[AgentContext], Awaitable[None]]
) -> None:
    await next(context)

    if context.result is not None:
        if context.is_streaming:
            async def override_stream():
                for chunk in ["Custom ", "response ", "here."]:
                    yield AgentResponseUpdate(contents=[Content.from_text(text=chunk)])
            context.result = override_stream()
        else:
            context.result = AgentResponse(
                messages=[Message(role="assistant", contents=["Custom response."])]
            )
```

## Middleware Chain Order

Multiple middleware execute in registration order:

```csharp
var agent = originalAgent
    .AsBuilder()
    .Use(AuthMiddleware)      // First in, last out
    .Use(LoggingMiddleware)   // Second
    .Use(CachingMiddleware)   // Third, closest to agent
    .Build();

// Execution: Auth -> Logging -> Caching -> Agent -> Caching -> Logging -> Auth
```

## Middleware Registration Levels

```csharp
// Python: Agent-level vs Run-level
async with Agent(
    client=client,
    middleware=[SecurityMiddleware(), LoggingMiddleware()]  # All runs
) as agent:

    # This run: agent + run middleware
    result = await agent.run(
        "Query",
        middleware=[ExtraMiddleware()]  # This run only
    )

    # This run: agent middleware only
    result = await agent.run("Another query")
```
