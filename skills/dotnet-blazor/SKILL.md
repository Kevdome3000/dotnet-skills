---
name: dotnet-blazor
version: "1.0.0"
category: "Web and Cloud"
description: "Build and review Blazor applications across server, WebAssembly, web app, and hybrid scenarios with correct component design, state flow, rendering, and hosting choices."
compatibility: "Use for Blazor Web Apps, Blazor Server, Blazor WebAssembly, and Blazor Hybrid scenarios."
---

# Blazor

## Trigger On

- working on Razor components, Blazor render modes, or app hosting models
- modernizing UI state, event handling, forms, or navigation in Blazor
- choosing between web and hybrid Blazor delivery models

## Workflow

1. Identify the hosting model or render mode first; component behavior, latency, and resource access differ materially across server, WebAssembly, and hybrid setups.
2. Keep components focused, parameter-driven, and explicit about state ownership and side effects.
3. Use shared services carefully and avoid hidden singleton state that breaks rendering expectations or testability.
4. Prefer reusable components over page-level duplication, but stop before building an accidental design system in app code.
5. Use `dotnet-maui`, `dotnet-wpf`, or `dotnet-winforms` when the task is actually Blazor Hybrid inside a native host.
6. Validate rendering, event flows, and network behavior with the right mix of component tests and end-to-end checks.

## Deliver

- components that match the actual Blazor hosting model
- clear state ownership and render behavior
- tests or smoke coverage for critical UI paths

## Validate

- render mode assumptions are correct
- components do not hide service or lifecycle problems
- hybrid scenarios use native-host constraints intentionally
