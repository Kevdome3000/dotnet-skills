# Preview Status, Support, and Recurring Checks

## Official Support Channels

| Need | Official Source |
|---|---|
| Current docs | Microsoft Learn Agent Framework site |
| Source and release visibility | `microsoft/agent-framework` GitHub repository |
| Questions and discussion | GitHub Discussions |
| Community help | Community and office-hours information in the repo |

## Public Preview Reality

The official overview page still marks Microsoft Agent Framework as public preview.

Treat that as a real engineering constraint:

- expect prerelease packages
- expect API churn
- verify current docs before locking abstractions
- keep production risk and rollout expectations explicit

## What To Verify On Every Non-Trivial Task

- Which provider and SDK are actually in use.
- Whether the chosen feature has documented .NET support or only Python documentation.
- Whether the agent uses in-memory history, service-stored history, or a custom chat store.
- Whether side-effecting tools have approval and audit paths.
- Whether the hosting surface is OpenAI-compatible HTTP, A2A, AG-UI, Azure Functions, or only local development.
- Whether preview packages and auth requirements are documented in the target repo or service.

## Common Failure Modes

- Reusing an `AgentThread` with a different agent or provider configuration.
- Expecting service-managed history from a Chat Completions-style agent.
- Putting too many unrelated tools on one agent instead of using workflows or specialist agents.
- Treating DevUI as a production hosting solution.
- Persisting MCP credentials in durable configuration instead of run-scoped tool resources.
- Forgetting that hosted-thread cleanup may require the provider SDK rather than `AgentThread` itself.

## Documentation Coverage Notes

The official docs set currently spans:

- overview and quick start
- tutorials
- user guide pages for agents, workflows, hosting, MCP, observability, and DevUI
- AG-UI integration pages
- migration guides
- support pages, including FAQ, troubleshooting, and upgrade-guide indexes

Some areas are uneven across languages:

- declarative workflows are documented primarily for Python
- DevUI docs are published mainly for Python, with .NET placeholders
- support upgrade guides are currently Python-focused
- the troubleshooting page currently contains only a small starter set of issues and is still being reworked

## Support Page Signals

The live Learn support pages currently reinforce these practical checks:

- FAQ confirms the main supported languages are .NET and Python.
- Troubleshooting currently starts with authentication and package-version checks.
- Upgrade guides currently point mainly to Python migration notes, so treat them as churn signals rather than direct .NET API guidance.

Use those pages as concept and roadmap signals, but do not present them as guaranteed .NET APIs when the .NET docs do not actually publish them.

## Practical Refresh Rule

When the framework changes, re-check at least:

- the overview page
- agent types and running-agents docs
- workflows overview and requests-and-responses docs
- hosting docs
- AG-UI docs if you have a frontend protocol surface
- migration and support pages for churn signals
