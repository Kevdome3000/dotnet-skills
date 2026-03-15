---
name: dotnet-winui
version: "1.0.0"
category: "Desktop and Mobile"
description: "Build or review WinUI 3 applications with the Windows App SDK, modern Windows desktop patterns, packaging decisions, and interop boundaries with other .NET stacks."
compatibility: "Requires a WinUI 3, Windows App SDK, or MAUI-on-Windows integration scenario."
---

# WinUI 3 and Windows App SDK

## Trigger On

- building native modern Windows desktop UI on WinUI 3
- integrating Windows App SDK features into a .NET app
- deciding between WinUI, WPF, WinForms, and MAUI for Windows work

## Workflow

1. Use WinUI when the product needs a modern Windows-native UI stack and Windows App SDK capabilities, not just because it is newer.
2. Keep packaging, deployment, and app model assumptions explicit because unpackaged and packaged workflows differ materially.
3. Use XAML and MVVM patterns intentionally, with clear boundaries between view code, app services, and Windows-specific integrations.
4. If the app is MAUI on Windows, separate what belongs to MAUI from what belongs to WinUI-specific customization.
5. Review interop, background activation, and Windows capability usage at the boundary instead of scattering it across views.
6. Validate on Windows targets directly because WinUI behavior often depends on runtime environment and deployment model.

## Deliver

- modern Windows UI code with clear platform boundaries
- explicit deployment and packaging assumptions
- cleaner interop between shared and Windows-specific layers

## Validate

- WinUI is chosen for a real product reason
- Windows App SDK dependencies are explicit
- packaging and runtime assumptions are tested
