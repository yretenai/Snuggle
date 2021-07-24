using System.Collections.Generic;
using Equilibrium.IO;

namespace Equilibrium.Models.Serialization {
    public record UnityExternalInfo {
        public static UnityExternalInfo FromReader(BiEndianBinaryReader reader, UnitySerializedFile header) => new();

        public static ICollection<UnityExternalInfo> ArrayFromReader(BiEndianBinaryReader reader, UnitySerializedFile header) => new List<UnityExternalInfo>();
    }
}
