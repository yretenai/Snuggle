using JetBrains.Annotations;

namespace Equilibrium.Models.Bundle {
    [PublicAPI]
    public enum UnityFSCompressionType : uint {
        None,
        LZMA,
        LZ4,
        LZ4HC,
    }
}
