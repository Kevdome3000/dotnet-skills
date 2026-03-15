---
name: dotnet-web-api
version: "1.0.0"
category: "Web and Cloud"
description: "Build or maintain controller-based ASP.NET Core APIs when the project needs controller conventions, advanced model binding, validation extensions, OData, JsonPatch, or existing API patterns."
compatibility: "Requires an ASP.NET Core API project that uses or should use controllers."
---

# ASP.NET Core Web API

## Trigger On

- working on controller-based APIs in ASP.NET Core
- needing controller-specific extensibility or conventions
- migrating or reviewing existing API controllers and filters

## Workflow

1. Use controllers when the API needs controller-centric features, not simply because older templates did so.
2. Keep controllers thin: map HTTP concerns to application services or handlers, and avoid embedding data access and business rules directly in actions.
3. Use clear DTO boundaries, explicit validation, and predictable HTTP status behavior.
4. Review authentication and authorization at both controller and endpoint levels so the API surface is not accidentally inconsistent.
5. Keep OpenAPI generation, versioning, and error contract behavior deliberate rather than incidental.
6. Use `dotnet-minimal-apis` for new simple APIs instead of defaulting to controllers out of habit.

## Deliver

- controller APIs with explicit contracts and policies
- reduced controller bloat
- tests or smoke checks for critical API behavior

## Validate

- controller features are actually justified
- actions do not hide business logic and persistence details
- HTTP semantics stay predictable across endpoints
