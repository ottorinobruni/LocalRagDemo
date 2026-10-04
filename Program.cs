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
// Create the document search tool
//

var documentSearch =
    new DocumentSearchService(
        vectorCollection,
        embeddingGenerator);

var searchDocumentsTool =
    AIFunctionFactory.Create(
        documentSearch.SearchDocumentsAsync);

//
// Create the agent
//

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

//
// Interactive loop
//

Console.WriteLine();
Console.WriteLine("Product API Agent");
Console.WriteLine(
    "Ask a question or type 'exit' to quit.");

while (true)
{
    Console.WriteLine();
    Console.Write("You: ");

    var question =
        Console.ReadLine();

    if (string.IsNullOrWhiteSpace(question))
    {
        continue;
    }

    if (question.Equals(
        "exit",
        StringComparison.OrdinalIgnoreCase))
    {
        break;
    }

    var response =
        await agent.RunAsync(question);

    Console.WriteLine();
    Console.WriteLine(
        $"Agent: {response}");
}

//
// Generates deterministic chunk IDs.
//
// Running the application again updates existing chunks
// instead of inserting duplicates.
//

static Guid CreateChunkId(
    string documentName,
    int chunkIndex)
{
    var bytes =
        Encoding.UTF8.GetBytes(
            $"{documentName}:{chunkIndex}");

    return new Guid(
        MD5.HashData(bytes));
}