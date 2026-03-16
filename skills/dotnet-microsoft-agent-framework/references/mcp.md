# Model Context Protocol And External Boundaries

## Choose The Right Protocol

| Need | Choose | Why |
|---|---|---|
| Expose tools or contextual data to an agent | MCP | Tool and context transport |
| Let one agent call another remote agent | A2A | Agent-to-agent delegation and discovery |
| Drive a browser or app UI for humans | AG-UI | Streaming UI protocol with state and approvals |

Do not blur these together:

- MCP is about tools and context.
- A2A is about remote agents.
- AG-UI is about human-facing UI integration.

## Using MCP With Agents

Agent Framework can attach remote MCP servers as tools for agents.

Typical pattern:

- create or configure the MCP tool or client
- add the resulting tool set to the agent
- run the agent normally with those tools available

The official docs emphasize these security rules:

- prefer trusted providers over random proxy servers
- review exactly what prompt or tool data is shared with each MCP server
- log or audit data exchanged with third-party MCP services
- pass authentication headers at run time rather than baking them into durable agent definitions

## Headers And Credentials

Custom headers such as bearer tokens should be injected only for the current run through the tool resources or request context that the MCP integration exposes.

Do not:

- persist API keys inside long-lived thread state
- hardcode third-party MCP credentials in source
- assume all MCP servers use the same auth pattern

## Hosted MCP Versus Local Or Remote MCP

- A `ChatClientAgent` can use MCP servers as tools when the underlying service and tool path support it.
- Some hosted providers also surface MCP as a managed tool category.
- Support is provider-specific. Verify the chosen agent type before you assume hosted MCP works.

## Agent As MCP Tool

You can expose an agent itself as an MCP tool so that any MCP client can call it.

```csharp
using Microsoft.Agents.AI;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol.Server;

McpServerTool tool = McpServerTool.Create(agent.AsAIFunction());

HostApplicationBuilder builder = Host.CreateEmptyApplicationBuilder(settings: null);
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithTools([tool]);

await builder.Build().RunAsync();
```

Use this when:

- you want external tools or clients to consume the agent through the MCP ecosystem
- the right abstraction is a callable tool, not a full conversational remote agent

Use A2A instead when the remote system should remain an agent with its own protocol semantics, discovery, and task model.

## Security Checklist

- Review every third-party MCP server before enabling it.
- Keep credentials request-scoped.
- Log what the agent sends to and receives from MCP servers.
- Limit the MCP tool set to the smallest useful subset.
- Treat MCP output as untrusted input before passing it to sensitive tools or downstream systems.
