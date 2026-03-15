# Agent Sessions, Memory & RAG

## Sessions (Conversation State)

`AgentSession` maintains conversation state across agent runs.

### Basic Usage

```csharp
// Create session
AgentSession session = await agent.CreateSessionAsync();

// Multi-turn conversation
var first = await agent.RunAsync("My name is Alice.", session);
var second = await agent.RunAsync("What is my name?", session);  // Agent remembers
```

### Session Contents

| Field | Purpose |
|-------|---------|
| `StateBag` | Arbitrary state container |
| `session_id` (Python) | Local unique identifier |
| `service_session_id` | Remote service conversation ID |

### Restore Existing Session

```csharp
// From existing conversation ID
AgentSession session = await agent.CreateSessionAsync(conversationId);

// Or deserialize
var serialized = agent.SerializeSession(session);
AgentSession resumed = await agent.DeserializeSessionAsync(serialized);
```

## RAG (Retrieval Augmented Generation)

### TextSearchProvider

Built-in RAG context provider:

```csharp
// Search function
static Task<IEnumerable<TextSearchProvider.TextSearchResult>> SearchAsync(
    string query, CancellationToken ct)
{
    var results = new List<TextSearchProvider.TextSearchResult>();

    if (query.Contains("return", StringComparison.OrdinalIgnoreCase))
    {
        results.Add(new()
        {
            SourceName = "Return Policy",
            SourceLink = "https://example.com/returns",
            Text = "Customers may return items within 30 days."
        });
    }

    return Task.FromResult<IEnumerable<TextSearchProvider.TextSearchResult>>(results);
}

// Create agent with RAG
AIAgent agent = chatClient.AsAIAgent(new ChatClientAgentOptions
{
    ChatOptions = new() { Instructions = "Answer using the context provided." },
    AIContextProviders = [new TextSearchProvider(SearchAsync)]
});
```

### TextSearchProvider Options

| Option | Description | Default |
|--------|-------------|---------|
| `SearchTime` | When to search: `BeforeAIInvoke` or on-demand | `BeforeAIInvoke` |
| `FunctionToolName` | Tool name for on-demand mode | "Search" |
| `ContextPrompt` | Prefix for search results | "## Additional Context..." |
| `CitationsPrompt` | Request citations | "Include citations..." |
| `RecentMessageMemoryLimit` | Messages to include in search | 0 (disabled) |

```csharp
var options = new TextSearchProviderOptions
{
    SearchTime = TextSearchProviderOptions.TextSearchBehavior.BeforeAIInvoke,
    RecentMessageMemoryLimit = 6
};
```

### Vector Store RAG (Python)

Using Semantic Kernel collections:

```csharp
// Python example with Azure AI Search
from semantic_kernel.connectors.azure_ai_search import AzureAISearchCollection

collection = AzureAISearchCollection[str, SupportArticle](
    record_type=SupportArticle,
    embedding_generator=OpenAITextEmbedding()
)

# Create search function
search_function = collection.create_search_function(
    function_name="search_knowledge_base",
    description="Search for support articles.",
    search_type="keyword_hybrid",
    string_mapper=lambda x: f"{x.record.title}: {x.record.content}"
)

# Convert to Agent Framework tool
search_tool = search_function.as_agent_framework_tool()

# Create agent with search
agent = chat_client.as_agent(
    instructions="Use search to answer questions.",
    tools=search_tool
)
```

## Context Providers

Custom memory and context injection:

```csharp
// Python example
class UserInfoMemory(BaseContextProvider):
    def __init__(self, client, user_info=None):
        self._chat_client = client
        self.user_info = user_info or UserInfo()

    async def invoking(self, messages, **kwargs) -> Context:
        """Provide context before agent runs."""
        if self.user_info.name:
            return Context(instructions=f"User's name is {self.user_info.name}.")
        return Context(instructions="Ask the user for their name.")

    async def invoked(self, request_messages, response_messages, **kwargs):
        """Extract information after agent runs."""
        # Extract user info from messages
        result = await self._chat_client.get_response(
            messages=request_messages,
            instructions="Extract user's name and age.",
            options={"response_format": UserInfo}
        )
        if result.value.name:
            self.user_info.name = result.value.name

# Use with agent
async with Agent(
    client=client,
    context_providers=[UserInfoMemory(client)]
) as agent:
    await agent.run("My name is Alice")
```

## Chat History Management

### Service-Managed History

Some providers (Assistants API) manage history server-side:

```csharp
// History is automatically managed
AgentSession session = await agent.CreateSessionAsync();
await agent.RunAsync("First message", session);
await agent.RunAsync("Second message", session);  // Server has full history
```

### Local History

For Chat Completion providers:

```csharp
public sealed class ConversationalAgent(IChatClient chatClient)
{
    private readonly List<ChatMessage> _history = [
        new(ChatRole.System, "You are a helpful assistant.")
    ];

    public async Task<string> ChatAsync(string message, CancellationToken ct = default)
    {
        _history.Add(new ChatMessage(ChatRole.User, message));
        var response = await chatClient.GetResponseAsync(_history, cancellationToken: ct);
        _history.Add(new ChatMessage(ChatRole.Assistant, response.Text));
        return response.Text;
    }
}
```

## Background Agent Responses

For long-running operations:

```csharp
// Python example
async with Agent(
    client=client,
    background_mode=True
) as agent:
    # Start background task
    task_id = await agent.start_background("Process large dataset")

    # Check status
    status = await agent.get_background_status(task_id)

    # Get result when ready
    result = await agent.get_background_result(task_id)
```

## Observability

Built-in telemetry for agent operations:

```csharp
// Enable OpenTelemetry
services.AddOpenTelemetry()
    .WithTracing(builder => builder
        .AddSource("Microsoft.Extensions.AI")
        .AddSource("Microsoft.Agents"));

// Or via middleware
var agent = chatClient
    .AsBuilder()
    .UseOpenTelemetry()
    .Build()
    .AsAIAgent("You are helpful.");
```

### Workflow Observability

```csharp
// Watch workflow events
await foreach (var evt in workflow.WatchStreamAsync())
{
    switch (evt)
    {
        case ExecutorStartEvent e:
            logger.LogInformation("Executor {Id} started", e.ExecutorId);
            break;
        case ExecutorCompleteEvent e:
            logger.LogInformation("Executor {Id} completed", e.ExecutorId);
            break;
    }
}
```
