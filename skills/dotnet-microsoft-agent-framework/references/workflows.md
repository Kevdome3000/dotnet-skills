# Workflows

## Use Workflows When Control Flow Must Be Explicit

Choose Workflows when you need:

- typed execution stages
- explicit sequencing or branching
- multiple agents that coordinate predictably
- checkpoints and resume
- request and response loops for human-in-the-loop or external systems
- shared state and deterministic orchestration

If one dynamic agent with a small tool surface is enough, keep the design simpler and stay with an agent.

## Core Concepts

| Concept | Meaning |
|---|---|
| Executor | A processing node that handles messages |
| Edge | A routing rule between executors |
| Workflow | A graph of executors and edges |
| Superstep | A unit of workflow progress after which checkpoints can be captured |
| `InputPort` | Boundary that lets workflows emit requests and receive responses |
| Shared state | Workflow-wide durable data accessible to executors |
| Checkpoint | Saved workflow state that supports restore or rehydration |
| Workflow as agent | A workflow wrapped so it can be exposed like an `AIAgent` |

## Builders And Composition

- Use `WorkflowBuilder` for graph-shaped flows with explicit edges and message types.
- Use `AgentWorkflowBuilder` when you want built-in orchestration helpers such as sequential or concurrent agent patterns.
- Use workflows to coordinate agents, custom executors, or both.
- Convert a workflow to an agent when a hosting protocol expects an `AIAgent`.

## Orchestration Patterns

| Pattern | Best For | Notes |
|---|---|---|
| Sequential | Pipelines and staged refinement | Each stage builds on the previous result |
| Concurrent | Parallel analysis and fan-out | Aggregate results explicitly |
| Handoff | Dynamic expert routing | Agents pass control based on context |
| Group Chat | Managed multi-agent discussion | Manager controls turn taking |
| Magentic | Planner-led multi-agent collaboration | Good for complex, generalist decomposition |

These are workflow patterns, not vague prompt recipes. Prefer them when the collaboration shape matters to correctness.

## Requests, Responses, And Human-In-The-Loop

Workflows can send requests outside the workflow and pause until a response arrives.

```csharp
var inputPort = InputPort.Create<ApprovalRequest, ApprovalResponse>("approval");

var workflow = new WorkflowBuilder(inputPort)
    .AddEdge(inputPort, reviewerExecutor)
    .AddEdge(reviewerExecutor, inputPort)
    .Build<ApprovalRequest>();
```

Key ideas:

- executors send requests through the workflow context
- the outer host listens for `RequestInfoEvent`
- responses are sent back into the workflow and routed to the waiting executor
- pending requests are preserved in checkpoints

This is the clean way to model approval, escalation, and external callbacks.

## Checkpoints

Checkpoints are created at the end of supersteps.

They capture:

- executor state
- queued messages
- pending requests and responses
- shared states

Custom executors that carry internal state must persist and restore it explicitly during checkpoint save and restore hooks.

## Shared State And State Isolation

- Use shared state only for data that truly belongs to the workflow as a whole.
- Keep executor-local state local and checkpoint it intentionally.
- Treat built workflows as immutable execution definitions.
- Be careful when reusing mutable workflow builders; state isolation matters when workflows are created through factories or reused in long-lived hosts.

## Workflows As Agents

Wrap a workflow as an agent when:

- you want to expose it through ASP.NET Core hosting
- a higher-level system expects an `AIAgent`
- you want protocol adapters such as OpenAI-compatible endpoints or A2A over a workflow

Do not wrap a workflow as an agent just to hide its structure from yourself. Keep the workflow graph explicit in code and docs.

## Observability And Visualization

- Workflow execution emits events such as executor start, agent response updates, workflow output, and superstep completion.
- Use workflow observability and visualization when diagnosing routing, aggregation, or checkpoint behavior.
- Visual traces are especially important once you introduce concurrent, handoff, or Magentic patterns.

## Declarative Workflows

Official docs currently position declarative workflows as Python-first, with YAML-based workflow definitions, expressions, and action libraries.

For .NET work:

- treat declarative workflow docs as conceptual guidance only
- do not invent a .NET declarative API surface that the docs do not actually publish
- keep .NET implementations programmatic unless official .NET declarative docs and packages are explicitly available
