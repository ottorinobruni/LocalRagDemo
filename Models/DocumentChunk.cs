public sealed record DocumentChunk(
    Guid Id,
    string DocumentName,
    int ChunkIndex,
    string Text);