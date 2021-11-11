using System;
using JetBrains.Annotations;

namespace Snuggle.Core.Models.Bundle {
    [PublicAPI, Flags]
    public enum UnityFSFlags : uint {
        CompressionRange = 0x3F,
        CombinedData = 0x40,
        BlocksInfoAtEnd = 0x80,
    }
}
