namespace Snuggle.SubScenes.Models;

public class EntityCustomMetadata {
    public EntityCustomMetadata() {
        TypeHash = 0;
        Data = Memory<byte>.Empty;
    }

    public EntityCustomMetadata(MemoryReader reader) {
        TypeHash = reader.Read<ulong>();
        Data = reader.ReadBlobArray<byte>();
    }

    public ulong TypeHash { get; set; }
    public Memory<byte> Data { get; set; }
}
