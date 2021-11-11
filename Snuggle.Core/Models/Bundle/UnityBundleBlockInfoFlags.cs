using System;
using JetBrains.Annotations;

namespace Snuggle.Core.Models.Bundle {
    [PublicAPI, Flags]
    public enum UnityBundleBlockInfoFlags : ushort {
        CompressionMask = 0x3F,
    }
}
