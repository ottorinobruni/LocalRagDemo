using System.ComponentModel;
using System.Text;
using LocalRagDemo.Models;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;

namespace LocalRagDemo.Services;

public sealed class DocumentSearchService(
    VectorStoreCollection<Guid, DocumentChunk> vectorCollection,
    IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator)
{
    private const int TopResults = 3;
    private const double MinimumScore = 0.5;

    [Description(
        "Searches the Product API documentation for information about " +
        "authentication, deployment, observability and troubleshooting.")]
    public async Task<string> SearchDocumentsAsync(
        [Description(
            "The search query used to find relevant Product API documentation.")]
        string query)
    {
        Console.WriteLine();
        Console.WriteLine($"[Tool] SearchDocuments: {query}");

        var embeddings =
            await embeddingGenerator.GenerateAsync([query]);

        var queryVector =
            embeddings[0].Vector;

        var results =
            vectorCollection.SearchAsync(
                queryVector,
                top: TopResults,
                options: new VectorSearchOptions<DocumentChunk>
                {
                    ScoreThreshold = MinimumScore
                });

        var context = new StringBuilder();

        await foreach (var result in results)
        {
            context.AppendLine(
                $"Source: {result.Record.DocumentName}");

            context.AppendLine(
                result.Record.Text);

            context.AppendLine();
        }

        if (context.Length == 0)
        {
            return
                "No relevant information was found in the Product API documentation.";
        }

        return context.ToString();
    }
}