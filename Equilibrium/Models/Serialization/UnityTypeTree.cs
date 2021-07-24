using System;
using System.Collections.Generic;
using Equilibrium.IO;

namespace Equilibrium.Models.Serialization {
    public record UnityTypeTree(
        bool Enabled,
        IEnumerable<UnityType> Types) {
        public static UnityTypeTree FromReader(BiEndianBinaryReader reader, UnitySerializedFile header) => new(false, ArraySegment<UnityType>.Empty);
    }

    public record UnityType { }
}
