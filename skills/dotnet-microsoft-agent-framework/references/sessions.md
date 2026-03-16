# Threads, Chat History, and Memory

## Core Rule

`AIAgent` instances are stateless. Conversation state and provider-specific state belong in `AgentThread`.

```csharp
AgentThread thread = await agent.GetNewThreadAsync();

AgentResponse first = await agent.RunAsync("My name is Alice.", thread);
AgentResponse second = await agent.RunAsync("What is my name?", thread);
```

If you omit the thread, the agent creates a throwaway thread for that single run only.

## Thread Compatibility Boundaries

- Create threads from the agent itself with `GetNewThreadAsync`.
- Treat threads as opaque provider-specific state.
- Do not reuse a thread created by one agent with a different agent unless you fully understand the underlying service model.
- If you change provider, service mode, or agent configuration, assume old threads are incompatible until proven otherwise.

## Chat History Storage Models

| Model | Typical Backends | What Lives In `AgentThread` |
|---|---|---|
| In-memory | Chat Completions-style backends | Full message history |
| Service-stored | Azure AI Foundry Agents, Assistants, many Responses modes | A service conversation or response-chain identifier |
| Third-party custom store | ChatCompletion-style agents with custom persistence | Store-specific state plus the thread identity |

## In-Memory Chat History And Reducers

When the service does not own history, Agent Framework can keep chat history in memory or in a custom store.

You can attach a reducer to the built-in in-memory store to control prompt growth.

```csharp
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

AIAgent agent = openAIClient.GetChatClient(modelName).AsAIAgent(new ChatClientAgentOptions
{
    Name = "Joker",
    ChatOptions = new() { Instructions = "You are good at telling jokes." },
    ChatMessageStoreFactory = (ctx, ct) => new ValueTask<ChatMessageStore>(
        new InMemoryChatMessageStore(
            new MessageCountingChatReducer(12),
            ctx.SerializedState,
            ctx.JsonSerializerOptions,
            InMemoryChatMessageStore.ChatReducerTriggerEvent.AfterMessageAdded))
});
```

Use reducers when:

- the thread is purely local
- the model context window can be exceeded
- summarization or count-based pruning is acceptable

## Third-Party Chat Stores

For custom persistence, implement `ChatMessageStore` and provide it through `ChatMessageStoreFactory`.

Key rules:

- each thread needs its own unique storage key
- serialization must preserve the store state needed to re-open that thread later
- if the service already owns chat history, your custom store is ignored

## Long-Term Memory Via Context Providers

Use `AIContextProvider` when you need memory that is richer than raw chat history.

Typical pattern:

- `InvokingAsync` injects instructions, messages, or functions before the agent runs
- `InvokedAsync` inspects the completed interaction and extracts memory to persistent storage

Use context providers for:

- user profile and preferences
- retrieval augmentation
- external memory systems
- post-run extraction and enrichment

## Serialize The Whole Thread

Persist the full `AgentThread`, not just messages.

```csharp
JsonElement serialized = thread.Serialize();
AgentThread resumed = await agent.DeserializeThreadAsync(serialized);
```

Why:

- service-backed threads store identifiers, not the message list
- custom stores can attach their own serialized state
- context providers can attach additional state

## Practical Safety Rules

- Always store the whole serialized thread.
- Restore with an agent configured the same way as the original.
- Be careful with service-backed thread lifecycle and cleanup; some providers require deletion through their own SDKs.
- When the architecture changes, consider thread migration explicitly rather than assuming old serialized state still works.
