using JetBrains.Annotations;

namespace Snuggle.Core.Models.Objects.Graphics;

[PublicAPI]
public enum TextureWrapMode {
    Repeat = 0,
    Clamp = 1,
    Mirror = 2,
    MirrorOnce = 3,
}
