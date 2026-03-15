---
name: dotnet-microsoft-agent-framework
version: "1.0.0"
category: "AI"
description: "Build agentic .NET applications with Microsoft Agent Framework using modern agent orchestration, provider abstractions, telemetry, and enterprise integration patterns."
compatibility: "Requires a .NET AI application that truly needs agent orchestration."
---

# Microsoft Agent Framework

## Trigger On

- building single-agent or multi-agent orchestration in .NET
- evaluating migration paths from Semantic Kernel agent features or AutoGen patterns
- adding enterprise-grade telemetry, tool use, or A2A and MCP-aware agent workflows

## Workflow

1. Use Agent Framework when the problem is genuinely agentic: orchestration, multi-agent collaboration, handoff, long-running state, or human-in-the-loop control.
2. Build on `Microsoft.Extensions.AI` abstractions intentionally and keep provider and tool wiring explicit.
3. Model orchestration flow directly instead of letting prompts become the hidden state machine.
4. Treat security, observability, and prompt-injection boundaries as first-class engineering concerns.
5. Because the framework is evolving quickly, verify package maturity and preview-versus-stable status in official docs before locking the repo to a version strategy.
6. Use `dotnet-semantic-kernel` only when plugin-and-prompt composition is sufficient and full agent orchestration is unnecessary.

## Deliver

- agent orchestration that matches the product need
- explicit provider, tool, and telemetry wiring
- clear boundaries around state, handoff, and safety behavior

## Validate

- agent complexity is justified
- orchestration logic is inspectable and testable
- version maturity is checked against current docs

## References

- [patterns.md](references/patterns.md) - Agent patterns, multi-agent orchestration, provider patterns, telemetry, and error handling
- [examples.md](references/examples.md) - Practical agent implementations including customer support, code review, document processing, RAG, shopping, and workflow automation
