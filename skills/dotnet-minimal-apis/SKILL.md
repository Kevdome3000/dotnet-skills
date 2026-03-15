---
name: dotnet-minimal-apis
version: "1.0.0"
category: "Web and Cloud"
description: "Design and implement Minimal APIs in ASP.NET Core using handler-first endpoints, route groups, filters, and lightweight composition suited to modern .NET services."
compatibility: "Requires an ASP.NET Core application that uses or should adopt Minimal APIs."
---

# Minimal APIs

## Trigger On

- building new HTTP APIs in ASP.NET Core
- refactoring controller-heavy endpoints into simpler handlers
- organizing route groups, endpoint filters, OpenAPI, or auth for Minimal APIs

## Workflow

1. Prefer Minimal APIs for new HTTP APIs when controller-specific extensibility is not required; this matches current ASP.NET Core guidance.
2. Group endpoints by capability, apply auth and filters at the group level, and keep handlers focused on orchestration rather than hidden side effects.
3. Use typed results, validation, and OpenAPI metadata explicitly so the API contract stays clear.
4. Keep business logic out of inline handlers when the endpoint grows beyond simple composition.
5. Use integration tests to verify route behavior, serialization, auth, and error handling.
6. Escalate to `dotnet-web-api` if the project truly needs controller features like advanced model binding, OData, or JsonPatch.

## Deliver

- clean Minimal API endpoint organization
- explicit contracts and route behavior
- tests that protect handler behavior

## Validate

- handlers stay small and compositional
- group-level policies reduce duplication
- controller-only features are not reimplemented poorly
