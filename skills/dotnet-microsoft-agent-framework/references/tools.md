# Tools and Tool Approval

## Tool Support Is Agent-Specific

The base `AIAgent` abstraction does not guarantee tool support. Tooling depends on the specific agent type and the underlying service.

For .NET, the common default is a `ChatClientAgent`, which supports:

- custom function tools that you provide
- service-provided built-in tools when the underlying service exposes them
- per-agent and per-run tool injection

## Function Tools

Use `AIFunctionFactory.Create` to expose plain .NET methods.

```csharp
using System.ComponentModel;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

[Description("Get the weather for a location.")]
static string GetWeather([Description("City or region.")] string location)
    => $"Weather in {location}: cloudy and 15C";

AIAgent agent = chatClient.AsAIAgent(
    instructions: "You are a helpful assistant.",
    tools: [AIFunctionFactory.Create(GetWeather)]);
```

Guidance:

- Add `Description` metadata to both the method and parameters.
- Keep tool contracts narrow and deterministic.
- Put side effects behind clearly named tools so approval or auditing is easy.

## Per-Run Tools

`ChatClientAgent` can accept tools per invocation by merging `ChatOptions` into `ChatClientAgentRunOptions`.

```csharp
var chatOptions = new ChatOptions
{
    Tools = [AIFunctionFactory.Create(GetWeather)]
};

var options = new ChatClientAgentRunOptions(chatOptions);
AgentResponse response = await agent.RunAsync(
    "What is the weather like in Amsterdam?",
    options: options);
```

Use per-run tools when:

- a tool should be available only for a single request
- tool access depends on the current user or tenant
- you need to attach temporary credentials or request-specific behavior

## Service-Provided Tools

Some agent backends expose service-native tools. These are provider-specific `AITool` implementations rather than plain functions.

Typical categories surfaced by the official docs are:

- code interpreter
- file search
- web search
- hosted MCP tools

Example for a hosted service tool:

```csharp
var agent = await persistentAgentsClient.CreateAIAgentAsync(
    deploymentName,
    instructions: "You are a helpful assistant.",
    tools: [new CodeInterpreterToolDefinition()]);
```

Do not assume these tools exist for every agent type. Check the provider and service before depending on them.

## Tool Approval And Human-In-The-Loop

Approval support is not uniform across all providers.

Use approval for:

- money movement
- data deletion or mutation
- external side effects
- third-party calls that can exfiltrate data

If the chosen agent backend does not give you built-in approval semantics, model approval explicitly with:

- workflow request and response handling
- middleware that blocks or rewrites calls
- a dedicated approval tool that returns a denial unless a human explicitly authorizes the action

## Agent As A Tool

Wrap a specialist agent as a callable tool when you want one agent to delegate bounded work to another.

```csharp
AIAgent weatherAgent = chatClient.AsAIAgent(
    name: "WeatherAgent",
    description: "Answers weather questions.",
    instructions: "You answer questions about weather.",
    tools: [AIFunctionFactory.Create(GetWeather)]);

AIAgent coordinator = chatClient.AsAIAgent(
    instructions: "Delegate weather questions when needed.",
    tools: [weatherAgent.AsAIFunction()]);
```

Choose agent-as-tool when:

- the delegated task has a clear boundary
- the caller should stay in control
- you do not need a full workflow graph

Choose a workflow instead when the handoff or retry logic must be explicit and inspectable.

## Tool Guardrails

- Start with the minimum useful tool set.
- Avoid putting dozens of unrelated tools on one agent; split into workflows or specialist agents.
- Log tool name, arguments, result shape, and approval outcome.
- Keep secrets out of static tool registration when they should be injected per request.
- Treat tool outputs as untrusted input, especially when they come from MCP servers or remote systems.
