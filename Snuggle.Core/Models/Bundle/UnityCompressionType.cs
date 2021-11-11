using JetBrains.Annotations;

namespace Snuggle.Core.Models.Bundle {
    [PublicAPI]
    public enum UnityCompressionType : uint {
        None,
        LZMA,
        LZ4,
        LZ4HC,
    }
}
