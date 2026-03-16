# DevUI

## What DevUI Is

DevUI is a sample application for running and testing agents and workflows with:

- a web UI
- an OpenAI-compatible local API
- trace viewing
- directory discovery and sample loading

The official docs are explicit: DevUI is for development and debugging, not for production.

## Important .NET Caveat

Current DevUI docs are primarily published for Python, and the .NET sections are largely marked as "coming soon".

For .NET work:

- use DevUI docs as conceptual guidance
- do not invent unsupported .NET APIs from the Python pages
- keep the production architecture on normal hosting surfaces such as ASP.NET Core, A2A, OpenAI hosting, or AG-UI

## What DevUI Gives You

- interactive testing of agents and workflows
- automatic input-shape handling
- sample gallery and local entity discovery
- OpenTelemetry trace viewing
- OpenAI-compatible local `/v1/*` API surface

This makes it useful for:

- quick prompt and tool smoke tests
- validating a workflow input contract
- visually inspecting trace behavior
- trying sample agents before writing your own

## Modes And Auth

The docs describe two modes:

- developer mode: full debug and reload capabilities
- user mode: restricted, simplified surface

Auth is optional and bearer-token based. Even so, the docs still position DevUI as a local-development tool, not a production boundary.

## Safe Usage Rules

- Keep DevUI on localhost by default.
- If you expose it on a network, use auth and a reverse proxy, and still treat it as non-production.
- Do not use DevUI as your real public chat surface.
- Be cautious when agents use side-effecting tools or MCP integrations.

## When To Use DevUI

- local debugging
- sample exploration
- trace inspection
- rapid iteration during agent and workflow design

## When Not To Use DevUI

- production web applications
- long-lived public endpoints
- strong security isolation requirements
- protocol contracts that should be owned by your real app rather than a sample tool
