using System.Numerics;

namespace Snuggle.ActorX;

public record Normals : ChunkHeader<Vector3> {
    public Normals() {
        Id = "VTXNORMS";
        Size = 12;
    }
}
