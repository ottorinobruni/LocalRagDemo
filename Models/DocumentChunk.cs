using Microsoft.Extensions.VectorData;

namespace LocalRagDemo.Models;

public sealed class DocumentChunk
{
    [VectorStoreKey]
    public Guid Id { get; set; }

    [VectorStoreData]
    public string DocumentName { get; set; } = string.Empty;

    [VectorStoreData]
    public int ChunkIndex { get; set; }

    [VectorStoreData]
    public string Text { get; set; } = string.Empty;

    [VectorStoreVector(768, DistanceFunction = DistanceFunction.CosineSimilarity)]
    public ReadOnlyMemory<float> Embedding { get; set; }
}