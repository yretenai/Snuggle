using System;
using JetBrains.Annotations;

namespace Equilibrium.Models.Bundle {
    [PublicAPI, Flags]
    public enum UnityBundleBlockInfoFlags : ushort {
        CompressionMask = 0x3F,
    }
}
