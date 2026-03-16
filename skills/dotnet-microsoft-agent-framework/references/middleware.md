# Middleware

## Middleware Layers

| Layer | Intercepts | Best For |
|---|---|---|
| Agent run middleware | Whole agent runs and agent outputs | input rewriting, audit spans, response shaping |
| Function-calling middleware | Tool invocations inside the agent loop | approvals, argument validation, result filtering |
| `IChatClient` middleware | Raw model calls for `ChatClientAgent`-style agents | logging, transport customization, model-call policy |

## Registering Middleware

```csharp
var agentWithMiddleware = originalAgent
    .AsBuilder()
    .Use(runFunc: CustomRunMiddleware, runStreamingFunc: CustomRunStreamingMiddleware)
    .Use(CustomFunctionMiddleware)
    .Build();

var chatClientWithMiddleware = chatClient
    .AsBuilder()
    .Use(getResponseFunc: CustomChatClientMiddleware, getStreamingResponseFunc: null)
    .Build();
```

You can also register `IChatClient` middleware through the `clientFactory` argument when creating an agent from a provider helper.

## Agent Run Middleware

Use run middleware when you need to inspect or modify:

- incoming messages
- thread usage
- high-level run options
- final `AgentResponse` or streamed updates

Important rule:

- If you provide only non-streaming middleware, streaming invocations can be forced through non-streaming behavior to satisfy middleware expectations.
- Prefer supplying both `runFunc` and `runStreamingFunc`, or use the shared overload when you only need pre-run behavior.

## Function-Calling Middleware

Function middleware is the right place for:

- approval checks
- argument sanitization
- side-effect blocking
- result normalization

It is currently tied to agent paths that use function-invoking chat clients, such as `ChatClientAgent`.

You can terminate a function loop by setting `FunctionInvocationContext.Terminate = true`, but use that carefully:

- it can stop remaining function calls in the same loop
- it can leave the thread with function-call content but no matching result content
- that can make the thread unusable for later runs

## `IChatClient` Middleware

Use chat-client middleware when the policy belongs to the model call itself:

- telemetry enrichment
- prompt stamping
- model-level retries or custom transport behavior
- sensitive-data logging controls

Remember that `IChatClient` middleware only sees `ChatClientAgent`-style inference calls. It does not automatically intercept arbitrary provider-hosted agent logic that bypasses `IChatClient`.

## Practical Middleware Patterns

- Logging and audit trails around agent runs and tool calls
- Policy enforcement before sensitive tools execute
- Prompt or message normalization before model calls
- Output filtering before results leave your service
- Correlation IDs and OpenTelemetry context propagation

## Guardrails

- Keep middleware deterministic and easy to test.
- Use middleware for cross-cutting policy, not core business logic.
- Prefer explicit workflow request and response paths when a human decision is a real state transition.
- Be conservative about mutating messages or tool results unless the behavior is documented and covered by tests.
