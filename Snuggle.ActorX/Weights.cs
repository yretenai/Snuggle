namespace Snuggle.ActorX;

public record Weights : ChunkHeader<Weight> {
    public Weights() {
        Id = "RAWWEIGHTS";
        Size = 12;
    }
}