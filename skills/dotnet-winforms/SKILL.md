---
name: dotnet-winforms
version: "1.0.0"
category: "Desktop and Mobile"
description: "Build, maintain, or modernize Windows Forms applications with practical guidance on designer-driven UI, event handling, data binding, and migration to modern .NET."
compatibility: "Requires a Windows Forms project on .NET or .NET Framework."
---

# Windows Forms

## Trigger On

- working on Windows Forms UI, event-driven workflows, or classic LOB applications
- migrating WinForms from .NET Framework to modern .NET
- cleaning up oversized form code or designer coupling

## Workflow

1. Respect the designer-generated boundary and avoid editing generated code directly unless the fix explicitly belongs there.
2. Move business logic, validation, and data access out of forms so event handlers remain orchestration code.
3. Keep control naming, layout, and data-binding behavior predictable; WinForms maintenance cost compounds quickly when the UI surface is ad hoc.
4. When modernizing, choose whether to stay on WinForms with better structure or move only when the product truly needs a different UI stack.
5. Use `dotnet-blazor` hybrid guidance only when the task is genuinely about embedding Razor components into WinForms.
6. Validate runtime behavior on Windows because designer success alone proves very little.

## Deliver

- less brittle form code and event handling
- better separation between UI and business logic
- pragmatic modernization guidance for WinForms-heavy apps

## Validate

- designer files stay stable
- forms are not acting as the application service layer
- Windows-only runtime behavior is tested
