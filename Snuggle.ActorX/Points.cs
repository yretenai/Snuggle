using System.Numerics;

namespace Snuggle.ActorX;

public record Points : ChunkHeader<Vector3> {
    public Points() {
        Id = "PNTS0000";
        Size = 12;
    }
}
