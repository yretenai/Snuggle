namespace Snuggle.ActorX;

public record struct Bone {
    public string Name;
    public uint Flags;
    public uint Children;
    public int Parent;
    public Joint Joint;
}
