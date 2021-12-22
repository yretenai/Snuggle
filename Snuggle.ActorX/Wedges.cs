namespace Snuggle.ActorX;

public record Wedges : ChunkHeader<Wedge> {
    public Wedges() {
        Id = "VTXW0000";
        Size = 16;
    }
}

public record struct Wedge {
    public uint PointIndex;
    public float U;
    public float V;
    public byte Material;
    public byte Reserved;
    public ushort Pad;
}
