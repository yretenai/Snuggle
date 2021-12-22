namespace Snuggle.ActorX;

public record Materials : ChunkHeader<Material> {
    public Materials() {
        Id = "MATT0000";
        Size = 88;
    }
}
