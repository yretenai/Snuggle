using System;
using JetBrains.Annotations;

namespace Equilibrium.Game.Unite {
    [PublicAPI, Flags]
    public enum UniteFSFlags {
        Encrypted = 0x200,
    }
}
