namespace Snuggle.ActorX;

public record Colors : ChunkHeader<Color> {
    public Colors() {
        Id = "VERTEXCOLOR";
        Size = 4;
    }
}

public record struct Color {
    public byte R;
    public byte G;
    public byte B;
    public byte A;
}
