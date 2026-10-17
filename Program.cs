using System.Security.Cryptography;
using System.Text;
using CommunityToolkit.VectorData.Qdrant;
using LocalRagDemo.Models;
using LocalRagDemo.Services;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DataIngestion;
using Microsoft.Extensions.DataIngestion.Chunkers;
using Microsoft.Extensions.VectorData;
using Microsoft.ML.Tokenizers;
using OllamaSharp;
using Qdrant.Client;

const string OllamaUrl = "http://localhost:11434";

const string ChatModel = "llama3.2";
const string EmbeddingModel = "nomic-embed-text";

const string QdrantHost = "localhost";
const int QdrantPort = 6334;

const string CollectionName = "local-rag-agent";

const int MaxTokensPerChunk = 200;
const int OverlapTokens = 30;

//
// Ollama clients
//

using var chatOllama =
    new OllamaApiClient(
        new Uri(OllamaUrl),
        ChatModel);

using var embeddingOllama =
    new OllamaApiClient(
        new Uri(OllamaUrl),
        EmbeddingModel);

IEmbeddingGenerator<string, Embedding<float>>
    embeddingGenerator = embeddingOllama;

//
// Qdrant
//

var qdrantClient =
    new QdrantClient(
        QdrantHost,
        QdrantPort);

using var vectorStore =
    new QdrantVectorStore(
        qdrantClient,
        ownsClient: true);

using var vectorCollection =
    vectorStore.GetCollection<Guid, DocumentChunk>(
        CollectionName);

await vectorCollection.EnsureCollectionExistsAsync();

//
// Document ingestion
//

var documentsPath =
    Path.Combine(
        AppContext.BaseDirectory,
        "Documents");

if (!Directory.Exists(documentsPath))
{
    Console.WriteLine(
        $"Documents directory not found: {documentsPath}");

    return;
}

var documentFiles =
    Directory.GetFiles(
        documentsPath,
        "*.md");

if (documentFiles.Length == 0)
{
    Console.WriteLine(
        "No Markdown documents were found.");

    return;
}

var reader =
    new MarkdownReader();

var tokenizer =
    TiktokenTokenizer.CreateForModel(
        "gpt-4o");

var chunker =
    new DocumentTokenChunker(
        new IngestionChunkerOptions(tokenizer)
        {
            MaxTokensPerChunk = MaxTokensPerChunk,
            OverlapTokens = OverlapTokens
        });

var chunks =
    new List<DocumentChunk>();

foreach (var file in documentFiles)
{
    var documentName =
        Path.GetFileName(file);

    var document =
        await reader.ReadAsync(
            new FileInfo(file),
            documentName);

    var chunkIndex = 0;

    await foreach (
        var chunk in chunker.ProcessAsync(document))
    {
        chunks.Add(
            new DocumentChunk
            {
                Id = CreateChunkId(
                    documentName,
                    chunkIndex),

                DocumentName =
                    documentName,

                ChunkIndex =
                    chunkIndex,

                Text =
                    chunk.Content
            });

        chunkIndex++;
    }
}

//
// Generate embeddings
//

var embeddings =
    await embeddingGenerator.GenerateAsync(
        chunks.Select(
            chunk => chunk.Text));

for (var i = 0; i < chunks.Count; i++)
{
    chunks[i].Embedding =
        embeddings[i].Vector;
}

//
// Store chunks in Qdrant
//

await vectorCollection.UpsertAsync(
    chunks);

Console.WriteLine(
    $"Indexed {chunks.Count} chunks " +
    $"from {documentFiles.Length} documents.");

//
// Create the agent tools (Part 2)
//

var documentSearch =
    new DocumentSearchService(
        vectorCollection,
        embeddingGenerator);

var deploymentService =
    new DeploymentService();

var applicationLogsService =
    new ApplicationLogsService();

var searchDocumentsTool =
    AIFunctionFactory.Create(
        documentSearch.SearchDocumentsAsync);

var deploymentStatusTool =
    AIFunctionFactory.Create(
        deploymentService.GetDeploymentStatus);

var recentErrorsTool =
    AIFunctionFactory.Create(
        applicationLogsService.GetRecentErrors);

//
// Create the agent with multiple tools (Part 2)
//

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

//
// Reuse one session across all questions (Part 2)
//

// Create a session for the conversation
AgentSession session = await agent.CreateSessionAsync();

//
// Interactive loop
//

Console.WriteLine();
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

//
// Generates deterministic chunk IDs.
// Running the application again updates existing chunks
// instead of inserting duplicates.
//

static Guid CreateChunkId(
    string documentName,
    int chunkIndex)
{
    var bytes = Encoding.UTF8.GetBytes($"{documentName}:{chunkIndex}");

    return new Guid(MD5.HashData(bytes));
}