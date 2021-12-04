using JetBrains.Annotations;

namespace Snuggle.Core.Models.Objects.Graphics;

[PublicAPI]
public enum SpritePackingRotation {
    None = 0,
    FlipHorizontal = 1,
    FlipVertical = 2,
    Rotate180 = 3,
    Rotate90 = 4,
}
