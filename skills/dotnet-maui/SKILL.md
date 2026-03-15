---
name: dotnet-maui
version: "1.0.0"
category: "Desktop and Mobile"
description: "Build, review, or migrate .NET MAUI applications across Android, iOS, macOS, and Windows with correct cross-platform UI, platform integration, and native packaging assumptions."
compatibility: "Requires a .NET MAUI app or a migration from Xamarin.Forms or platform-specific projects."
---

# .NET MAUI

## Trigger On

- working on cross-platform mobile or desktop UI in .NET MAUI
- integrating device capabilities, navigation, or platform-specific code
- migrating Xamarin.Forms or aligning a shared codebase across targets

## Workflow

1. Confirm the target platforms first because build, packaging, and platform API behavior differ materially across Android, iOS, Mac Catalyst, and Windows.
2. Keep shared UI, shared business logic, and platform-specific code intentionally separated.
3. Use handlers, platform services, and dependency injection deliberately rather than hiding platform conditionals throughout the UI layer.
4. When Windows-specific work dominates, decide whether MAUI is still the right abstraction or whether `dotnet-winui`, `dotnet-wpf`, or `dotnet-winforms` is a better fit.
5. Treat startup, permissions, lifecycle, and threading as platform contracts that need explicit testing.
6. If Aspire integration is in play, verify whether the current tooling path is stable or preview before cementing the workflow.

## Deliver

- shared MAUI code with explicit platform seams
- navigation and lifecycle behavior that fits each target
- a realistic build and deployment path for the chosen platforms

## Validate

- cross-platform reuse is real, not superficial
- platform-specific behavior is isolated and testable
- build assumptions for Mac/iOS and Windows are explicit
