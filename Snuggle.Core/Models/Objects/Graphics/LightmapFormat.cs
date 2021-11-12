using JetBrains.Annotations;

namespace Snuggle.Core.Models.Objects.Graphics;

// ReSharper disable InconsistentNaming
[PublicAPI]
public enum LightmapFormat {
    Unknown = -1,
    Default = 0,
    BakedLightmapDoubleLDR = 1,
    BakedLightmapRGBM = 2,
    NormalMapDXT5NM = 3,
    NormalMapPlain = 4,
    RGBMEncoded = 5,
    AlwaysPadded = 6,
    DoubleLDR = 7,
    BakedLightmapFullHDR = 8,
    RealTimeLightmapRGBM = 9,
}
// ReSharper enable InconsistentNaming
