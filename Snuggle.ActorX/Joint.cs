using System.Numerics;

namespace Snuggle.ActorX;

public record struct Joint {
    public Quaternion Orientation;
    public Vector3 Position;
    public float Length;
    public Vector3 Size;
}