---
name: dotnet-entity-framework-core
version: "1.0.0"
category: "Data, Distributed, and AI"
description: "Design, tune, or review EF Core data access with proper modeling, migrations, query translation, performance, and lifetime management for modern .NET applications."
compatibility: "Requires EF Core or a migration plan toward it."
---

# Entity Framework Core

## Trigger On

- working on `DbContext`, migrations, model configuration, or EF queries
- reviewing tracking, loading, performance, or transaction behavior
- porting data access from EF6 or custom repositories to EF Core

## Workflow

1. Prefer EF Core for new development unless a documented gap requires EF6 or another data-access strategy.
2. Keep `DbContext` lifetime aligned with the unit of work and avoid hiding it behind unnecessary abstractions that remove query power while preserving complexity.
3. Review query translation, includes, split versus single query tradeoffs, and projection shape to avoid accidental N+1 or over-fetching behavior.
4. Treat migrations as first-class artifacts with reviewable changes, not throwaway generated noise.
5. Be deliberate about concurrency, transactions, and provider-specific behavior because EF Core is cross-provider but not provider-identical.
6. Use tests and query inspection to validate the generated behavior, not just the in-memory mental model.

## Deliver

- EF Core models and queries that match the domain
- safer migrations and lifetime management
- performance-aware data access decisions

## Validate

- query behavior is intentional
- migrations are reviewable and correct
- provider-specific tradeoffs are acknowledged
