---
name: dotnet-code-review
version: "1.0.0"
category: "Core"
description: "Review .NET changes for bugs, regressions, architectural drift, missing tests, incorrect async or disposal behavior, and platform-specific pitfalls before you approve or merge them."
compatibility: "Works for application code, libraries, tests, tooling, and infrastructure changes."
---

# .NET Code Review

## Trigger On

- reviewing a pull request or patch in a .NET repository
- checking for behavioral regressions, API misuse, or missing tests
- auditing architectural or framework-specific correctness

## Workflow

1. Prioritize correctness, data loss, concurrency, security, lifecycle, and platform-compatibility issues before style concerns.
2. Check async flows, cancellation propagation, exception handling, disposal, and transient versus singleton lifetime mistakes.
3. Verify tests cover the changed behavior, not only the happy path or refactored implementation details.
4. Inspect framework-specific boundaries such as EF query translation, ASP.NET middleware order, Blazor render state, or MAUI UI-thread access.
5. Call out missing observability, migration risk, or runtime configuration drift when those are part of the change.
6. Keep findings concrete, reproducible, and tied to specific files or behavior.

## Deliver

- ranked review findings with file references
- clear residual risks and test gaps
- brief summary of what changed only after findings

## Validate

- findings describe user-visible or maintainability-impacting risk
- assumptions are stated when repo context is incomplete
- no trivial style nit hides a more serious issue
