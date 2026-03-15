# dotnet-skills

A curated catalog of reusable agent skills for modern and legacy .NET development.

The canonical skill catalog lives in [`skills/`](skills/). Each skill has its own folder, a `SKILL.md` file, and an `agents/openai.yaml` file for OpenAI or Codex adapter metadata.

This repository is designed to help an AI coding agent work effectively across the main .NET platforms and frameworks: ASP.NET Core, Blazor, Minimal APIs, MAUI, WPF, Windows Forms, WinUI, Azure Functions, Worker Services, Aspire, Entity Framework, Orleans, ML.NET, Semantic Kernel, Microsoft.Extensions.AI, Microsoft Agent Framework, and the major legacy stacks that still exist in real companies.

## What Is In This Repo

- A canonical `skills/` catalog using clean `dotnet-*` names instead of the older `mcaf-*` prefix.
- Platform skills for the major .NET app models and Microsoft frameworks.
- Architecture and code-review skills that help with solution design, migration, and engineering quality.
- Quality, testing, and tooling skills for analyzers, coverage, mutation testing, architecture rules, and formatting.
- `agents/openai.yaml` metadata for every skill as the repository's OpenAI or Codex adapter layer.
- A contributor guide in [`CONTRIBUTING.md`](CONTRIBUTING.md) that tells library authors how to add their projects and document correct usage.
- A generated catalog flow where release CI builds the machine-readable catalog from skill frontmatter.
- A publishable `.NET` tool package that installs the catalog through `dotnet skills ...`.

## Install As a .NET Tool

The repository is configured to publish a real `.NET` tool:

- package id: `ManagedCode.DotnetSkills.Tool`
- command name: `dotnet-skills`
- CLI shape: `dotnet skills ...`

Install it from NuGet:

```bash
dotnet tool install --global ManagedCode.DotnetSkills.Tool
```

Then use it:

```bash
dotnet skills list
dotnet skills where --agent copilot --scope project
dotnet skills sync
dotnet skills install
dotnet skills install aspire orleans
dotnet skills install aspire --agent anthropic --scope project
dotnet skills install aspire --agent gemini --scope project
```

By default, `dotnet skills list` and `dotnet skills install` try to use the latest `catalog-v*` GitHub release from this repository. If no remote catalog release is available, or the network is unavailable, the tool falls back to the bundled catalog inside the `.nupkg`.

`dotnet skills sync` downloads the selected remote catalog into the local cache first. The cache defaults to `$CODEX_HOME/cache/dotnet-skills` when `CODEX_HOME` is set, or `~/.codex/cache/dotnet-skills` otherwise.

Installed skills still go to `$CODEX_HOME/skills` when `CODEX_HOME` is set, or into `~/.codex/skills` otherwise.

The catalog keeps stable namespaced skill IDs such as `dotnet-aspire`, but the CLI accepts short aliases in commands, so `dotnet skills install aspire` resolves to the `dotnet-aspire` skill.

Agent-aware install targets:

- `--agent codex` with `--scope global`: `~/.codex/skills` or `$CODEX_HOME/skills`
- `--agent codex` with `--scope project`: `<repo>/.codex/skills`
- `--agent claude` or `--agent anthropic` with `--scope global`: skill payloads in `~/.claude/skills` plus generated subagent adapters in `~/.claude/agents`
- `--agent claude` or `--agent anthropic` with `--scope project`: skill payloads in `<repo>/.claude/skills` plus generated subagent adapters in `<repo>/.claude/agents`
- `--agent copilot` with `--scope global`: `~/.copilot/skills`
- `--agent copilot` with `--scope project`: `<repo>/.github/skills`
- `--agent gemini` with `--scope global`: `~/.gemini/skills`
- `--agent gemini` with `--scope project`: `<repo>/.gemini/skills`

Notes:

- Claude Code does not consume the raw skill folders directly the same way Codex, Copilot, and Gemini do, so the tool generates `.claude/agents/<skill>.md` adapters that point Claude at the installed skill payload.
- Gemini CLI also supports the Open Code `./.agents/skills` convention, but this tool uses the vendor-specific `.gemini/skills` path as the default install target.

## Publish The Tool

The repository can publish the tool to NuGet through [`.github/workflows/publish-tool.yml`](/Users/ksemenenko/Developer/dotnet-skills/.github/workflows/publish-tool.yml).

Recommended setup:

1. Configure a NuGet.org Trusted Publishing policy for `managedcode/dotnet-skills`.
2. Point that policy at the `publish-tool.yml` workflow and the `release` GitHub environment.
3. Add the repository secret `NUGET_USER` with the NuGet.org account or organization profile name that owns `ManagedCode.DotnetSkills.Tool`.
4. Publish by creating a GitHub release tagged like `v1.2.3`, or run `workflow_dispatch` with `package_version=1.2.3`.

The workflow derives the NuGet package version from the release tag or the manual `package_version` input, builds the tool, packs it, uploads the `.nupkg` as a workflow artifact, and then pushes it to NuGet.
Installability smoke tests run in CI before publish.

If Trusted Publishing is not configured yet, the same workflow can still use a classic `NUGET_API_KEY` repository secret as a fallback.

Do not cut a new NuGet tool release for every skill-content change. Use a catalog release unless the CLI executable or package metadata actually changed.

## Publish The Catalog

Skill content is versioned separately from the NuGet tool package.

Use [`.github/workflows/publish-catalog.yml`](/Users/ksemenenko/Developer/dotnet-skills/.github/workflows/publish-catalog.yml) when the `skills/` catalog changes but the `dotnet-skills` executable itself does not need a new NuGet release.

That workflow:

1. generates fresh catalog outputs in CI from `skills/*/SKILL.md`
2. creates a release tag named `catalog-v<version>`
3. uploads `dotnet-skills-manifest.json`
4. uploads `dotnet-skills-catalog.zip`

The tool resolves the latest remote catalog by scanning GitHub releases for the newest non-draft `catalog-v*` release. You can also pin a specific catalog release with:

```bash
dotnet skills sync --catalog-version 1.2.3
dotnet skills install --catalog-version 1.2.3
```

Local `dotnet build` and `dotnet pack` for the tool generate a temporary bundled manifest from `skills/*/SKILL.md` into the build output. The canonical checked catalog outputs are still generated in release CI.

Official references:

- [Create a .NET tool](https://learn.microsoft.com/en-us/dotnet/core/tools/global-tools-how-to-create)
- [NuGet Trusted Publishers](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishers)
- [Create a package using MSBuild](https://learn.microsoft.com/en-us/nuget/create-packages/creating-a-package-msbuild)
- [Publish packages with `dotnet nuget push`](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-nuget-push)
- [GitHub REST API for releases](https://docs.github.com/en/rest/releases/releases)

## Skill Layout

Every skill is intentionally small and predictable:

```text
skills/<skill-name>/
├── SKILL.md
└── agents/
    └── openai.yaml
```

Some migrated tooling skills also include a `references/` folder copied from the original material when that extra detail is useful.

## Catalog

The catalog below is generated from `SKILL.md` frontmatter.

- `description` is copied directly from each skill and should stay exact.
- `version` is required for every skill and is shown in the table.
- Release CI generates the machine-readable catalog from skill metadata.
- Run `python3 scripts/generate_catalog.py` locally only if you want a preview of the generated README section.
- Contribution rules live in [`CONTRIBUTING.md`](CONTRIBUTING.md).

<!-- BEGIN GENERATED CATALOG -->

This catalog currently contains **52** skills.

### Core

| Skill | Version | Description | Folder |
| --- | --- | --- | --- |
| `dotnet` | `1.0.0` | Primary entry skill for modern and legacy .NET work. Detect the runtime, language version, app model, test stack, and quality gates; route to the right platform skill; and keep validation aligned with the actual repository. | [`skills/dotnet/`](skills/dotnet/) |
| `dotnet-architecture` | `1.0.0` | Design or review .NET solution architecture across modular monoliths, clean architecture, vertical slices, microservices, DDD, CQRS, and cloud-native boundaries without over-engineering. | [`skills/dotnet-architecture/`](skills/dotnet-architecture/) |
| `dotnet-code-review` | `1.0.0` | Review .NET changes for bugs, regressions, architectural drift, missing tests, incorrect async or disposal behavior, and platform-specific pitfalls before you approve or merge them. | [`skills/dotnet-code-review/`](skills/dotnet-code-review/) |
| `dotnet-microsoft-extensions` | `1.0.0` | Use the Microsoft.Extensions stack correctly across Generic Host, dependency injection, configuration, logging, options, HttpClientFactory, and other shared infrastructure patterns. | [`skills/dotnet-microsoft-extensions/`](skills/dotnet-microsoft-extensions/) |
| `dotnet-project-setup` | `1.0.0` | Create or reorganize .NET solutions with clean project boundaries, repeatable SDK settings, and a maintainable baseline for libraries, apps, tests, CI, and local development. | [`skills/dotnet-project-setup/`](skills/dotnet-project-setup/) |

### Web and Cloud

| Skill | Version | Description | Folder |
| --- | --- | --- | --- |
| `dotnet-aspire` | `1.0.0` | Use .NET Aspire to orchestrate distributed .NET applications locally with service discovery, telemetry, dashboards, and cloud-ready composition for cloud-native development. | [`skills/dotnet-aspire/`](skills/dotnet-aspire/) |
| `dotnet-aspnet-core` | `1.0.0` | Build, debug, modernize, or review ASP.NET Core applications with correct hosting, middleware, security, configuration, logging, and deployment patterns on current .NET. | [`skills/dotnet-aspnet-core/`](skills/dotnet-aspnet-core/) |
| `dotnet-azure-functions` | `1.0.0` | Build, review, or migrate Azure Functions in .NET with correct execution model, isolated worker setup, bindings, DI, and Durable Functions patterns. | [`skills/dotnet-azure-functions/`](skills/dotnet-azure-functions/) |
| `dotnet-blazor` | `1.0.0` | Build and review Blazor applications across server, WebAssembly, web app, and hybrid scenarios with correct component design, state flow, rendering, and hosting choices. | [`skills/dotnet-blazor/`](skills/dotnet-blazor/) |
| `dotnet-grpc` | `1.0.0` | Build or review gRPC services and clients in .NET with correct contract-first design, streaming behavior, transport assumptions, and backend service integration. | [`skills/dotnet-grpc/`](skills/dotnet-grpc/) |
| `dotnet-minimal-apis` | `1.0.0` | Design and implement Minimal APIs in ASP.NET Core using handler-first endpoints, route groups, filters, and lightweight composition suited to modern .NET services. | [`skills/dotnet-minimal-apis/`](skills/dotnet-minimal-apis/) |
| `dotnet-signalr` | `1.0.0` | Implement or review SignalR hubs, streaming, reconnection, transport, and real-time delivery patterns in ASP.NET Core applications. | [`skills/dotnet-signalr/`](skills/dotnet-signalr/) |
| `dotnet-web-api` | `1.0.0` | Build or maintain controller-based ASP.NET Core APIs when the project needs controller conventions, advanced model binding, validation extensions, OData, JsonPatch, or existing API patterns. | [`skills/dotnet-web-api/`](skills/dotnet-web-api/) |
| `dotnet-worker-services` | `1.0.0` | Build long-running .NET background services with `BackgroundService`, Generic Host, graceful shutdown, configuration, logging, and deployment patterns suited to workers and daemons. | [`skills/dotnet-worker-services/`](skills/dotnet-worker-services/) |

### Desktop and Mobile

| Skill | Version | Description | Folder |
| --- | --- | --- | --- |
| `dotnet-maui` | `1.0.0` | Build, review, or migrate .NET MAUI applications across Android, iOS, macOS, and Windows with correct cross-platform UI, platform integration, and native packaging assumptions. | [`skills/dotnet-maui/`](skills/dotnet-maui/) |
| `dotnet-winforms` | `1.0.0` | Build, maintain, or modernize Windows Forms applications with practical guidance on designer-driven UI, event handling, data binding, and migration to modern .NET. | [`skills/dotnet-winforms/`](skills/dotnet-winforms/) |
| `dotnet-winui` | `1.0.0` | Build or review WinUI 3 applications with the Windows App SDK, modern Windows desktop patterns, packaging decisions, and interop boundaries with other .NET stacks. | [`skills/dotnet-winui/`](skills/dotnet-winui/) |
| `dotnet-wpf` | `1.0.0` | Build and modernize WPF applications on .NET with correct XAML, data binding, commands, threading, styling, and Windows desktop migration decisions. | [`skills/dotnet-wpf/`](skills/dotnet-wpf/) |

### Data, Distributed, and AI

| Skill | Version | Description | Folder |
| --- | --- | --- | --- |
| `dotnet-entity-framework-core` | `1.0.0` | Design, tune, or review EF Core data access with proper modeling, migrations, query translation, performance, and lifetime management for modern .NET applications. | [`skills/dotnet-entity-framework-core/`](skills/dotnet-entity-framework-core/) |
| `dotnet-entity-framework6` | `1.0.0` | Maintain or migrate EF6-based applications with realistic guidance on what to keep, what to modernize, and when EF Core is or is not the right next step. | [`skills/dotnet-entity-framework6/`](skills/dotnet-entity-framework6/) |
| `dotnet-microsoft-agent-framework` | `1.0.0` | Build agentic .NET applications with Microsoft Agent Framework using modern agent orchestration, provider abstractions, telemetry, and enterprise integration patterns. | [`skills/dotnet-microsoft-agent-framework/`](skills/dotnet-microsoft-agent-framework/) |
| `dotnet-microsoft-extensions-ai` | `1.0.0` | Use Microsoft.Extensions.AI abstractions such as `IChatClient` and embeddings cleanly in .NET applications, libraries, and provider integrations. | [`skills/dotnet-microsoft-extensions-ai/`](skills/dotnet-microsoft-extensions-ai/) |
| `dotnet-mixed-reality` | `1.0.0` | Work on C# and .NET-adjacent mixed-reality solutions around HoloLens, MRTK, OpenXR, Azure services, and integration boundaries where .NET participates in the stack. | [`skills/dotnet-mixed-reality/`](skills/dotnet-mixed-reality/) |
| `dotnet-mlnet` | `1.0.0` | Use ML.NET to train, evaluate, or integrate machine-learning models into .NET applications with realistic data preparation, inference, and deployment expectations. | [`skills/dotnet-mlnet/`](skills/dotnet-mlnet/) |
| `dotnet-orleans` | `1.0.0` | Build or review distributed .NET applications with Orleans grains, silos, streams, persistence, versioning, and cloud-native hosting patterns. | [`skills/dotnet-orleans/`](skills/dotnet-orleans/) |
| `dotnet-semantic-kernel` | `1.0.0` | Build AI-enabled .NET applications with Semantic Kernel using services, plugins, prompts, and function-calling patterns that remain testable and maintainable. | [`skills/dotnet-semantic-kernel/`](skills/dotnet-semantic-kernel/) |

### Legacy and Compatibility

| Skill | Version | Description | Folder |
| --- | --- | --- | --- |
| `dotnet-legacy-aspnet` | `1.0.0` | Maintain classic ASP.NET applications on .NET Framework, including Web Forms, older MVC, and legacy hosting patterns, while planning realistic modernization boundaries. | [`skills/dotnet-legacy-aspnet/`](skills/dotnet-legacy-aspnet/) |
| `dotnet-wcf` | `1.0.0` | Work on WCF services, clients, bindings, contracts, and migration decisions for SOAP and multi-transport service-oriented systems on .NET Framework or compatible stacks. | [`skills/dotnet-wcf/`](skills/dotnet-wcf/) |
| `dotnet-workflow-foundation` | `1.0.0` | Maintain or assess Workflow Foundation-based solutions on .NET Framework, especially where long-lived process logic or legacy designer artifacts still matter. | [`skills/dotnet-workflow-foundation/`](skills/dotnet-workflow-foundation/) |

### Quality, Testing, and Tooling

| Skill | Version | Description | Folder |
| --- | --- | --- | --- |
| `dotnet-analyzer-config` | `1.0.0` | Use a repo-root `.editorconfig` to configure free .NET analyzer and style rules. Use when a .NET repo needs rule severity, code-style options, section layout, or analyzer ownership made explicit. Nested `.editorconfig` files are allowed when they serve a clear subtree-specific purpose. | [`skills/dotnet-analyzer-config/`](skills/dotnet-analyzer-config/) |
| `dotnet-archunitnet` | `1.0.0` | Use the open-source free `ArchUnitNET` library for architecture rules in .NET tests. Use when a repo needs richer architecture assertions than lightweight fluent rule libraries usually provide. | [`skills/dotnet-archunitnet/`](skills/dotnet-archunitnet/) |
| `dotnet-cloc` | `1.0.0` | Use the open-source free `cloc` tool for line-count, language-mix, and diff statistics in .NET repositories. Use when a repo needs C# and solution footprint metrics, branch-to-branch LOC comparison, or repeatable code-size reporting in local workflows and CI. | [`skills/dotnet-cloc/`](skills/dotnet-cloc/) |
| `dotnet-code-analysis` | `1.0.0` | Use the free built-in .NET SDK analyzers and analysis levels. Use when a .NET repo needs first-party code analysis, `EnableNETAnalyzers`, `AnalysisLevel`, or warning policy wired into build and CI. | [`skills/dotnet-code-analysis/`](skills/dotnet-code-analysis/) |
| `dotnet-codeql` | `1.0.0` | Use the open-source CodeQL ecosystem for .NET security analysis. Use when a repo needs CodeQL query packs, CLI-based analysis on open source codebases, or GitHub Action setup with explicit licensing caveats for private repositories. | [`skills/dotnet-codeql/`](skills/dotnet-codeql/) |
| `dotnet-complexity` | `1.0.0` | Use free built-in .NET maintainability analyzers and code metrics configuration to find overly complex methods and coupled code. Use when a repo needs cyclomatic complexity checks, maintainability thresholds, or complexity-driven refactoring gates. | [`skills/dotnet-complexity/`](skills/dotnet-complexity/) |
| `dotnet-coverlet` | `1.0.0` | Use the open-source free `coverlet` toolchain for .NET code coverage. Use when a repo needs line and branch coverage, collector versus MSBuild driver selection, or CI-safe coverage commands. | [`skills/dotnet-coverlet/`](skills/dotnet-coverlet/) |
| `dotnet-csharpier` | `1.0.0` | Use the open-source free `CSharpier` formatter for C# and XML. Use when a .NET repo intentionally wants one opinionated formatter instead of a highly configurable `dotnet format`-driven style model. | [`skills/dotnet-csharpier/`](skills/dotnet-csharpier/) |
| `dotnet-format` | `1.0.0` | Use the free first-party `dotnet format` CLI for .NET formatting and analyzer fixes. Use when a .NET repo needs formatting commands, `--verify-no-changes` CI checks, or `.editorconfig`-driven code style enforcement. | [`skills/dotnet-format/`](skills/dotnet-format/) |
| `dotnet-meziantou-analyzer` | `1.0.0` | Use the open-source free `Meziantou.Analyzer` package for design, usage, security, performance, and style rules in .NET. Use when a repo wants broader analyzer coverage with a single NuGet package. | [`skills/dotnet-meziantou-analyzer/`](skills/dotnet-meziantou-analyzer/) |
| `dotnet-modern-csharp` | `1.0.0` | Write modern, version-aware C# for .NET repositories. Use when choosing language features across C# versions, especially C# 13 and C# 14, while staying compatible with the repo's target framework and `LangVersion`. | [`skills/dotnet-modern-csharp/`](skills/dotnet-modern-csharp/) |
| `dotnet-mstest` | `1.0.0` | Write, run, or repair .NET tests that use MSTest. Use when a repo uses `MSTest.Sdk`, `MSTest`, `[TestClass]`, `[TestMethod]`, `DataRow`, or Microsoft.Testing.Platform-based MSTest execution. | [`skills/dotnet-mstest/`](skills/dotnet-mstest/) |
| `dotnet-netarchtest` | `1.0.0` | Use the open-source free `NetArchTest.Rules` library for architecture rules in .NET unit tests. Use when a repo wants lightweight, fluent architecture assertions for namespaces, dependencies, or layering. | [`skills/dotnet-netarchtest/`](skills/dotnet-netarchtest/) |
| `dotnet-profiling` | `1.0.0` | Use the free official .NET diagnostics CLI tools for profiling and runtime investigation in .NET repositories. Use when a repo needs CPU tracing, live counters, GC and allocation investigation, exception or contention tracing, heap snapshots, or startup diagnostics without GUI-only tooling. | [`skills/dotnet-profiling/`](skills/dotnet-profiling/) |
| `dotnet-quality-ci` | `1.0.0` | Set up or refine open-source .NET code-quality gates for CI: formatting, `.editorconfig`, SDK analyzers, third-party analyzers, coverage, mutation testing, architecture tests, and security scanning. Use when a .NET repo needs an explicit quality stack in `AGENTS.md`, docs, or pipeline YAML. | [`skills/dotnet-quality-ci/`](skills/dotnet-quality-ci/) |
| `dotnet-quickdup` | `1.0.0` | Use the open-source free `QuickDup` clone detector for .NET repositories. Use when a repo needs duplicate C# code discovery, structural clone detection, DRY refactoring candidates, or repeatable duplication scans in local workflows and CI. | [`skills/dotnet-quickdup/`](skills/dotnet-quickdup/) |
| `dotnet-reportgenerator` | `1.0.0` | Use the open-source free `ReportGenerator` tool for turning .NET coverage outputs into HTML, Markdown, Cobertura, badges, and merged reports. Use when raw coverage files are not readable enough for CI or human review. | [`skills/dotnet-reportgenerator/`](skills/dotnet-reportgenerator/) |
| `dotnet-resharper-clt` | `1.0.0` | Use the free official JetBrains ReSharper Command Line Tools for .NET repositories. Use when a repo wants powerful `jb inspectcode` inspections, `jb cleanupcode` cleanup profiles, solution-level `.DotSettings` enforcement, or a stronger CLI quality gate for C# than the default SDK analyzers alone. | [`skills/dotnet-resharper-clt/`](skills/dotnet-resharper-clt/) |
| `dotnet-roslynator` | `1.0.0` | Use the open-source free `Roslynator` analyzer packages and optional CLI for .NET. Use when a repo wants broad C# static analysis, auto-fix flows, dead-code detection, optional CLI checks, or extra rules beyond the SDK analyzers. | [`skills/dotnet-roslynator/`](skills/dotnet-roslynator/) |
| `dotnet-stryker` | `1.0.0` | Use the open-source free `Stryker.NET` mutation testing tool for .NET. Use when a repo needs to measure whether tests actually catch faults, especially in critical libraries or domains. | [`skills/dotnet-stryker/`](skills/dotnet-stryker/) |
| `dotnet-stylecop-analyzers` | `1.0.0` | Use the open-source free `StyleCop.Analyzers` package for naming, layout, documentation, and style rules in .NET projects. Use when a repo wants stricter style conventions than the SDK analyzers alone provide. | [`skills/dotnet-stylecop-analyzers/`](skills/dotnet-stylecop-analyzers/) |
| `dotnet-tunit` | `1.0.0` | Write, run, or repair .NET tests that use TUnit. Use when a repo uses `TUnit`, `[Test]`, `[Arguments]`, source-generated test projects, or Microsoft.Testing.Platform-based execution. | [`skills/dotnet-tunit/`](skills/dotnet-tunit/) |
| `dotnet-xunit` | `1.0.0` | Write, run, or repair .NET tests that use xUnit. Use when a repo uses `xunit`, `xunit.v3`, `[Fact]`, `[Theory]`, or `xunit.runner.visualstudio`, and you need the right CLI, package, and runner guidance for xUnit on VSTest or Microsoft.Testing.Platform. | [`skills/dotnet-xunit/`](skills/dotnet-xunit/) |

<!-- END GENERATED CATALOG -->

## Contribution Workflow

Contributors should add both knowledge and ownership:

- add your project when it deserves a dedicated skill
- add your upstream repository or documentation to the watch list when changes should trigger refresh issues
- write the skill so another engineer or agent can understand what the library is, why to use it, how to wire it into a real project, and when not to use it

Start with [`CONTRIBUTING.md`](CONTRIBUTING.md).

## Migration From `mcaf-*` Names

The older `skills/mcaf-*` material was treated as source input. The curated catalog is now the lowercase `skills/` tree with clean `dotnet-*` names.

| Old name | New name | Note |
| --- | --- | --- |
| `mcaf-dotnet` | `dotnet` | Migrated into the canonical `skills/` catalog. |
| `mcaf-dotnet-analyzer-config` | `dotnet-analyzer-config` | Migrated into the canonical `skills/` catalog. |
| `mcaf-dotnet-archunitnet` | `dotnet-archunitnet` | Migrated into the canonical `skills/` catalog. |
| `mcaf-dotnet-cloc` | `dotnet-cloc` | Migrated into the canonical `skills/` catalog. |
| `mcaf-dotnet-code-analysis` | `dotnet-code-analysis` | Migrated into the canonical `skills/` catalog. |
| `mcaf-dotnet-codeql` | `dotnet-codeql` | Migrated into the canonical `skills/` catalog. |
| `mcaf-dotnet-complexity` | `dotnet-complexity` | Migrated into the canonical `skills/` catalog. |
| `mcaf-dotnet-coverlet` | `dotnet-coverlet` | Migrated into the canonical `skills/` catalog. |
| `mcaf-dotnet-csharpier` | `dotnet-csharpier` | Migrated into the canonical `skills/` catalog. |
| `mcaf-dotnet-features` | `dotnet-modern-csharp` | Migrated into the canonical `skills/` catalog. |
| `mcaf-dotnet-format` | `dotnet-format` | Migrated into the canonical `skills/` catalog. |
| `mcaf-dotnet-meziantou-analyzer` | `dotnet-meziantou-analyzer` | Migrated into the canonical `skills/` catalog. |
| `mcaf-dotnet-mstest` | `dotnet-mstest` | Migrated into the canonical `skills/` catalog. |
| `mcaf-dotnet-netarchtest` | `dotnet-netarchtest` | Migrated into the canonical `skills/` catalog. |
| `mcaf-dotnet-profiling` | `dotnet-profiling` | Migrated into the canonical `skills/` catalog. |
| `mcaf-dotnet-quality-ci` | `dotnet-quality-ci` | Migrated into the canonical `skills/` catalog. |
| `mcaf-dotnet-quickdup` | `dotnet-quickdup` | Migrated into the canonical `skills/` catalog. |
| `mcaf-dotnet-reportgenerator` | `dotnet-reportgenerator` | Migrated into the canonical `skills/` catalog. |
| `mcaf-dotnet-resharper-clt` | `dotnet-resharper-clt` | Migrated into the canonical `skills/` catalog. |
| `mcaf-dotnet-roslynator` | `dotnet-roslynator` | Migrated into the canonical `skills/` catalog. |
| `mcaf-dotnet-stryker` | `dotnet-stryker` | Migrated into the canonical `skills/` catalog. |
| `mcaf-dotnet-stylecop-analyzers` | `dotnet-stylecop-analyzers` | Migrated into the canonical `skills/` catalog. |
| `mcaf-dotnet-tunit` | `dotnet-tunit` | Migrated into the canonical `skills/` catalog. |
| `mcaf-dotnet-xunit` | `dotnet-xunit` | Migrated into the canonical `skills/` catalog. |
| `mcaf-solid-maintainability` | `dotnet-architecture` | Referenced in legacy skills; now covered by a real skill in `skills/`. |
| `mcaf-architecture-overview` | `dotnet-architecture` | Referenced in legacy skills; now covered by a real skill in `skills/`. |

## Coverage Baseline

The platform and framework list was refreshed against official Microsoft documentation on **2026-03-15**. The goal was to cover the major .NET development surfaces that matter in practice, not only the narrow tooling skills that already existed.

Primary sources used for the catalog design:

- [Build apps with .NET](https://learn.microsoft.com/dotnet/core/apps)
- [ASP.NET Core overview](https://learn.microsoft.com/aspnet/core/overview?view=aspnetcore-10.0)
- [Blazor](https://learn.microsoft.com/aspnet/core/blazor/?view=aspnetcore-10.0)
- [.NET MAUI](https://learn.microsoft.com/dotnet/maui/what-is-maui?view=net-maui-10.0)
- [WPF](https://learn.microsoft.com/dotnet/desktop/wpf/overview/)
- [Windows Forms](https://learn.microsoft.com/dotnet/desktop/winforms/overview/)
- [Entity Framework Core and EF6 comparison](https://learn.microsoft.com/ef/efcore-and-ef6/)
- [.NET Aspire](https://learn.microsoft.com/dotnet/aspire/)
- [Microsoft Orleans](https://learn.microsoft.com/dotnet/orleans/overview)
- [ML.NET](https://learn.microsoft.com/dotnet/machine-learning/overview)
- [Microsoft.Extensions.AI](https://learn.microsoft.com/dotnet/ai/microsoft-extensions-ai)
- [Microsoft Agent Framework](https://learn.microsoft.com/agent-framework/overview/)
- [Azure Functions isolated worker](https://learn.microsoft.com/azure/azure-functions/dotnet-isolated-process-guide)
- [Windows developer platform overview](https://learn.microsoft.com/windows/apps/get-started/)
- [Mixed Reality overview](https://learn.microsoft.com/windows/mixed-reality/discover/mr-learning-overview)
- [.NET architecture guidance](https://learn.microsoft.com/dotnet/architecture/)
- [Semantic Kernel](https://learn.microsoft.com/semantic-kernel/overview/)

## Upstream Automation

The repository now includes a scheduled GitHub Actions workflow that watches upstream framework releases and selected official documentation pages, then opens or refreshes GitHub issues when something changes.

Files:

- [`.github/workflows/publish-catalog.yml`](/Users/ksemenenko/Developer/dotnet-skills/.github/workflows/publish-catalog.yml)
- [`.github/workflows/publish-tool.yml`](/Users/ksemenenko/Developer/dotnet-skills/.github/workflows/publish-tool.yml)
- [`.github/workflows/catalog-check.yml`](/Users/ksemenenko/Developer/dotnet-skills/.github/workflows/catalog-check.yml)
- [`.github/upstream-watch.d/`](/Users/ksemenenko/Developer/dotnet-skills/.github/upstream-watch.d)
- [`.github/workflows/upstream-watch.yml`](/Users/ksemenenko/Developer/dotnet-skills/.github/workflows/upstream-watch.yml)
- [`.github/upstream-watch.json`](/Users/ksemenenko/Developer/dotnet-skills/.github/upstream-watch.json)
- [`.github/upstream-watch-state.json`](/Users/ksemenenko/Developer/dotnet-skills/.github/upstream-watch-state.json)
- [`catalog/skills.json`](/Users/ksemenenko/Developer/dotnet-skills/catalog/skills.json)
- [`tools/DotnetSkills.Tooling/DotnetSkills.Tooling.csproj`](/Users/ksemenenko/Developer/dotnet-skills/tools/DotnetSkills.Tooling/DotnetSkills.Tooling.csproj)
- [`scripts/generate_catalog.py`](/Users/ksemenenko/Developer/dotnet-skills/scripts/generate_catalog.py)
- [`scripts/upstream_watch.py`](/Users/ksemenenko/Developer/dotnet-skills/scripts/upstream_watch.py)

What it does:

- Packs and publishes the installable `ManagedCode.DotnetSkills.Tool` package that exposes `dotnet skills ...`.
- Generates the public README catalog section and the machine-readable `catalog/skills.json` manifest from skill frontmatter.
- Generates release-time catalog outputs from `skills/*/SKILL.md`.
- Runs every day on a schedule and can also be launched manually with `workflow_dispatch`.
- Uses `gh api` for GitHub releases, labels, open-issue lookup, issue creation, and issue refresh.
- Uses `curl` for official documentation URLs and tracks `ETag`, `Last-Modified`, or a fallback body hash.
- Keeps one open issue per watch id, so repeat changes update the existing automation issue instead of spamming duplicates.
- Commits the refreshed state file back to the repository so the next run compares against a real baseline.

How the configuration stays maintainable:

- human-edited watch definitions live in small fragment files under [`.github/upstream-watch.d/`](/Users/ksemenenko/Developer/dotnet-skills/.github/upstream-watch.d)
- [`.github/upstream-watch.json`](/Users/ksemenenko/Developer/dotnet-skills/.github/upstream-watch.json) is generated from those fragments by [`scripts/generate_upstream_watch.py`](/Users/ksemenenko/Developer/dotnet-skills/scripts/generate_upstream_watch.py)
- custom libraries should be added to a vendor-specific or domain-specific fragment instead of growing one giant root file
- CI fails if the generated root file is out of sync with the fragments

Bootstrap behavior:

- The first time a watch is seen, the workflow stores its current value in the state file and does **not** create an issue.
- Issues are created only when a later run sees a new release tag or a changed documentation fingerprint.

Current watch coverage includes:

- GitHub releases for .NET SDK, .NET runtime, ASP.NET Core, MAUI, Windows App SDK, EF Core, Orleans, dotnet/extensions, Azure Functions .NET worker, ML.NET, Semantic Kernel, and Microsoft Agent Framework.
- GitHub releases for selected third-party ecosystem projects that matter to this catalog, currently `managedcode/Storage`, `managedcode/Communication`, `managedcode/markitdown`, `managedcode/Orleans.SignalR`, `managedcode/MimeTypes`, and `managedcode/Orleans.Graph`.
- Official Microsoft Learn pages for .NET app models, ASP.NET Core, MAUI, WPF, Windows Forms, EF Core versus EF6, Aspire, Microsoft.Extensions.AI, and Microsoft Agent Framework.

Normal users do not need any local maintenance commands for this automation.

GitHub Actions runs the catalog checks, upstream watch, and tool publishing flow.

To add a new source:

1. Add a new watch entry to the right fragment under [`.github/upstream-watch.d/`](/Users/ksemenenko/Developer/dotnet-skills/.github/upstream-watch.d).
2. Map it to the affected `dotnet-*` skills in the `skills` array.
3. If the repo publishes mixed-language or mixed-package releases, add `match_tag_regex` so only the .NET-relevant release stream opens issues.
4. Regenerate [`.github/upstream-watch.json`](/Users/ksemenenko/Developer/dotnet-skills/.github/upstream-watch.json) with `python3 scripts/generate_upstream_watch.py`.

## Notes

- `skills/` is the canonical catalog to use going forward.
- The original `mcaf-*` drafts were migrated into `skills/` and renamed to clean `dotnet-*` skill names.
- There is no duplicate uppercase skill tree anymore; the repository now uses the lowercase `skills/` path as the single source of truth.
