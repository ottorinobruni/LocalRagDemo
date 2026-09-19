using System.Security.Cryptography;
using System.Text;
using CommunityToolkit.VectorData.Qdrant;
using LocalRagDemo.Models;
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
const string CollectionName = "local-rag";

const int MaxTokensPerChunk = 200;
const int OverlapTokens = 30;

const int TopResults = 3;
const double MinimumScore = 0.5;

//
// AI clients
//

using var chatOllama = new OllamaApiClient(
    new Uri(OllamaUrl),
    ChatModel);

using var embeddingOllama = new OllamaApiClient(
    new Uri(OllamaUrl),
    EmbeddingModel);

IChatClient chatClient = chatOllama;

IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator =
    embeddingOllama;

//
// Qdrant through VectorData
//

var qdrantClient = new QdrantClient(
    QdrantHost,
    QdrantPort);

using var vectorStore = new QdrantVectorStore(
    qdrantClient,
    ownsClient: true);

using var vectorCollection =
    vectorStore.GetCollection<Guid, DocumentChunk>(
        CollectionName);

await vectorCollection.EnsureCollectionExistsAsync();

//
// Document ingestion
//

const string DocumentName = "sample.txt";

var documentPath = Path.Combine(
    AppContext.BaseDirectory,
    "Documents",
    DocumentName);

if (!File.Exists(documentPath))
{
    Console.WriteLine($"Document not found: {documentPath}");
    return;
}

var reader = new MarkdownReader();

var document = await reader.ReadAsync(
    new FileInfo(documentPath),
    DocumentName);

var tokenizer =
    TiktokenTokenizer.CreateForModel("gpt-4o");

var chunker = new DocumentTokenChunker(
    new IngestionChunkerOptions(tokenizer)
    {
        MaxTokensPerChunk = MaxTokensPerChunk,
        OverlapTokens = OverlapTokens
    });

var chunks = new List<DocumentChunk>();

var chunkIndex = 0;

await foreach (var chunk in chunker.ProcessAsync(document))
{
    chunks.Add(new DocumentChunk
    {
        Id = CreateChunkId(
            DocumentName,
            chunkIndex),

        DocumentName = DocumentName,
        ChunkIndex = chunkIndex,
        Text = chunk.Content
    });

    chunkIndex++;
}

//
// Generate embeddings and store the chunks
//

var embeddings = await embeddingGenerator.GenerateAsync(
    chunks.Select(chunk => chunk.Text));

for (var i = 0; i < chunks.Count; i++)
{
    chunks[i].Embedding = embeddings[i].Vector;
}

await vectorCollection.UpsertAsync(chunks);

Console.WriteLine(
    $"Indexed {chunks.Count} document chunks.");

Console.WriteLine();
Console.WriteLine("Ask a question about the document.");
Console.WriteLine("Type 'exit' to quit.");

//
// Query loop
//

while (true)
{
    Console.Write("\nYou: ");

    var question = Console.ReadLine();

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

    var questionEmbeddings =
        await embeddingGenerator.GenerateAsync([question]);

    var questionVector =
        questionEmbeddings[0].Vector;

    var context = new StringBuilder();
    var matches = 0;

    var results = vectorCollection.SearchAsync(
        questionVector,
        top: TopResults,
        options: new VectorSearchOptions<DocumentChunk>
        {
            ScoreThreshold = MinimumScore
        });

    await foreach (var result in results)
    {
        context.AppendLine(result.Record.Text);
        context.AppendLine();

        matches++;
    }

    if (matches == 0)
    {
        Console.WriteLine();
        Console.WriteLine(
            "Assistant: The available documents do not contain enough information.");

        continue;
    }

    var prompt = $"""
        Answer the question using only the provided context.

        If the context contains information related to the question,
        answer directly using that information.

        A partial answer is acceptable.
        Do not refuse to answer just because the context does not provide
        a complete explanation.

        Only say "The available documents do not contain enough information."
        if the context contains no information relevant to the question.

        Do not use external knowledge and do not speculate.

        Context:
        {context}

        Question:
        {question}

        Answer:
        """;

    var response = await chatClient.GetResponseAsync(
        prompt,
        new ChatOptions
        {
            Temperature = 0
        });

    Console.WriteLine();
    Console.WriteLine(
        $"Assistant: {response.Text}");
}

//
// Builds a stable identifier so that re-running the application
// overwrites the existing chunks instead of duplicating them.
//

static Guid CreateChunkId(
    string documentName,
    int chunkIndex)
{
    var bytes = Encoding.UTF8.GetBytes(
        $"{documentName}:{chunkIndex}");

    return new Guid(MD5.HashData(bytes));
}