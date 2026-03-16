# Quick-Start And Tutorial Recipes

This reference indexes the official tutorials that informed this skill. Use it when you need the smallest official walkthrough that proves a pattern before you write production code.

## Foundation

| Need | Official Source Path | Why It Matters |
|---|---|---|
| Start from the smallest working setup | `overview/agent-framework-overview.md` | Explains agents vs workflows and the preview-state framing |
| Get a minimal install and first run | `tutorials/quick-start.md` | Fastest path to a basic agent |
| Browse official learning paths | `tutorials/overview.md` | Shows the supported tutorial families |

## Agent Recipes

| Need | Official Source Path | What It Demonstrates |
|---|---|---|
| Create and run a standard agent | `tutorials/agents/run-agent.md` | Basic packages, `AsAIAgent`, streaming, and normal run flow |
| Pass images or multimodal content | `tutorials/agents/images.md` | Content types beyond plain text |
| Add function tools | `tutorials/agents/function-tools.md` | Function tool registration and multi-tool classes |
| Add approval for function tools | `tutorials/agents/function-tools-approvals.md` | Human approval loop for risky tools |
| Produce structured output | `tutorials/agents/structured-output.md` | Typed output instead of plain text |
| Maintain multi-turn conversations | `tutorials/agents/multi-turn-conversation.md` | Thread reuse and contextual follow-up behavior |
| Persist and resume conversations | `tutorials/agents/persisted-conversation.md` | Thread serialization and resume |
| Store chat history outside memory | `tutorials/agents/third-party-chat-history-storage.md` | Custom `ChatMessageStore` patterns |
| Add memory or context providers | `tutorials/agents/memory.md` | Long-term memory via context injection and extraction |
| Add middleware | `tutorials/agents/middleware.md` | Run, function, and chat-client interception |
| Turn an agent into a tool | `tutorials/agents/agent-as-function-tool.md` | Specialist-agent delegation |
| Expose an agent as an MCP tool | `tutorials/agents/agent-as-mcp-tool.md` | Agent-to-tool bridging via MCP |
| Enable observability | `tutorials/agents/enable-observability.md` | OpenTelemetry setup and diagnostics |
| Host a durable agent | `tutorials/agents/create-and-run-durable-agent.md` | Azure Functions durable hosting |
| Orchestrate durable agents | `tutorials/agents/orchestrate-durable-agents.md` | Deterministic multi-agent orchestration on Azure |

## Workflow Recipes

| Need | Official Source Path | What It Demonstrates |
|---|---|---|
| Build a sequential workflow | `tutorials/workflows/simple-sequential-workflow.md` | Ordered execution and stage-by-stage processing |
| Build a concurrent workflow | `tutorials/workflows/simple-concurrent-workflow.md` | Fan-out and aggregation |
| Put agents inside a workflow | `tutorials/workflows/agents-in-workflows.md` | Agent specialization inside typed workflow execution |
| Add branching logic | `tutorials/workflows/workflow-with-branching-logic.md` | Conditional edges and route selection |
| Register factories with a builder | `tutorials/workflows/workflow-builder-with-factories.md` | Workflow construction and state isolation concerns |
| Handle external requests and responses | `tutorials/workflows/requests-and-responses.md` | `InputPort`, request events, and HITL loops |
| Add checkpointing and resume | `tutorials/workflows/checkpointing-and-resuming.md` | Supersteps, recovery, and resumption |

## Hosting, Integration, And Enterprise Recipes

| Need | Official Source Path | What It Demonstrates |
|---|---|---|
| Protect prompts and responses with Purview | `tutorials/plugins/use-purview-with-agent-framework-sdk.md` | Middleware-based data governance |
| Host agents in ASP.NET Core | `user-guide/hosting/index.md` | `AddAIAgent`, `AddWorkflow`, thread stores, and protocol adapters |
| Expose OpenAI-compatible HTTP endpoints | `user-guide/hosting/openai-integration.md` | Responses and Chat Completions hosting |
| Expose A2A endpoints | `user-guide/hosting/agent-to-agent-integration.md` | Agent cards and agent-to-agent interoperability |
| Build a web UI protocol surface | `integrations/ag-ui/index.md` | AG-UI architecture, SSE, approvals, and state |
| Smoke-test locally with a sample app | `user-guide/devui/index.md` | DevUI capabilities and limits |

## Usage Guidance

- Start with the smallest tutorial that proves the behavior you need.
- After a tutorial works, cross-check the matching user-guide page before you design the production abstraction.
- If the official docs are Python-only for a feature, treat them as conceptual guidance for .NET rather than as proof of a shipped .NET API surface.
