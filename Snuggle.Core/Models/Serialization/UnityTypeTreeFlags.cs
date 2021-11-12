using System;
using JetBrains.Annotations;

namespace Snuggle.Core.Models.Serialization;

[PublicAPI]
[Flags]
public enum UnityTypeTreeFlags : uint {
    None = 0x0,
    Ignored = 0x1,
    Boolean = 0x100, // one bit value.
    AlignValue = 0x4000, // on primitives 
    AlignStructure = 0x8000, // on structures containing aligned primitives
}
