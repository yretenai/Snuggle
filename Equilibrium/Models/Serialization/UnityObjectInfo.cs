using System.Collections.Generic;
using Equilibrium.IO;
using JetBrains.Annotations;

namespace Equilibrium.Models.Serialization {
    [PublicAPI]
    public record UnityObjectInfo {
        public static UnityObjectInfo FromReader(BiEndianBinaryReader reader, UnitySerializedFile header) => new();

        public static ICollection<UnityObjectInfo> ArrayFromReader(BiEndianBinaryReader reader, UnitySerializedFile header) => new List<UnityObjectInfo>();
    }
}
