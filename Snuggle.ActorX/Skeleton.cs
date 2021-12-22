namespace Snuggle.ActorX;

public record Skeleton : ChunkHeader<Bone> {
    public Skeleton() {
        Id = "REFSKELT";
        Size = 120;
    }
}