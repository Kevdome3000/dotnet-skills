---
name: dotnet-entity-framework6
version: "1.0.0"
category: "Data, Distributed, and AI"
description: "Maintain or migrate EF6-based applications with realistic guidance on what to keep, what to modernize, and when EF Core is or is not the right next step."
compatibility: "Requires EF6 or a transition plan from EF6 to EF Core or modern .NET."
---

# Entity Framework 6

## Trigger On

- working in an EF6 codebase
- deciding whether to keep EF6, move to modern .NET, or port to EF Core
- reviewing EDMX, code-first, or legacy ASP.NET/WPF/WinForms data access

## Workflow

1. Treat EF6 as stable and supported but no longer the innovation path; do not promise EF Core-only features to EF6 applications.
2. Decide separately between runtime migration and ORM migration; moving to modern .NET can happen before or without moving to EF Core.
3. Review advanced mapping usage, lazy loading, stored procedures, and designer-driven models before planning a port because these often drive migration cost.
4. Prefer small, validated migration slices rather than big-bang rewrites of the data layer.
5. Use `dotnet-entity-framework-core` only when the app is actually ready to adopt EF Core patterns and provider support.
6. Keep performance and behavior checks close to the real database provider, not only mock or in-memory tests.

## Deliver

- realistic EF6 maintenance or migration guidance
- clear separation between runtime upgrade and ORM upgrade work
- reduced risk for legacy data access changes

## Validate

- migration assumptions are backed by real feature usage
- EF6-only features are identified early
- the proposed path avoids avoidable churn
