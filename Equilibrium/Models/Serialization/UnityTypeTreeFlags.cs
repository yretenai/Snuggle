using System;
using JetBrains.Annotations;

namespace Equilibrium.Models.Serialization {
    [PublicAPI, Flags]
    public enum UnityTypeTreeFlags : uint {
        None = 0x0,
        Array = 0x1,
        Unknown2 = 0x2,
        Unknown3 = 0x4,
        Unknown4 = 0x8,
        Unknown5 = 0x10,
        Unknown6 = 0x20,
        Unknown7 = 0x40,
        Unknown8 = 0x80,
        Unknown9 = 0x100,
        Unknown10 = 0x200,
        Unknown11 = 0x400,
        Unknown12 = 0x800,
        Unknown13 = 0x1000,
        Unknown14 = 0x2000,
        Unknown15 = 0x4000,
        Unknown16 = 0x8000,
        Unknown17 = 0x10000,
        Unknown18 = 0x20000,
        Unknown19 = 0x40000,
        Unknown20 = 0x80000,
        Unknown21 = 0x100000,
        Unknown22 = 0x200000,
        Unknown23 = 0x400000,
        Unknown24 = 0x800000,
        Unknown25 = 0x1000000,
        Unknown26 = 0x2000000,
        Unknown27 = 0x4000000,
        Unknown28 = 0x8000000,
        Unknown29 = 0x10000000,
        Unknown30 = 0x20000000,
        Unknown31 = 0x40000000,
        Unknown32 = 0x80000000,
    }
}
