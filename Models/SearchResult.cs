public sealed record SearchResult(
    string DocumentName,
    int ChunkIndex,
    string Text,
    float Score);