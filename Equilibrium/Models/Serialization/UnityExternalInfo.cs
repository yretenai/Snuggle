using System.Collections.Generic;
using Equilibrium.IO;
using JetBrains.Annotations;

namespace Equilibrium.Models.Serialization {
    [PublicAPI]
    public record UnityExternalInfo {
        public static UnityExternalInfo FromReader(BiEndianBinaryReader reader, UnitySerializedFile header) => new();

        public static ICollection<UnityExternalInfo> ArrayFromReader(BiEndianBinaryReader reader, UnitySerializedFile header) => new List<UnityExternalInfo>();
    }
}
