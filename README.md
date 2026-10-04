# Local RAG and AI Agent in C# with Ollama and Qdrant

A simple educational project that started as a fully local Retrieval-Augmented Generation (RAG) application and now evolves into a local AI Agent using **C#**, **Ollama**, **Qdrant**, and **Microsoft Agent Framework**.

The current version keeps the existing document ingestion and vector search pipeline, but exposes document retrieval as an agent tool. Instead of always searching Qdrant before every model call, the model can decide when Product API documentation is needed.

> **Disclaimer:** This project is purely for educational purposes. There are better ways to structure, secure, and optimize real-world applications. Use it as a starting point for learning and adapt it to your production requirements.

## Related Articles

### Local RAG Series

- [Build a Local RAG Application in C# with Ollama and Qdrant - Part 1](https://www.ottorinobruni.com/build-local-rag-application-csharp-ollama-qdrant-part-1/)
- [Build a Local RAG Application in C# with Ollama and Qdrant - Part 2](https://www.ottorinobruni.com/build-local-rag-application-csharp-ollama-qdrant-part-2/)
- [Build a Local RAG Application in C# with Ollama and Qdrant - Part 3](https://www.ottorinobruni.com/build-local-rag-application-csharp-ollama-qdrant-part-3/)
- [Build a Local RAG Application in C# with Ollama and Qdrant - Part 4](https://www.ottorinobruni.com/build-local-rag-application-csharp-ollama-qdrant-part-4/)

### AI Agent Series

- [From Local RAG to an AI Agent in C# with Ollama and Qdrant - Part 1](https://www.ottorinobruni.com/from-local-rag-to-ai-agent-csharp-ollama-qdrant-part-1/)

You may also find this introductory article useful:

- [AI Basics for Developers: Tokens, Inference, Embeddings, Vector Databases and RAG Explained](https://www.ottorinobruni.com/ai-basics-for-developers-tokens-inference-embeddings-vector-databases-rag-explained/)

## How It Works

The current version combines the existing RAG retrieval pipeline with an AI Agent.

```text
User Question
     |
     v
AI Agent / Ollama
     |
     +---- No tool required ----> Answer
     |
     +---- SearchDocumentsAsync
                |
                v
             Qdrant
                |
                v
      Retrieved Documentation
                |
                v
          AI Agent / Ollama
                |
                v
              Answer
```

The main difference from the previous RAG version is **who decides when retrieval happens**.

In the original RAG application, every question followed a predefined flow:

```text
Question → Retrieval → Prompt → Model → Answer
```

In the agent version, document search becomes an optional capability that the model can request when Product API-specific information is needed.

The project uses:

- **C# / .NET 10** for the application.
- **Microsoft Agent Framework** to create the AI Agent and expose document search as a tool.
- **Microsoft.Extensions.AI** for chat and embedding abstractions.
- **Microsoft.Extensions.VectorData** for vector storage and vector search abstractions.
- **Microsoft.Extensions.DataIngestion** to load and prepare documents.
- **Microsoft.Extensions.DataIngestion.Markdig** to read Markdown documents into `IngestionDocument`.
- **Microsoft.ML.Tokenizers** for token-based chunking.
- **OllamaSharp** to integrate Ollama with the .NET AI abstractions.
- **CommunityToolkit.VectorData.Qdrant** to integrate Qdrant with `Microsoft.Extensions.VectorData`.
- **Ollama** to run the local models.
- **nomic-embed-text** to generate embeddings.
- **llama3.2** as the local chat model.
- **Qdrant** to store embeddings and perform vector similarity search.

Everything can run locally on your machine.

## What's New in AI Agent - Part 1

This version evolves the previous local RAG application into the first version of a local AI Agent.

The main changes are:

- Added Microsoft Agent Framework.
- Added `DocumentSearchService`.
- Exposed vector search through `SearchDocumentsAsync`.
- Replaced unconditional retrieval with agent-driven tool calling.
- Added multiple Product API Markdown documents.
- Added tool logging so that we can observe when document retrieval is requested.
- Kept the existing Ollama, Qdrant, VectorData, DataIngestion, and embedding infrastructure.

The retrieval pipeline itself still works in the same way:

```text
Query → Embedding → Qdrant → Relevant Chunks
```

What changes is how that pipeline is triggered.

## Prerequisites

Before running the project, install:

- [.NET SDK](https://dotnet.microsoft.com/download)
- [Ollama](https://ollama.com/)
- [Docker](https://www.docker.com/)
- [Qdrant](https://qdrant.tech/)
- [Visual Studio Code](https://code.visualstudio.com/) or another C# IDE
- [C# Dev Kit for VS Code](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit) if you use VS Code

## 1. Install the Ollama Models

Pull the local language model:

```bash
ollama pull llama3.2
```

Pull the embedding model:

```bash
ollama pull nomic-embed-text
```

Verify that both models are available:

```bash
ollama list
```

Ollama normally exposes its local API at:

```text
http://localhost:11434
```

## 2. Run Qdrant

Download the latest Qdrant Docker image:

```bash
docker pull qdrant/qdrant
```

Run Qdrant:

```bash
docker run -p 6333:6333 -p 6334:6334 \
    -v "$(pwd)/qdrant_storage:/qdrant/storage:z" \
    qdrant/qdrant
```

The ports used are:

- `6333` — Qdrant HTTP/REST API and web interface.
- `6334` — Qdrant gRPC API used by the .NET client.

You can verify that Qdrant is running with:

```bash
docker ps
```

Or open:

```text
http://localhost:6333
```

The `qdrant_storage` directory keeps the vector database data persistent when the container is stopped or recreated.

## 3. Clone and Restore the Project

Clone the repository:

```bash
git clone https://github.com/ottorinobruni/LocalRagDemo
cd LocalRagDemo
```

Restore the dependencies:

```bash
dotnet restore
```

The project already contains the libraries introduced in the previous RAG series. The new dependency introduced for the agent version is Microsoft Agent Framework.

```bash
dotnet add package Microsoft.Agents.AI
```

## Project Structure

The current version uses multiple Product API documents and keeps the document search logic separate from the agent configuration.

```text
LocalRagDemo
├── Documents
│   ├── authentication.md
│   ├── deployment.md
│   ├── observability.md
│   └── troubleshooting.md
├── Models
│   └── DocumentChunk.cs
├── Services
│   └── DocumentSearchService.cs
├── Program.cs
└── LocalRagDemo.csproj
```

## 4. Product API Documents

The demo uses a small fictional Product API knowledge base split across several Markdown files:

- `authentication.md` — JWT authentication and related configuration.
- `deployment.md` — Kubernetes deployment and configuration changes.
- `observability.md` — logs, diagnostics, and health checks.
- `troubleshooting.md` — common HTTP 401 problems and diagnostic steps.

The files must be copied to the application output directory because the application loads them from `AppContext.BaseDirectory`.

The project file contains:

```xml
<ItemGroup>
  <None Update="Documents\*.md">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
</ItemGroup>
```

This ensures that all Markdown documents are available at runtime.

## 5. Document Ingestion

The ingestion pipeline is the same one introduced in Part 4 of the RAG series:

```text
Documents → IngestionDocument → Token-based Chunks → Embeddings → Qdrant
```

For each Markdown file, the application:

1. Loads the document using `MarkdownReader`.
2. Converts it into an `IngestionDocument`.
3. Splits it into token-based chunks using `DocumentTokenChunker`.
4. Generates embeddings using `nomic-embed-text`.
5. Stores the chunks and vectors in Qdrant.

Chunk IDs are generated deterministically from the document name and chunk index, so running the application again updates existing chunks instead of inserting duplicates.

For simplicity, this demo does not remove obsolete chunks if a document becomes shorter.

## 6. Document Search as an Agent Tool

`DocumentSearchService` contains the vector search logic.

The service receives:

- the Qdrant vector collection;
- the embedding generator.

Its `SearchDocumentsAsync` method:

1. receives a natural-language query;
2. generates its embedding;
3. searches Qdrant;
4. returns the most relevant Product API document chunks.

The method is exposed to the agent through:

```csharp
var searchDocumentsTool =
    AIFunctionFactory.Create(
        documentSearch.SearchDocumentsAsync);
```

Every tool invocation is logged:

```text
[Tool] SearchDocuments: ...
```

This makes it easy to observe when the model decides that document retrieval is required.

## 7. Create the Agent

The agent is created from the existing Ollama chat client:

```csharp
AIAgent agent =
    chatOllama.AsAIAgent(
        name: "ProductApiAgent",

        instructions:
        """
        You are an assistant for the Product API.

        Use the available documentation tool when you need
        Product API-specific information to answer the user's question.

        Base Product API-specific answers on information returned
        by the documentation tool.

        If the documentation does not contain enough information,
        say so clearly.

        Do not invent Product API configuration values.
        """,

        tools:
        [
            searchDocumentsTool
        ]);
```

The model does not access Qdrant directly. It only knows that a documentation search capability is available and can decide when to request it.

Each `RunAsync` call is independent in this first version. Conversation state and memory are intentionally outside the scope of Part 1.

## 8. Run the Application

Make sure Ollama and Qdrant are running, then execute:

```bash
dotnet run
```

The application will:

1. Read the Product API Markdown documents.
2. Split them into token-based chunks.
3. Generate embeddings using `nomic-embed-text`.
4. Store the chunks in Qdrant.
5. Create `DocumentSearchService`.
6. Expose `SearchDocumentsAsync` as an agent tool.
7. Create the `ProductApiAgent`.
8. Wait for a user question.
9. Let the model decide whether document retrieval is needed.
10. Execute the tool when requested and return the final answer.

You should see output similar to:

```text
Indexed 4 chunks from 4 documents.

Product API Agent
Ask a question or type 'exit' to quit.

You:
```

The exact number of chunks may vary depending on the document content and chunking configuration.

## Example Questions

### General question

```text
What is the difference between HTTP GET and POST?
```

This does not require Product API-specific information. The model may answer without calling the documentation tool.

### Product API-specific question

```text
Which environment variable configures the JWT authority for the Product API?
```

Here the answer exists only in the demo documentation, so the agent can use the document search tool.

You may see output similar to:

```text
[Tool] SearchDocuments: Product API JWT authority environment variable
```

The exact generated query may vary between runs.

### More complex question

```text
After deploying the Product API to Kubernetes, some requests return HTTP 401.
Which configuration should I check, and what diagnostic information is available?
```

This question can require information from authentication, deployment, troubleshooting, and observability documents.

The important difference from the previous RAG application is that retrieval is no longer executed unconditionally. The model can decide whether the documentation tool is needed.

## Configuration

The default configuration used by the sample application is:

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

`DocumentSearchService` also uses:

```csharp
const int TopResults = 3;
const double MinimumScore = 0.5;
```

If your services run on different ports or hosts, update these values accordingly.

## Article Versions

Each article corresponds to a tagged version of the repository:

- `part-1` — Initial project setup.
- `part-2` — Manual RAG implementation with Ollama and Qdrant.
- `part-3` — RAG implementation using `Microsoft.Extensions.AI` and `Microsoft.Extensions.VectorData`.
- `part-4` — Document ingestion using `Microsoft.Extensions.DataIngestion` and token-based chunking.
- `agent-part-1` — Local AI Agent using Microsoft Agent Framework and document search as a tool.

You can check out the agent version using:

```bash
git checkout agent-part-1
```

## Resources

- [Microsoft Agent Framework](https://learn.microsoft.com/agent-framework/)
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
- LinkedIn: [www.linkedin.com/in/ottorinobruni](https://www.linkedin.com/in/ottorinobruni/)
- X / Twitter: [@ottorinobruni](https://twitter.com/ottorinobruni)

If you found the project useful, you can follow the complete explanation in the related article series linked above.
