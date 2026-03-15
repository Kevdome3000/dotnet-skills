---
name: dotnet-microsoft-extensions-ai
version: "1.0.0"
category: "AI"
description: "Use Microsoft.Extensions.AI abstractions such as `IChatClient` and embeddings cleanly in .NET applications, libraries, and provider integrations."
compatibility: "Requires `Microsoft.Extensions.AI` or a plan to standardize AI provider integration."
---

# Microsoft.Extensions.AI

## Trigger On

- adding provider-agnostic AI abstractions to a .NET app or library
- wrapping or consuming `IChatClient`, embeddings, or middleware
- choosing between low-level abstractions and a fuller agent framework

## Workflow

1. Use `Microsoft.Extensions.AI` when the app or library needs clean provider abstraction, middleware composition, or testability without adopting a larger agent framework.
2. Reference the abstractions package directly only when you truly need the lower-level surface; most applications benefit from the higher-level package and middleware helpers.
3. Keep provider registration, telemetry, caching, and tool invocation explicit in DI so behavior is inspectable.
4. Avoid coupling application code directly to one vendor API when the abstraction already models the needed capability.
5. Use `dotnet-microsoft-agent-framework` when the requirement is agent orchestration rather than just model and embedding abstraction.
6. Validate with realistic provider implementations and mocks so abstraction benefits actually pay off.

## Deliver

- clean provider-agnostic AI integration
- middleware-friendly AI client composition
- testable abstractions instead of vendor lock-in in app code

## Validate

- the abstraction is solving a real portability or integration problem
- DI wiring stays explicit
- agentic requirements are not underspecified as simple chat-client work

## References

- [patterns.md](references/patterns.md) - IChatClient patterns, embedding patterns, provider integration, and testing patterns
- [examples.md](references/examples.md) - Practical AI integration examples including RAG, semantic caching, content moderation, structured output extraction, and API integration
