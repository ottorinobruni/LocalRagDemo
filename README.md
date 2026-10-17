# Local RAG and AI Agent in C# with Ollama and Qdrant

A hands-on educational project that starts with a fully local **Retrieval-Augmented Generation (RAG)** application and evolves into a **tool-enabled AI agent** built with **C# / .NET 10**, **Microsoft Agent Framework**, **Ollama**, and **Qdrant**.

The latest tutorial, **AI Agent – Part 2**, adds multiple tools and conversation state. The agent can search Product API documentation, inspect simulated deployment information, retrieve simulated application errors, and handle follow-up questions within a single session.

> **Disclaimer:** This project is for educational purposes. The Product API, deployment status, and application logs are fictional. The deployment and logging tools return hard-coded sample data; they do **not** connect to a live Kubernetes cluster or monitoring service. Additional validation, security, testing, and observability would be required for production use.

## Related Articles

### Local RAG Series

1. [Build a Local RAG Application in C# with Ollama and Qdrant – Part 1](https://www.ottorinobruni.com/build-local-rag-application-csharp-ollama-qdrant-part-1/)
2. [Build a Local RAG Application in C# with Ollama and Qdrant – Part 2](https://www.ottorinobruni.com/build-local-rag-application-csharp-ollama-qdrant-part-2/)
3. [Build a Local RAG Application in C# with Ollama and Qdrant – Part 3](https://www.ottorinobruni.com/build-local-rag-application-csharp-ollama-qdrant-part-3/)
4. [Build a Local RAG Application in C# with Ollama and Qdrant – Part 4](https://www.ottorinobruni.com/build-local-rag-application-csharp-ollama-qdrant-part-4/)

### AI Agent Series

1. [From Local RAG to an AI Agent in C# with Ollama and Qdrant – Part 1](https://www.ottorinobruni.com/from-local-rag-to-ai-agent-csharp-ollama-qdrant-part-1/) – Turn document retrieval into an agent tool.
2. [From Local RAG to an AI Agent in C# with Ollama and Qdrant – Part 2](https://www.ottorinobruni.com/from-local-rag-to-ai-agent-csharp-ollama-qdrant-part-2/) – Add multiple tools and conversation state.

Additional reading: [AI Basics for Developers: Tokens, Inference, Embeddings, Vector Databases and RAG Explained](https://www.ottorinobruni.com/ai-basics-for-developers-tokens-inference-embeddings-vector-databases-rag-explained/).

## How It Works

The original RAG application always searched the document store before generating an answer:

```text
Question → Retrieval → Prompt → Model → Answer
```

In **AI Agent – Part 1**, retrieval became an optional tool requested by the model. In **Part 2**, the agent has three tools, and conversation state preserves context between questions:

```text
                User Question / Follow-up
                           |
                           v
                     AgentSession
                           |
                           v
                  AI Agent + Ollama
                           |
           +---------------+---------------+
           |               |               |
           v               v               v
 SearchDocumentsAsync  GetDeploymentStatus  GetRecentErrors
           |               |               |
           v               v               v
        Qdrant       Simulated deployment  Simulated logs
           |               |               |
           +---------------+---------------+
                           |
                           v
                  AI Agent + Ollama
                           |
                           v
                   Contextual Answer
```

The model can also answer without invoking a tool when one is not needed. The diagram shows **available capabilities**, not a mandatory execution order: the model might call none, one, or multiple tools.

The document ingestion and retrieval pipeline remains unchanged:

```text
Markdown Documents
        |
        v
 IngestionDocument
        |
        v
 Token-based Chunks
        |
        v
 Embeddings (nomic-embed-text)
        |
        v
       Qdrant
        |
        v
 SearchDocumentsAsync (when requested by the agent)
```

## Technology Stack

| Component | Purpose |
| --- | --- |
| C# / .NET 10 | Console application |
| Microsoft Agent Framework (`Microsoft.Agents.AI`) | Agent, tool orchestration, and `AgentSession` |
| `Microsoft.Extensions.AI` | AI abstractions, embedding generation, and function tools |
| `Microsoft.Extensions.VectorData` | Vector-store abstractions |
| `Microsoft.Extensions.DataIngestion` | Document ingestion and chunking |
| `Microsoft.Extensions.DataIngestion.Markdig` | Markdown document reader |
| `Microsoft.ML.Tokenizers` | Token-based chunking |
| `OllamaSharp` | Integration with local Ollama models |
| Ollama / `llama3.2` | Local language model and tool selection |
| Ollama / `nomic-embed-text` | Local embeddings |
| `CommunityToolkit.VectorData.Qdrant` | VectorData integration for Qdrant |
| `Qdrant.Client` / Qdrant | Vector search and storage |

Everything in this example can run locally. **Part 2 does not require additional NuGet libraries beyond those already used in Agent Part 1.**

## What's New in AI Agent – Part 2

- **Three agent tools**, rather than one:
  - `SearchDocumentsAsync` searches the Product API documentation in Qdrant.
  - `GetDeploymentStatus` returns a simulated Kubernetes deployment status.
  - `GetRecentErrors` returns simulated Product API authentication errors.
- **Agent-driven tool selection:** `AIFunctionFactory.Create(...)` exposes existing C# methods as callable tools.
- **Conversation state:** an `AgentSession` is reused across requests, enabling follow-up questions.
- **Improved troubleshooting instructions:** the agent is asked to check deployment status, errors, and documentation when investigating failures.
- **Realistic limitations:** tool availability does not guarantee that a small local model will invoke every relevant tool or ground every conclusion in tool results.

The existing document ingestion, embeddings, and Qdrant retrieval code remains in place.

## Prerequisites

Install the following before running the application:

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Ollama](https://ollama.com/)
- [Docker](https://www.docker.com/) (for Qdrant)
- [Visual Studio Code](https://code.visualstudio.com/), Visual Studio, or another C# IDE
- Optional for VS Code: [C# Dev Kit](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit)

### 1. Install the Ollama Models

```bash
ollama pull llama3.2
ollama pull nomic-embed-text
ollama list
```

The default Ollama endpoint is `http://localhost:11434`. Make sure Ollama is running before starting the application.

### 2. Run Qdrant

```bash
docker pull qdrant/qdrant

docker run -p 6333:6333 -p 6334:6334 \
  -v "$(pwd)/qdrant_storage:/qdrant/storage:z" \
  qdrant/qdrant
```

- `6333`: Qdrant HTTP API and dashboard (`http://localhost:6333/dashboard`).
- `6334`: Qdrant gRPC port used by this .NET project.

The mounted `qdrant_storage` directory preserves data between container restarts. You can verify the container is running with `docker ps`.

### 3. Clone and Run

```bash
git clone https://github.com/ottorinobruni/LocalRagDemo.git
cd LocalRagDemo
dotnet restore
dotnet run
```

The project's `.csproj` already declares its NuGet dependencies, including `Microsoft.Agents.AI`; there is no need to add the same package again after cloning.

## Project Structure

```text
LocalRagDemo/
├── Documents/
│   ├── authentication.md
│   ├── deployment.md
│   ├── observability.md
│   └── troubleshooting.md
├── Models/
│   └── DocumentChunk.cs
├── Services/
│   ├── DocumentSearchService.cs
│   ├── DeploymentService.cs
│   └── ApplicationLogsService.cs
├── Program.cs
└── LocalRagDemo.csproj
```

### Product API Documentation

The fictional Product API knowledge base contains four Markdown documents:

- `authentication.md`: JWT configuration and validation.
- `deployment.md`: Kubernetes deployment configuration and rollout behavior.
- `observability.md`: health checks, diagnostics, and logging.
- `troubleshooting.md`: common HTTP 401 problems and investigation steps.

The application reads Markdown files from its output directory. The project includes:

```xml
<ItemGroup>
  <None Update="Documents\*.md">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
</ItemGroup>
```

### Document Ingestion

The ingestion flow, introduced in Local RAG Part 4, is reused without agent-specific changes:

1. Read Markdown files with `MarkdownReader`.
2. Create `IngestionDocument` instances.
3. Split each document with `DocumentTokenChunker`.
4. Generate embeddings with `nomic-embed-text`.
5. Store chunks and vectors in Qdrant.

Chunk IDs are based on the document name and chunk index. Rerunning the demo updates those IDs rather than inserting duplicate records. For simplicity, this implementation does not delete obsolete chunks when a document becomes shorter.

## Agent Tools

### 1. Document Search

`DocumentSearchService` reuses the existing embedding generator and vector collection. Its `SearchDocumentsAsync(string query)` method embeds a query, searches Qdrant, and returns relevant document chunks with their source document names.

```csharp
var documentSearch = new DocumentSearchService(
    vectorCollection,
    embeddingGenerator);

var searchDocumentsTool = AIFunctionFactory.Create(
    documentSearch.SearchDocumentsAsync);
```

When invoked, it logs a message similar to:

```text
[Tool] SearchDocuments: HTTP 401 troubleshooting
```

### 2. Deployment Status

`DeploymentService.GetDeploymentStatus()` returns **simulated** deployment data such as:

```text
Environment: Production
Version: 2.1.0
Platform: Kubernetes
Status: Running
Replicas: 3/3
Last Deployment: 2026-10-15 10:30 UTC
```

It logs `[Tool] GetDeploymentStatus` when called. The sample does not contact Kubernetes.

### 3. Recent Application Errors

`ApplicationLogsService.GetRecentErrors()` returns **simulated** diagnostic messages:

```text
[10:42:15] HTTP 401 Unauthorized
JWT validation failed: Invalid issuer.

[10:43:02] HTTP 401 Unauthorized
Token issuer does not match configured authority.

[10:44:18] HTTP 401 Unauthorized
Authentication failed for /api/products.
```

It logs `[Tool] GetRecentErrors` when called. The sample does not query live logs.

### Register the Tools

The services are ordinary C# classes. We expose their methods using `AIFunctionFactory`:

```csharp
var documentSearch = new DocumentSearchService(
    vectorCollection,
    embeddingGenerator);

var deploymentService = new DeploymentService();
var applicationLogsService = new ApplicationLogsService();

var searchDocumentsTool = AIFunctionFactory.Create(
    documentSearch.SearchDocumentsAsync);

var deploymentStatusTool = AIFunctionFactory.Create(
    deploymentService.GetDeploymentStatus);

var recentErrorsTool = AIFunctionFactory.Create(
    applicationLogsService.GetRecentErrors);
```

The `[Description]` attributes on the tool methods help the model understand their purpose. Tools are selected by the model; their implementations run in the C# application.

## Create the Agent with Multiple Tools

The agent reuses the Ollama chat client and exposes all three tools:

```csharp
AIAgent agent =
    chatOllama.AsAIAgent(
        name: "ProductApiAgent",
        instructions:
        """
        You are an assistant for the Product API.

        You have tools for documentation search,
        deployment status and application errors.

        When investigating a Product API failure:
        1. Call GetDeploymentStatus.
        2. Call GetRecentErrors.
        3. Call SearchDocumentsAsync for relevant documentation.
        4. Combine the actual results into your answer.

        Do not stop after using only one tool
        when investigating a failure.

        Never claim that a tool was executed unless
        its result is available in the conversation.

        Do not invent application status,
        errors or configuration values.

        If a tool was not executed, clearly state
        that the corresponding information was not verified.

        For general questions, use only the tools needed.
        """,
        tools:
        [
            searchDocumentsTool,
            deploymentStatusTool,
            recentErrorsTool
        ]);
```

These instructions *encourage* the agent to investigate failures with multiple tools. They do **not** enforce a deterministic sequence, and the model may call fewer tools than requested.

## Conversation State with `AgentSession`

In Agent Part 1, every question was independent. Part 2 uses a single session across the interactive console loop:

```csharp
AgentSession session = await agent.CreateSessionAsync();

Console.WriteLine("Product API Agent");
Console.WriteLine("Ask a question or type 'exit' to quit.");

while (true)
{
    Console.WriteLine();
    Console.Write("You: ");

    var question = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(question))
    {
        continue;
    }

    if (question.Equals("exit", StringComparison.OrdinalIgnoreCase))
    {
        break;
    }

    var response = await agent.RunAsync(question, session);

    Console.WriteLine();
    Console.WriteLine($"Agent: {response}");
}
```

**Create the session outside the loop.** Reusing it lets the agent interpret questions such as “What should I check next?” in the context of previous exchanges.

The sample session lives only in memory. Exiting the application and launching it again starts a new conversation. Session persistence is not implemented in this demo.

## Run the Demo

With Ollama and Qdrant running, execute:

```bash
dotnet run
```

At startup, the application ingests the Markdown documents, generates embeddings, upserts the chunks in Qdrant, creates the three tools and the agent, and starts the interactive prompt.

### Test Individual Tools

Try these questions to understand tool selection:

```text
What is the current deployment status of the Product API?

Show me the most recent Product API application errors.

According to the Product API documentation, which environment
variables are required for JWT authentication?
```

The corresponding tools are `GetDeploymentStatus`, `GetRecentErrors`, and `SearchDocumentsAsync`. Tool choice is still made by the model, so the exact calls may vary.

### Test Multiple Tools

This prompt successfully triggered all three tools during the Part 2 article's test:

```text
I need a complete Product API diagnostic report. Check the deployment
status, retrieve recent application errors, and search the documentation
for HTTP 401 troubleshooting. Include findings from all three sources.
```

Observed tool calls in that run:

```text
[Tool] SearchDocuments: HTTP 401 troubleshooting
[Tool] GetRecentErrors
[Tool] GetDeploymentStatus
```

The agent returned a report combining deployment information, recent authentication errors, and troubleshooting documentation. The tool order was chosen by the model, not hard-coded by the application.

### Test Conversation State

Ask several questions in the **same application run**:

```text
You: What version of the Product API is running in production?
You: How many replicas does that version have?
You: Can you summarize what we just discussed?
```

The reused `AgentSession` makes the previous conversation available when answering follow-up questions. The quality of the answers still depends on the model.

## Observed Limitations

The Part 2 experiments with local `llama3.2` showed important differences between **available tools** and **tools actually invoked**:

- For some troubleshooting prompts, the agent called only `SearchDocumentsAsync` or `GetRecentErrors` before answering, despite having three tools available.
- A more explicit request for a diagnostic report produced a successful three-tool run.
- In some responses, the model included claims not supported by the retrieved results. For example, it suggested that a token might be expired when the simulated logs only showed issuer-validation failures.

**Successful tool execution does not guarantee a fully grounded answer.** Verify conclusions against the actual tool results, especially for operational or security-sensitive decisions.

Better tool descriptions and instructions may improve behavior but cannot guarantee that every tool will be called or that a specific sequence will be followed. If mandatory checks must always run, a structured workflow is more predictable than relying solely on model-driven tool selection.

The upcoming Agent Part 3 will explore **workflows** and **human-in-the-loop approvals**.

## Configuration

The sample's defaults in `Program.cs` are:

```csharp
const string OllamaUrl = "http://localhost:11434";
const string ChatModel = "llama3.2";
const string EmbeddingModel = "nomic-embed-text";

const string QdrantHost = "localhost";
const int QdrantPort = 6334;

const string CollectionName = "local-rag-agent";
const int MaxTokensPerChunk = 200;
const int OverlapTokens = 30;
```

`DocumentSearchService` uses:

```csharp
private const int TopResults = 3;
private const double MinimumScore = 0.5;
```

Adjust endpoints and configuration values for your environment.

## Article Versions

The series has progressed through the following stages:

| Version | Focus |
| --- | --- |
| `part-1` | Initial local RAG setup |
| `part-2` | Manual RAG pipeline with Ollama and Qdrant |
| `part-3` | RAG using `Microsoft.Extensions.AI` and `Microsoft.Extensions.VectorData` |
| `part-4` | Document ingestion with `Microsoft.Extensions.DataIngestion` |
| `agent-part-1` | AI agent with document retrieval as a tool |
| **Agent Part 2 (current)** | Multiple tools and conversation state |

The labels above describe the series' stages. See the repository's [tags](https://github.com/ottorinobruni/LocalRagDemo/tags) for the actual available Git tags; a new Part 2 tag should only be referenced after it has been created.

To explore a previous tagged agent version, if available:

```bash
git checkout agent-part-1
```

## Resources

- [Microsoft Agent Framework](https://learn.microsoft.com/agent-framework/)
- [Microsoft Agent Framework – Sessions](https://learn.microsoft.com/en-us/agent-framework/agents/conversations/session)
- [Microsoft.Extensions.AI](https://learn.microsoft.com/dotnet/ai/microsoft-extensions-ai)
- [Microsoft.Extensions.VectorData](https://learn.microsoft.com/dotnet/ai/vector-data)
- [Microsoft.Extensions.DataIngestion](https://learn.microsoft.com/dotnet/ai/conceptual/data-ingestion)
- [Ollama](https://ollama.com/)
- [Ollama API Documentation](https://docs.ollama.com/api/introduction)
- [Qdrant](https://qdrant.tech/)
- [Qdrant Documentation](https://qdrant.tech/documentation/)
- [.NET](https://dotnet.microsoft.com/)

## Author

Created by **Ottorino Bruni**.

- Blog: [ottorinobruni.com](https://www.ottorinobruni.com/)
- LinkedIn: [linkedin.com/in/ottorinobruni](https://www.linkedin.com/in/ottorinobruni/)
- X / Twitter: [@ottorinobruni](https://twitter.com/ottorinobruni)

If you found this project useful, explore the articles linked above to follow its evolution from a local RAG pipeline to a tool-enabled AI agent.
