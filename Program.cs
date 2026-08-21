using Qdrant.Client;

const string ollamaUrl = "http://localhost:11434";
const string qdrantHost = "localhost";
const int qdrantPort = 6334;

const string embeddingModel = "nomic-embed-text";
const string chatModel = "llama3.2";
const string collectionName = "local-rag";

using var httpClient = new HttpClient
{
    BaseAddress = new Uri(ollamaUrl)
};

var qdrantClient = new QdrantClient(
    qdrantHost,
    qdrantPort);

var ollama = new OllamaService(
    httpClient,
    embeddingModel,
    chatModel);

var vectorStore = new QdrantVectorStore(
    qdrantClient,
    collectionName);

var documentPath = "Documents/sample.txt";

var text = await File.ReadAllTextAsync(documentPath);

var chunks = SplitDocument(
        Path.GetFileName(documentPath),
        text)
    .ToList();

if (chunks.Count == 0)
{
    throw new InvalidOperationException(
        "The document contains no chunks.");
}

var firstEmbedding =
    await ollama.GenerateEmbeddingAsync(
        chunks[0].Text);

await vectorStore.EnsureCollectionAsync(
    (ulong)firstEmbedding.Length);

await vectorStore.StoreAsync(
    chunks[0],
    firstEmbedding);

foreach (var chunk in chunks.Skip(1))
{
    var embedding =
        await ollama.GenerateEmbeddingAsync(
            chunk.Text);

    await vectorStore.StoreAsync(
        chunk,
        embedding);
}

Console.WriteLine(
    $"Indexed {chunks.Count} document chunks.");

Console.WriteLine();
Console.WriteLine(
    "Ask a question about the document.");
Console.WriteLine(
    "Type 'exit' to quit.");

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

    var questionEmbedding =
        await ollama.GenerateEmbeddingAsync(question);

    var relevantChunks =
        await vectorStore.SearchAsync(
            questionEmbedding,
            limit: 5);

    var answer =
        await ollama.GenerateAnswerAsync(
            question,
            relevantChunks);

    Console.WriteLine();
    Console.WriteLine($"Assistant: {answer}");
}

static IEnumerable<DocumentChunk> SplitDocument(
    string documentName,
    string text,
    int chunkSize = 800,
    int overlap = 100)
{
    var index = 0;
    var start = 0;

    while (start < text.Length)
    {
        var length = Math.Min(
            chunkSize,
            text.Length - start);

        var chunkText = text
            .Substring(start, length)
            .Trim();

        if (!string.IsNullOrWhiteSpace(chunkText))
        {
            yield return new DocumentChunk(
                Guid.NewGuid(),
                documentName,
                index++,
                chunkText);
        }

        if (start + length >= text.Length)
        {
            break;
        }

        start += length - overlap;
    }
}