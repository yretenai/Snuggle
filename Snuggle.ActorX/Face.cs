namespace Snuggle.ActorX;

public record struct Face<T> where T : struct {
    public T Wedge;
    public byte MaterialIndex;
    public byte AuxiliaryMaterialIndex;
    public uint SmoothGroup;
}
