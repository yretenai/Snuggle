namespace Snuggle.ActorX; 

public record ActorHead : ChunkHeader<IChunkHeader> {
    public ActorHead() {
        Id = "ACTRHEAD";
        Type = 20210917;
    }
}
