---
name: dotnet-wpf
version: "1.0.0"
category: "Desktop and Mobile"
description: "Build and modernize WPF applications on .NET with correct XAML, data binding, commands, threading, styling, and Windows desktop migration decisions."
compatibility: "Requires a WPF project on .NET or .NET Framework."
---

# WPF

## Trigger On

- working on WPF UI, MVVM, binding, commands, or desktop modernization
- migrating WPF from .NET Framework to .NET
- integrating newer Windows capabilities into a WPF app

## Workflow

1. Treat WPF as Windows-only even when the wider .NET stack is cross-platform.
2. Keep data binding, commands, and view models explicit instead of burying behavior in code-behind.
3. Use styles, templates, and resources deliberately so UI changes remain composable rather than page-specific hacks.
4. Review UI-thread affinity, dispatcher usage, async flows, and collection updates because WPF failures often surface at runtime.
5. For modernization, decide whether the app should stay WPF with targeted upgrades or whether a different Windows stack is justified.
6. Validate both designer-time and runtime behavior when the repo depends heavily on XAML composition.

## Deliver

- cleaner WPF views and view-model boundaries
- safer binding and threading behavior
- migration guidance grounded in actual Windows constraints

## Validate

- binding and command flows are explicit
- code-behind is not carrying hidden business logic
- Windows-only assumptions are acknowledged
