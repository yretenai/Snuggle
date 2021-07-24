using System;
using JetBrains.Annotations;

namespace Equilibrium.Models.Serialization {
    [PublicAPI, Flags]
    public enum UnityTypeTreeMetaFlags : uint {
        Align = 0x4000,
    }
}
