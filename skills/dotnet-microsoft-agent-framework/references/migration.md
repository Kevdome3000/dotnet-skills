# Migration Notes

## Semantic Kernel To Agent Framework

For .NET teams, this is the most important migration path.

| Semantic Kernel Pattern | Agent Framework Pattern |
|---|---|
| `Kernel`-centric agent setup | `AIAgent` or `ChatClientAgent` over `IChatClient` |
| Manual provider-specific thread construction | `await agent.GetNewThreadAsync()` |
| `InvokeAsync` / `InvokeStreamingAsync` | `RunAsync` / `RunStreamingAsync` |
| `KernelFunction` plugins and plugin registration | `AIFunctionFactory.Create(...)` during agent creation or per run |
| `KernelArguments` and prompt execution settings | `ChatClientAgentRunOptions` with `ChatOptions` |
| Kernel DI as the center of agent creation | Direct agent or chat-client DI registration |

Key shifts from the official .NET guide:

- namespaces move to `Microsoft.Agents.AI` and `Microsoft.Extensions.AI`
- agent creation is simpler and less `Kernel`-heavy
- the agent creates the thread; the caller no longer chooses provider-specific thread classes
- tool registration is direct and agent-first instead of plugin-first
- non-streaming calls return one `AgentResponse`, not a streaming-shaped result sequence

Important watchouts:

- `AgentThread` cleanup for hosted-service providers is provider-specific and may require the provider SDK
- Responses is the forward-looking model; Assistants-style hosted threads are no longer the main direction of travel
- thread state is opaque and provider-specific, so migration is not just a rename exercise

## AutoGen To Agent Framework

The official AutoGen migration guide is Python-oriented, but the architecture differences are still useful for .NET teams.

Concept mapping:

| AutoGen Concept | Agent Framework Concept |
|---|---|
| Assistant-style agent with tools | `ChatAgent` or, conceptually for .NET, `ChatClientAgent` |
| GraphFlow or team orchestration | Typed `Workflow` graphs with executors and edges |
| Group chat patterns | Group Chat or Magentic orchestrations |
| Event-driven HITL loops | Request and response handling with workflow input ports |
| Checkpointing and runtime recovery | Workflow checkpoints and resume |

Architecture lessons that matter even in .NET:

- Agent Framework prefers typed workflow graphs over opaque orchestration loops.
- Request and response is a first-class workflow boundary.
- Hosted tools and Responses-based patterns are more central than in older AutoGen flows.
- Checkpoints and observability are core design features, not afterthoughts.

Because the AutoGen guide is Python-first, use it to translate concepts and orchestration choices, not to infer .NET API names that are not documented in .NET.

## Migration Checklist

- Re-check whether the new design should be a single agent, a workflow, or a durable orchestration.
- Replace any provider-specific thread construction with `GetNewThreadAsync`.
- Revisit tool registration and approval instead of blindly porting plugin models.
- Re-test streaming and non-streaming flows because response models changed.
- Revisit cleanup logic for service-backed threads.
- Revisit hosting: OpenAI-compatible APIs, A2A, and AG-UI are now explicit protocol layers over agents.
