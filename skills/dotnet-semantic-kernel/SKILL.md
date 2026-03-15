---
name: dotnet-semantic-kernel
version: "1.0.0"
category: "Data, Distributed, and AI"
description: "Build AI-enabled .NET applications with Semantic Kernel using services, plugins, prompts, and function-calling patterns that remain testable and maintainable."
compatibility: "Requires Semantic Kernel packages or an AI integration plan in .NET."
---

# Semantic Kernel for .NET

## Trigger On

- adding AI-driven prompts, plugins, or orchestration to a .NET app
- reviewing kernel construction, service registration, or plugin usage
- migrating older Semantic Kernel code to current APIs

## Workflow

1. Model the kernel as composition of services and plugins; keep business logic outside prompt templates when it belongs in code.
2. Use plugins to expose capabilities intentionally, whether for retrieval, action-taking, or controlled automation.
3. Treat prompt and function-calling behavior as part of the contract and validate failure paths, safety, and fallback behavior.
4. Use dependency injection and logging so AI flows remain observable and testable like the rest of the .NET stack.
5. If the task needs multi-agent orchestration and enterprise-grade agent flows, evaluate `dotnet-microsoft-agent-framework` rather than stretching Semantic Kernel past the requirement.
6. Inspect current package and migration guidance before applying older examples because Semantic Kernel APIs have evolved significantly.

## Deliver

- kernel setup with clear service and plugin composition
- AI features that fit naturally into the existing .NET app
- observable and testable function-calling behavior

## Validate

- plugins and prompts are owned intentionally
- AI flows remain debuggable
- older API examples are not copied blindly
