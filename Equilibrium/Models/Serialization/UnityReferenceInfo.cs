using System.Collections.Generic;
using System.Collections.Immutable;
using Equilibrium.IO;

namespace Equilibrium.Models.Serialization {
    public record UnityReferenceInfo {
        public static UnityReferenceInfo FromReader(BiEndianBinaryReader reader, UnitySerializedFile header) => new();

        public static ICollection<UnityReferenceInfo> ArrayFromReader(BiEndianBinaryReader reader, UnitySerializedFile header) {
            if (header.Version < UnitySerializedFileVersion.RefObject) {
                return ImmutableList<UnityReferenceInfo>.Empty;
            }

            return new List<UnityReferenceInfo>();
        }
    }
}
