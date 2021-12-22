namespace Snuggle.ActorX;

public record Faces : ChunkHeader<Face<FaceWedge>> {
    public Faces() {
        Id = "FACE3200";
        Size = 18;
    }
}
