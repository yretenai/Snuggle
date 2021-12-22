namespace Snuggle.ActorX;

public interface IChunkHeader {
    public string Id { get; }
    public uint Type { get; }
    public uint Size { get; }
    public uint Count { get; }
}
