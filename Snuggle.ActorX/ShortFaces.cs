namespace Snuggle.ActorX;

public record ShortFaces : ChunkHeader<Face<ShortFaceWedge>> {
    public ShortFaces() {
        Id = "FACE0000";
        Size = 12;
    }
}
