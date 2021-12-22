namespace Snuggle.ActorX;

public record ChunkHeader<T> : IChunkHeader {
    public string Id { get; protected init; } = "";
    public uint Type { get; protected init; }
    public uint Size { get; protected init; }
    public uint Count => (uint) Values.Count;
    public List<T> Values { get; } = new();
}
