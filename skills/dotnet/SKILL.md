---
name: dotnet
version: "1.0.0"
category: "Core"
description: "Primary entry skill for modern and legacy .NET work. Detect the runtime, language version, app model, test stack, and quality gates; route to the right platform skill; and keep validation aligned with the actual repository."
compatibility: "Requires a .NET repository, solution, or project tree."
---

# .NET Entry Skill

## Trigger On

- implementing, debugging, reviewing, or refactoring C# or .NET code
- deciding which .NET skill should own a task
- mixing platform work with tests, analyzers, architecture, or migration decisions

## Workflow

1. Detect the real stack first: target frameworks, language version, SDK level, project SDKs, hosting model, test framework, and analyzer/tooling packages.
2. Route web work through `dotnet-aspnet-core`, `dotnet-minimal-apis`, `dotnet-web-api`, `dotnet-blazor`, `dotnet-signalr`, or `dotnet-grpc` as appropriate.
3. Route client and desktop work through `dotnet-maui`, `dotnet-wpf`, `dotnet-winforms`, or `dotnet-winui`.
4. Route data, distributed, and AI work through `dotnet-entity-framework-core`, `dotnet-entity-framework6`, `dotnet-orleans`, `dotnet-mlnet`, `dotnet-semantic-kernel`, `dotnet-microsoft-extensions-ai`, or `dotnet-microsoft-agent-framework`.
5. Route legacy work through `dotnet-legacy-aspnet`, `dotnet-wcf`, or `dotnet-workflow-foundation` instead of forcing modern patterns into the wrong stack.
6. After any code change, run the repo-defined build, test, and quality pass and keep commands aligned with the actual runner and tool chain.

## Deliver

- the right .NET subskill selection for the task
- repo-compatible code and configuration changes
- validation evidence that matches the project stack

## Validate

- platform assumptions match the project SDK and packages
- runner-specific commands are not mixed incorrectly
- modern features are only used when the repo supports them
