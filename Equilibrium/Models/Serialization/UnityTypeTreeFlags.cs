using System;
using JetBrains.Annotations;

namespace Equilibrium.Models.Serialization {
    [PublicAPI, Flags]
    public enum UnityTypeTreeFlags : uint {
        IsArray = 1,
    }
}
