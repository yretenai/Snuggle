using JetBrains.Annotations;

namespace Snuggle.Core.Models.Objects.Graphics;

[PublicAPI]
public enum SpriteMaskInteraction {
    None = 0,
    VisibleInsideMask = 1,
    VisibleOutsideMask = 2,
}
