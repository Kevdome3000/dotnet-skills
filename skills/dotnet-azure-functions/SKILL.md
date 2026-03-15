---
name: dotnet-azure-functions
version: "1.0.0"
category: "Web and Cloud"
description: "Build, review, or migrate Azure Functions in .NET with correct execution model, isolated worker setup, bindings, DI, and Durable Functions patterns."
compatibility: "Requires an Azure Functions project or a migration plan for one."
---

# Azure Functions for .NET

## Trigger On

- working on Azure Functions in .NET
- migrating from the in-process model to the isolated worker model
- adding Durable Functions, bindings, or host configuration

## Workflow

1. Prefer the isolated worker model for new work and migrations; the in-process model reaches end of support on November 10, 2026.
2. Detect the real target framework, Functions runtime version, and worker packages before changing code or templates.
3. Use normal .NET DI, middleware, and configuration patterns when the isolated worker model supports them; do not mix in-process guidance into isolated apps.
4. Keep binding setup, trigger behavior, and retry semantics explicit instead of relying on scattered attributes and local emulator assumptions.
5. For Durable Functions, validate orchestration constraints, replay behavior, and typed activity patterns before refactoring.
6. Verify both local execution and deployment packaging because Functions often fail at the seams between host, bindings, and environment config.

## Deliver

- correct Functions project setup for the chosen model
- clear binding and host configuration
- migration-safe guidance when upgrading execution models

## Validate

- execution model guidance is not mixed
- bindings and host settings match the target runtime
- local and deployment behavior are both checked
