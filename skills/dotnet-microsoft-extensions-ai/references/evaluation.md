# Microsoft.Extensions.AI.Evaluation

## Overview

The evaluation libraries simplify assessing AI response quality and safety. Built on `Microsoft.Extensions.AI` abstractions.

## Packages

| Package | Purpose |
|---------|---------|
| `Microsoft.Extensions.AI.Evaluation` | Core abstractions and types |
| `Microsoft.Extensions.AI.Evaluation.Quality` | Quality evaluators (relevance, coherence, completeness) |
| `Microsoft.Extensions.AI.Evaluation.Safety` | Safety evaluators (content harm, protected material) |
| `Microsoft.Extensions.AI.Evaluation.NLP` | NLP-based evaluators (BLEU, F1) - no LLM required |
| `Microsoft.Extensions.AI.Evaluation.Reporting` | Caching, storage, report generation |
| `Microsoft.Extensions.AI.Evaluation.Console` | CLI tool (`dotnet aieval`) |

## Quality Evaluators

Use LLM to evaluate response quality:

```csharp
// Create evaluators
var relevance = new RelevanceEvaluator();
var completeness = new CompletenessEvaluator();
var coherence = new CoherenceEvaluator();
var groundedness = new GroundednessEvaluator();
var fluency = new FluencyEvaluator();

// Agent-focused evaluators
var intentResolution = new IntentResolutionEvaluator();  // How well agent resolves user intent
var taskAdherence = new TaskAdherenceEvaluator();        // How well agent follows instructions
var toolCallAccuracy = new ToolCallAccuracyEvaluator();  // How well agent uses tools
```

### Basic Evaluation

```csharp
using Microsoft.Extensions.AI.Evaluation;
using Microsoft.Extensions.AI.Evaluation.Quality;

var evaluator = new RelevanceEvaluator();

var result = await evaluator.EvaluateAsync(
    query: "What is the capital of France?",
    response: "Paris is the capital of France.",
    chatClient: chatClient);

Console.WriteLine($"Relevance: {result.Score}");  // 0.0 - 1.0
Console.WriteLine($"Reasoning: {result.Reason}");
```

### Multiple Evaluations

```csharp
var evaluators = new IEvaluator[]
{
    new RelevanceEvaluator(),
    new CompletenessEvaluator(),
    new CoherenceEvaluator()
};

var results = await Task.WhenAll(
    evaluators.Select(e => e.EvaluateAsync(query, response, chatClient)));

foreach (var (evaluator, result) in evaluators.Zip(results))
{
    Console.WriteLine($"{evaluator.Name}: {result.Score:F2}");
}
```

## NLP Evaluators

No LLM required - uses traditional NLP techniques:

```csharp
using Microsoft.Extensions.AI.Evaluation.NLP;

// BLEU score - machine translation quality
var bleu = new BLEUEvaluator();
var bleuResult = await bleu.EvaluateAsync(
    response: "The quick brown fox",
    references: ["The fast brown fox", "A quick brown fox"]);

// F1 score - word overlap
var f1 = new F1Evaluator();
var f1Result = await f1.EvaluateAsync(
    response: "Paris is the capital",
    reference: "The capital is Paris");

// GLEU - sentence-level BLEU variant
var gleu = new GLEUEvaluator();
```

## Safety Evaluators

Require Azure AI Foundry Evaluation service:

```csharp
using Microsoft.Extensions.AI.Evaluation.Safety;

// Content harm detection
var hateEvaluator = new HateAndUnfairnessEvaluator();
var violenceEvaluator = new ViolenceEvaluator();
var selfHarmEvaluator = new SelfHarmEvaluator();
var sexualEvaluator = new SexualEvaluator();

// Or use combined evaluator
var contentHarm = new ContentHarmEvaluator();

// Other safety evaluators
var protectedMaterial = new ProtectedMaterialEvaluator();
var codeVulnerability = new CodeVulnerabilityEvaluator();
var indirectAttack = new IndirectAttackEvaluator();
var groundednessPro = new GroundednessProEvaluator();
```

## Custom Evaluator

```csharp
public class CustomEvaluator : IEvaluator
{
    public string Name => "Custom";

    public async Task<EvaluationResult> EvaluateAsync(
        string query,
        string response,
        IChatClient chatClient,
        CancellationToken ct = default)
    {
        // Custom evaluation logic
        var isGood = response.Length > 10;

        return new EvaluationResult
        {
            Score = isGood ? 1.0 : 0.0,
            Reason = isGood ? "Response is detailed" : "Response too short"
        };
    }
}
```

## Test Integration

```csharp
[TestClass]
public class AIResponseTests
{
    private readonly IChatClient _chatClient;
    private readonly RelevanceEvaluator _evaluator;

    [TestMethod]
    public async Task Response_ShouldBeRelevant()
    {
        var response = await _chatClient.GetResponseAsync("What is 2+2?");

        var result = await _evaluator.EvaluateAsync(
            query: "What is 2+2?",
            response: response.Text,
            chatClient: _chatClient);

        Assert.IsTrue(result.Score >= 0.8, $"Low relevance: {result.Reason}");
    }
}
```

## Reporting

```csharp
using Microsoft.Extensions.AI.Evaluation.Reporting;

// Configure reporting with Azure Storage
var options = new ReportingOptions
{
    StorageConnectionString = "...",
    ContainerName = "evaluations"
};

// Generate HTML report
await ReportGenerator.GenerateAsync(results, "report.html");
```

### CLI Tool

```bash
# Install
dotnet tool install --global Microsoft.Extensions.AI.Evaluation.Console

# Generate report
dotnet aieval report --input ./results --output ./report.html

# Manage cached responses
dotnet aieval cache --list
dotnet aieval cache --clear
```

## Response Caching

Evaluation libraries cache LLM responses for faster re-runs:

```csharp
var evaluator = new RelevanceEvaluator(new EvaluatorOptions
{
    UseCache = true,
    CacheDirectory = "./.evaluation-cache"
});
```
