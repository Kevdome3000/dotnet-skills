# Architecture and Agent Types

## Route The Problem Before Choosing APIs

| Need | Choose | Why |
|---|---|---|
| Deterministic, auditable, low-latency logic | Plain .NET code or a non-agent workflow | If you can write the function, do that instead of adding an agent |
| One dynamic decision-maker with a limited tool surface | `AIAgent` or `ChatClientAgent` | Keeps orchestration simple and local |
| Typed multi-step execution with explicit control flow | `Workflow` | Executors, edges, checkpoints, and HITL stay inspectable |
| Long-running Azure serverless execution with persisted state | Durable agents on Azure Functions | Durable Task manages thread persistence and reliable replay |
| Remote interoperability or external clients | Hosting integrations | OpenAI-compatible HTTP, A2A, and AG-UI are protocol adapters over your agent |
| Local interactive debugging only | DevUI | Good for development smoke tests, not production |

## Core Runtime Model

- `AIAgent` is the common abstraction for all agent types.
- `AIAgent` instances are stateless. Long-lived state belongs in `AgentThread`.
- `AgentResponse` and `AgentResponseUpdate` can contain more than final text: tool calls, tool results, reasoning updates, and other content.
- `ChatClientAgent` is the default .NET wrapper for any `Microsoft.Extensions.AI.IChatClient`.
- Workflows are explicit graphs of executors and edges. They are not just prompt chains.

## Agent Type Selection

| Agent Type | Use When | Thread Model | Notes |
|---|---|---|---|
| `ChatClientAgent` over any `IChatClient` | You already have a chat client and want the broadest, simplest path | Varies by underlying service | Best default when you want normal .NET composition and middleware |
| Azure OpenAI or OpenAI Responses agent | You want the forward-looking OpenAI-compatible model, richer events, or background responses | Service-backed, in-memory, or both depending on configuration | Good default for new OpenAI-style apps and remote protocol hosting |
| Azure OpenAI or OpenAI Chat Completions agent | You want simple request/response chat with client-managed history | In-memory or custom store | Good for compatibility and straightforward chat flows |
| Azure AI Foundry Agent | You need hosted agent resources and service-provided thread storage and tools | Service-stored only | Best when the managed service model itself is a requirement |
| OpenAI Assistants agent | You must use the Assistants service specifically | Service-stored only | Treat this as a hosted-thread model, not the default future-facing path |
| Custom `AIAgent` | Built-in wrappers are not enough | You own the model | Only use when `IChatClient`-based or hosted-service agents cannot express the behavior |
| A2A agent proxy | You need to call a remote agent over the A2A protocol | Service-managed by the remote agent | This is agent-to-agent interop, not a local inference provider |

## Conversation History Support By Backend

| Backend | Conversation History Model |
|---|---|
| Azure AI Foundry Agents | Service-stored persistent conversation history |
| OpenAI or Azure OpenAI Responses | Service-stored response chain or in-memory history, depending on mode |
| OpenAI or Azure OpenAI Chat Completions | In-memory history or custom chat store |
| OpenAI Assistants | Service-stored persistent conversation history |
| A2A | Service-stored conversation history on the remote side |

## Durable Agents Are A Hosting Choice, Not The Default

- Use the Azure Functions durable extension when you need persistent threads, week-long execution, or deterministic multi-agent orchestration under failure and replay.
- Durable agents wrap your registered agent as a `DurableAIAgent` inside orchestrations.
- In orchestrations, retrieve agents from the orchestration context instead of manually constructing them inside the orchestration body.
- Keep normal ASP.NET Core or worker applications on standard agents and standard workflows unless durability is a real requirement.

## Practical Defaults For .NET

1. Start with a `ChatClientAgent` or `AsAIAgent` over the provider you already trust.
2. Prefer Responses-based agents when you need service-side conversation management, background responses, or OpenAI-compatible remote APIs.
3. Use Workflows for typed coordination, request and response loops, checkpoints, or multi-agent routing.
4. Add hosting protocols only after the in-process agent or workflow model already works.
