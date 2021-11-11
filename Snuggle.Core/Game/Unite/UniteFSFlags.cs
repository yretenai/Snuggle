using System;
using JetBrains.Annotations;

namespace Snuggle.Core.Game.Unite {
    [PublicAPI, Flags]
    public enum UniteFSFlags {
        Encrypted = 0x200,
    }
}
