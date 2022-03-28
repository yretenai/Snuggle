using DragonLib.IO;

namespace Snuggle.Core.Models.Objects.Graphics;

public record struct SpriteSettings {
    public static SpriteSettings Default { get; } = new() { Packed = 0, Mode = SpritePackingMode.Tight, Rotation = SpritePackingRotation.None, Type = SpriteMeshType.FullRectangle };

    [BitField(1)]
    public byte Packed { get; set; }

    [BitField(1)]
    public SpritePackingMode Mode { get; set; }

    [BitField(4)]
    public SpritePackingRotation Rotation { get; set; }

    [BitField(1)]
    public SpriteMeshType Type { get; set; }
}
