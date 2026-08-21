using Qdrant.Client;
using Qdrant.Client.Grpc;

public sealed class QdrantVectorStore
{
    private readonly QdrantClient _client;
    private readonly string _collectionName;

    public QdrantVectorStore(
        QdrantClient client,
        string collectionName)
    {
        _client = client;
        _collectionName = collectionName;
    }

    public async Task EnsureCollectionAsync(
        ulong vectorSize,
        CancellationToken cancellationToken = default)
    {
        if (await _client.CollectionExistsAsync(
            _collectionName,
            cancellationToken))
        {
            return;
        }

        await _client.CreateCollectionAsync(
            collectionName: _collectionName,
            vectorsConfig: new VectorParams
            {
                Size = vectorSize,
                Distance = Distance.Cosine
            },
            cancellationToken: cancellationToken);
    }

    public async Task StoreAsync(
        DocumentChunk chunk,
        float[] embedding,
        CancellationToken cancellationToken = default)
    {
        var point = new PointStruct
        {
            Id = new PointId
            {
                Uuid = chunk.Id.ToString()
            },
            Vectors = embedding,
            Payload =
            {
                ["documentName"] = chunk.DocumentName,
                ["chunkIndex"] = chunk.ChunkIndex,
                ["text"] = chunk.Text
            }
        };

        await _client.UpsertAsync(
            _collectionName,
            [point],
            cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<SearchResult>> SearchAsync(
        float[] queryVector,
        ulong limit = 5,
        CancellationToken cancellationToken = default)
    {
        var results = await _client.QueryAsync(
            collectionName: _collectionName,
            query: queryVector,
            limit: limit,
            cancellationToken: cancellationToken);

        return results
            .Select(result => new SearchResult(
                result.Payload["documentName"].StringValue,
                (int)result.Payload["chunkIndex"].IntegerValue,
                result.Payload["text"].StringValue,
                result.Score))
            .ToList();
    }
}