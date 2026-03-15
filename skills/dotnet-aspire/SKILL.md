---
name: dotnet-aspire
version: "1.0.0"
category: "Web and Cloud"
description: "Use .NET Aspire to orchestrate distributed .NET applications locally with service discovery, telemetry, dashboards, and cloud-ready composition for cloud-native development."
compatibility: "Best for distributed apps, microservices, local cloud-native dev loops, and multi-service solutions."
---

# .NET Aspire

## Trigger On

- adding Aspire orchestration to a distributed .NET app
- wiring local dependencies like databases, caches, or brokers
- using the dashboard, service defaults, and local observability for multi-service systems

## Workflow

1. Use Aspire when the repo genuinely has multiple services, infrastructure dependencies, or developer-observability needs; avoid adding it to a simple single-service app without a payoff.
2. Keep `AppHost` responsible for orchestration and resource wiring, not business logic.
3. Use service defaults for shared telemetry and health configuration so distributed behavior is consistent.
4. Model backing services and wait-for dependencies explicitly so local startup order is reproducible.
5. When integrating Orleans, MAUI, or Functions, validate whether the integration is first-class, preview, or still evolving in the current docs.
6. Verify the dashboard, service discovery, and connection strings instead of assuming templates are correct for the repo structure.

## Deliver

- a working local distributed app composition
- shared service defaults and observability wiring
- clear orchestration boundaries between app code and AppHost

## Validate

- Aspire adds real value for the repo complexity
- resource wiring is explicit and reproducible
- service startup and local diagnostics work end to end
