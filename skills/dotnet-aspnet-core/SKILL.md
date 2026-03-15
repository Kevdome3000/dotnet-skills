---
name: dotnet-aspnet-core
version: "1.0.0"
category: "Web and Cloud"
description: "Build, debug, modernize, or review ASP.NET Core applications with correct hosting, middleware, security, configuration, logging, and deployment patterns on current .NET."
compatibility: "Requires an ASP.NET Core project or solution."
---

# ASP.NET Core

## Trigger On

- working on ASP.NET Core apps, services, or middleware
- changing auth, routing, configuration, hosting, or deployment behavior
- deciding between ASP.NET Core sub-stacks such as Blazor, Minimal APIs, or controller APIs

## Workflow

1. Detect the real hosting shape first: top-level `Program`, middleware order, auth model, and endpoint registrations.
2. Keep the HTTP pipeline explicit and review exception handling, HTTPS, forwarded headers, static files, auth, and endpoint mapping in order.
3. Prefer built-in DI, configuration, and logging patterns unless the repo intentionally wraps them.
4. Route specialized UI work to `dotnet-blazor`; route real-time and RPC work to `dotnet-signalr` or `dotnet-grpc`.
5. For new HTTP APIs, consider `dotnet-minimal-apis` first unless controller-specific features are required.
6. Validate with build, tests, and targeted endpoint or integration checks rather than assuming compile success is enough.

## Deliver

- production-credible ASP.NET Core code and config
- a clear request pipeline and hosting story
- verification that matches the affected endpoints and middleware

## Validate

- middleware order is intentional
- security and configuration changes are explicit
- endpoint behavior is covered by tests or smoke checks
