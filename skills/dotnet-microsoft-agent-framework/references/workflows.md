# Agent Framework Workflows

## Overview

Workflows orchestrate multiple agents in predefined sequences. Unlike agents (LLM-driven, dynamic), workflows have explicit control flow.

## Key Concepts

| Concept | Description |
|---------|-------------|
| **Executors** | Processing units (agents or custom logic) |
| **Edges** | Connections between executors |
| **Events** | Lifecycle and execution observability |
| **Checkpointing** | Save/resume long-running workflows |

## Orchestration Types

| Type | Description | Use Case |
|------|-------------|----------|
| Sequential | Pipeline, each builds on previous | Document review, translation chain |
| Concurrent | Parallel execution | Multi-search, parallel analysis |
| Handoff | Transfer control between agents | Customer support escalation |
| Group Chat | Multi-agent discussion | Brainstorming, debate |

## Sequential Orchestration

Agents process in order, each receiving full conversation history:

```csharp
// Create translation agents
static ChatClientAgent GetTranslationAgent(string language, IChatClient client) =>
    new(client, $"Translate any input to {language}. State the input language first.");

var agents = new[] { "French", "Spanish", "English" }
    .Select(lang => GetTranslationAgent(lang, chatClient));

// Build sequential workflow
var workflow = AgentWorkflowBuilder.BuildSequential(agents);

// Run
var messages = new List<ChatMessage> { new(ChatRole.User, "Hello, world!") };
StreamingRun run = await InProcessExecution.StreamAsync(workflow, messages);
await run.TrySendMessageAsync(new TurnToken(emitEvents: true));

await foreach (WorkflowEvent evt in run.WatchStreamAsync())
{
    if (evt is AgentResponseUpdateEvent e)
        Console.WriteLine($"{e.ExecutorId}: {e.Data}");
    else if (evt is WorkflowOutputEvent)
        break;
}
```

### Output

```
French_Translation: English detected. Bonjour, le monde !
Spanish_Translation: French detected. ¡Hola, mundo!
English_Translation: Spanish detected. Hello, world!
```

## Concurrent Orchestration

Execute multiple agents in parallel:

```csharp
var searchAgents = new[]
{
    chatClient.AsAIAgent("Search academic papers"),
    chatClient.AsAIAgent("Search news articles"),
    chatClient.AsAIAgent("Search social media")
};

var workflow = AgentWorkflowBuilder.BuildConcurrent(searchAgents);

// All agents run simultaneously
var result = await workflow.RunAsync("AI safety research 2024");
```

## Handoff Orchestration

Transfer control between specialized agents:

```csharp
var triageAgent = chatClient.AsAIAgent(
    instructions: "Route to billing, technical, or general support.");

var billingAgent = chatClient.AsAIAgent(
    instructions: "Handle billing and payment issues.");

var technicalAgent = chatClient.AsAIAgent(
    instructions: "Handle technical problems and bugs.");

// Define handoff rules
var workflow = AgentWorkflowBuilder.BuildHandoff(
    entryAgent: triageAgent,
    handoffs: new Dictionary<string, AIAgent>
    {
        ["billing"] = billingAgent,
        ["technical"] = technicalAgent
    });
```

## Group Chat Orchestration

Multi-agent discussion with turn-taking:

```csharp
var developer = chatClient.AsAIAgent(
    name: "Developer",
    instructions: "You are a software developer.");

var reviewer = chatClient.AsAIAgent(
    name: "Reviewer",
    instructions: "You review code for quality and security.");

var manager = chatClient.AsAIAgent(
    name: "Manager",
    instructions: "You make final decisions.");

var workflow = AgentWorkflowBuilder.BuildGroupChat(
    participants: [developer, reviewer, manager],
    maxTurns: 10);
```

## Custom Executor

Mix agents with custom logic:

```csharp
public class SummarizerExecutor : IExecutor
{
    public async Task<ExecutorResult> ExecuteAsync(
        WorkflowContext context,
        CancellationToken ct = default)
    {
        var messages = context.Messages;
        var userCount = messages.Count(m => m.Role == ChatRole.User);
        var assistantCount = messages.Count(m => m.Role == ChatRole.Assistant);

        var summary = new ChatMessage(ChatRole.Assistant,
            $"Summary: {userCount} user messages, {assistantCount} assistant messages");

        return new ExecutorResult(messages.Append(summary).ToList());
    }
}

// Use in workflow
var workflow = AgentWorkflowBuilder.BuildSequential([
    contentAgent,
    new SummarizerExecutor()
]);
```

## Workflow Builder (Graph-Based)

For complex workflows with conditional routing:

```csharp
var builder = new WorkflowBuilder()
    .AddExecutor("entry", triageAgent)
    .AddExecutor("billing", billingAgent)
    .AddExecutor("technical", technicalAgent)
    .AddExecutor("escalate", managerAgent)
    .AddEdge("entry", "billing", when: m => m.Contains("payment"))
    .AddEdge("entry", "technical", when: m => m.Contains("bug"))
    .AddEdge("billing", "escalate", when: m => m.Contains("urgent"))
    .AddEdge("technical", "escalate", when: m => m.Contains("critical"));

var workflow = builder.Build();
```

## Events

Monitor workflow execution:

```csharp
await foreach (var evt in run.WatchStreamAsync())
{
    switch (evt)
    {
        case WorkflowStartEvent start:
            Console.WriteLine("Workflow started");
            break;
        case ExecutorStartEvent execStart:
            Console.WriteLine($"Executor {execStart.ExecutorId} starting");
            break;
        case AgentResponseUpdateEvent update:
            Console.Write(update.Data);
            break;
        case ExecutorCompleteEvent execComplete:
            Console.WriteLine($"Executor {execComplete.ExecutorId} completed");
            break;
        case WorkflowOutputEvent output:
            Console.WriteLine("Workflow completed");
            break;
    }
}
```

## Checkpointing

Save and resume long-running workflows:

```csharp
// Save checkpoint
var checkpoint = await workflow.CreateCheckpointAsync();
var serialized = JsonSerializer.Serialize(checkpoint);
await File.WriteAllTextAsync("checkpoint.json", serialized);

// Restore later
var restored = JsonSerializer.Deserialize<WorkflowCheckpoint>(
    await File.ReadAllTextAsync("checkpoint.json"));
var resumedWorkflow = await workflow.ResumeAsync(restored);
```

## Workflow as Agent

Convert a workflow to an agent for composition:

```csharp
var workflow = AgentWorkflowBuilder.BuildSequential([writer, reviewer]);

// Use workflow as an agent
AIAgent workflowAgent = workflow.AsAgent(
    name: "ContentPipeline",
    description: "Creates and reviews content");

// Can now be used as a tool in another agent
mainAgent.AddTool(workflowAgent.AsAIFunction());
```
