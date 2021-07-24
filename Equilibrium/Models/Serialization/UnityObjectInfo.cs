using System.Collections.Generic;
using Equilibrium.IO;

namespace Equilibrium.Models.Serialization {
    public record UnityObjectInfo {
        public static UnityObjectInfo FromReader(BiEndianBinaryReader reader, UnitySerializedFile header) => new();

        public static ICollection<UnityObjectInfo> ArrayFromReader(BiEndianBinaryReader reader, UnitySerializedFile header) => new List<UnityObjectInfo>();
    }
}
