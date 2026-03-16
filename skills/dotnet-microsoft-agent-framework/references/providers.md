# Providers, SDKs, and Endpoint Choices

## Default Guidance

- Prefer a `ChatClientAgent` or `AsAIAgent` when you want the simplest .NET composition model.
- Prefer Responses-based agents for new OpenAI-compatible work that needs richer eventing, service-backed history, or background responses.
- Prefer Chat Completions agents for simpler client-managed chat flows or legacy compatibility.
- Use hosted agent services such as Azure AI Foundry Agents only when the managed-service model itself is required.
- Use any `IChatClient` implementation, including local models, when you want provider freedom and can accept the capabilities of that chat client.

## Agent And Provider Matrix

| Backend | Typical .NET Construction | History Model | Best For | Watchouts |
|---|---|---|---|---|
| Any `IChatClient` | `new ChatClientAgent(chatClient, ...)` or `chatClient.AsAIAgent(...)` | Varies | Broadest .NET integration surface and custom providers | Tooling and history depend on the specific chat client |
| Azure OpenAI Chat Completions | `new AzureOpenAIClient(...).GetChatClient(...).AsAIAgent(...)` | In-memory or custom store | Simple chat flows and compatibility | You own conversation persistence |
| Azure OpenAI Responses | `new AzureOpenAIClient(...).GetOpenAIResponseClient(...).AsAIAgent(...)` | Service-backed or in-memory, depending on mode | Recommended default for new OpenAI-style work | Requires preview-era packages and careful provider-mode decisions |
| OpenAI Chat Completions | `new OpenAIClient(...).GetChatClient(...).AsAIAgent(...)` | In-memory or custom store | Stateless or client-managed chat | No service-backed history by default |
| OpenAI Responses | `new OpenAIClient(...).GetOpenAIResponseClient(...).AsAIAgent(...)` | Service-backed or in-memory, depending on mode | New OpenAI-compatible apps, long-running work | Background behavior and history depend on configuration |
| Azure AI Foundry Agents | `PersistentAgentsClient.CreateAIAgentAsync(...)` | Service-stored only | Managed agent resources and built-in service tools | Less control over local thread-storage behavior |
| OpenAI Assistants | Provider-specific assistant client `CreateAIAgentAsync(...)` | Service-stored only | Existing assistant-style hosted workflows | Treat this as an older hosted-thread model, not the default future-facing path |
| Local or custom model via `IChatClient` | `new ChatClientAgent(chatClient, ...)` | Varies | Ollama or other `IChatClient`-backed services | Make sure function calling and multimodal support are actually present |
| A2A remote agent | A2A agent proxy or hosting surface | Remote service-managed | Cross-agent interoperability | This is remote delegation, not a model provider selection |

## SDK And URL Selection

The official docs call out multiple SDK routes for Azure and OpenAI surfaces.

| AI Service | SDK | Package | URL Pattern |
|---|---|---|---|
| Azure AI Foundry Models | OpenAI SDK | `OpenAI` | `https://ai-foundry-<resource>.services.ai.azure.com/openai/v1/` |
| Azure AI Foundry Models | Azure OpenAI SDK | `Azure.AI.OpenAI` | `https://ai-foundry-<resource>.services.ai.azure.com/` |
| Azure AI Foundry Models | Azure AI Inference SDK | `Azure.AI.Inference` | `https://ai-foundry-<resource>.services.ai.azure.com/models` |
| Azure AI Foundry Agents | Persistent Agents SDK | `Azure.AI.Agents.Persistent` | `https://ai-foundry-<resource>.services.ai.azure.com/api/projects/ai-project-<project>` |
| Azure OpenAI | Azure OpenAI SDK | `Azure.AI.OpenAI` | `https://<resource>.openai.azure.com/` |
| Azure OpenAI | OpenAI SDK | `OpenAI` | `https://<resource>.openai.azure.com/openai/v1/` |
| OpenAI | OpenAI SDK | `OpenAI` | Default OpenAI endpoint |

The official docs explicitly recommend the OpenAI SDK where it is a viable fit, especially when you want one client model across OpenAI-style services.

## Responses Versus Chat Completions

| Use Responses When | Use Chat Completions When |
|---|---|
| Building new remote or hosted integrations | Preserving compatibility with existing Chat Completions clients |
| You want service-side conversation management or response tracking | State is entirely client-managed |
| You need background responses or long-running operations | You want the simplest request/response contract |
| You want richer eventing and more future-facing OpenAI-compatible behavior | You do not need service-managed response chains |

## Background Responses

The official .NET docs currently call out background responses only for:

- OpenAI Responses agents
- Azure OpenAI Responses agents

If long-running continuation tokens are a key requirement, start with Responses rather than Chat Completions.

## Custom Chat Clients And Local Models

`ChatClientAgent` is the main escape hatch for:

- Ollama
- in-house providers
- future `IChatClient` adapters

When using a custom or local chat client:

- verify tool calling support before designing around function tools
- verify multimodal support before committing to image or file inputs
- assume you own history persistence unless the client proves otherwise
