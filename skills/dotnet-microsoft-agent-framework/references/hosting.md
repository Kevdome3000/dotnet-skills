# Hosting and Integration Surfaces

## Core ASP.NET Core Hosting

The base hosting layer is `Microsoft.Agents.AI.Hosting`.

Use it to:

- register agents with dependency injection
- attach tools and thread stores through the hosted builder
- register workflows alongside agents
- convert workflows into `AIAgent` surfaces when protocol adapters need an agent

```csharp
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Extensions.AI;

var builder = WebApplication.CreateBuilder(args);

IChatClient chatClient = new AzureOpenAIClient(new Uri(endpoint), new DefaultAzureCredential())
    .GetChatClient(deploymentName)
    .AsIChatClient();
builder.Services.AddSingleton(chatClient);

var pirateAgent = builder.AddAIAgent(
    "pirate",
    instructions: "You are a pirate. Speak like a pirate.");

var workflow = builder.AddWorkflow("science-workflow", (sp, key) => { /* build workflow */ })
    .AddAsAIAgent();
```

Useful hosted-builder extensions from the official docs:

- `.WithAITool(...)`
- `.WithInMemoryThreadStore()`
- `.AddAsAIAgent()` for workflows

## OpenAI-Compatible HTTP Endpoints

Use `Microsoft.Agents.AI.Hosting.OpenAI` when external clients should call your agent through OpenAI-style APIs.

The docs expose two protocol families:

- Chat Completions
- Responses, plus conversation helpers

Key methods called out in the official docs:

- `builder.AddOpenAIChatCompletions()`
- `app.MapOpenAIChatCompletions(agent)`
- `builder.AddOpenAIResponses()`
- `builder.AddOpenAIConversations()`
- `app.MapOpenAIResponses(agent)`
- `app.MapOpenAIConversations()`

Default guidance:

- Prefer Responses for new work.
- Keep Chat Completions for compatibility with older clients or simpler stateless integrations.

## A2A Hosting

Use `Microsoft.Agents.AI.Hosting.A2A` and `Microsoft.Agents.AI.Hosting.A2A.AspNetCore` when your surface is another agent, not a generic HTTP client.

The official hosting guide uses:

```csharp
app.MapA2A(agent, "/a2a/my-agent", agentCard: new()
{
    Name = "My Agent",
    Description = "A helpful agent.",
    Version = "1.0"
});
```

A2A gives you:

- agent discovery through agent cards
- message-based interop
- long-running task semantics
- cross-framework interoperability

## AG-UI

Use `Microsoft.Agents.AI.Hosting.AGUI.AspNetCore` when the problem is an interactive web or mobile agent UI.

The official docs emphasize:

- Server-Sent Events streaming
- thread or conversation management
- backend tool rendering
- human approval flows
- shared or predictive state updates

Core .NET entry point:

```csharp
app.MapAGUI("/", agent);
```

AG-UI is a protocol adapter for humans and frontends. It is not the same thing as A2A and it is not just MCP over HTTP.

## Azure Functions Durable Hosting

Use `Microsoft.Agents.AI.Hosting.AzureFunctions` when you need:

- serverless hosting
- durable thread persistence
- deterministic multi-agent orchestrations
- long-running flows that can survive failures and restarts

The official docs use:

```csharp
using IHost app = FunctionsApplication
    .CreateBuilder(args)
    .ConfigureFunctionsWebApplication()
    .ConfigureDurableAgents(options => options.AddAIAgent(agent))
    .Build();
```

In durable orchestrations:

- retrieve agents from the orchestration context
- let Durable Task own persistence and replay
- keep orchestration logic deterministic

## Purview Integration

For governance and compliance, the official docs include the `Microsoft.Agents.AI.Purview` integration.

Typical .NET shape:

- build the agent
- convert to builder
- add `.WithPurview(...)`
- rebuild the agent

Use this when prompts or responses need policy enforcement, audit, or compliance hooks before the agent is approved for enterprise rollout.

## Practical Rules

- Keep agent logic protocol-agnostic; let hosting layers adapt the protocol.
- Prefer one clear protocol surface per endpoint.
- Wrap workflows as agents only when a hosting layer truly requires an `AIAgent`.
- Keep DevUI separate from production hosting decisions.
