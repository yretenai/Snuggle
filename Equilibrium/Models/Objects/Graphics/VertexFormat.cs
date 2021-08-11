using JetBrains.Annotations;

namespace Equilibrium.Models.Objects.Graphics {
    [PublicAPI]
    public enum VertexFormat : byte {
        Single = 0,
        Half = 1,
        Color = 2,
        UNorm8 = 3,
        SNorm8 = 4,
        UNorm16 = 5,
        SNorm16 = 6,
        UInt8 = 7,
        SInt8 = 8,
        UInt16 = 9,
        SInt16 = 10,
        UInt32 = 11,
        SInt32 = 12,
    }
}
