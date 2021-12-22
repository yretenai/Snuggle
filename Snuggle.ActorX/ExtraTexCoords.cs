using System.Numerics;

namespace Snuggle.ActorX;

public record ExtraTexCoords : ChunkHeader<Vector2> {
    public ExtraTexCoords(int index) {
        Id = $"EXTRAUVS{index}";
        Size = 8;
    }
}
