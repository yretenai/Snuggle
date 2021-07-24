using System;
using JetBrains.Annotations;

namespace Equilibrium.Models.Bundle {
    [PublicAPI, Flags]
    public enum UnityBundleBlockFlags : ushort {
        CompressionMask = 0x3F,
    }
}
