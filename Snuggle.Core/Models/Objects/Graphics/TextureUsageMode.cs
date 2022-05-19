namespace Snuggle.Core.Models.Objects.Graphics;

public enum TextureUsageMode {
    Default = 0,
    BakedLightmapDoubleLDR = 1,
    BakedLightmapRGBM = 2,
    NormalmapDXT5nm = 3,
    NormalmapPlain = 4,
    RGBMEncoded = 5,
    AlwaysPadded = 6,
    DoubleLDR = 7,
    BakedLightmapFullHDR = 8,
    RealtimeLightmapRGBM = 9,
    NormalmapASTCnm = 10,
    SingleChannelRed = 11,
    SingleChannelAlpha = 12,
}
