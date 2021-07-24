using System.Collections.Generic;
using System.Collections.Immutable;
using Equilibrium.IO;
using JetBrains.Annotations;

namespace Equilibrium.Models.Serialization {
    [PublicAPI]
    public record UnityScriptInfo {
        public static UnityScriptInfo FromReader(BiEndianBinaryReader reader, UnitySerializedFile header) => new();

        public static ICollection<UnityScriptInfo> ArrayFromReader(BiEndianBinaryReader reader, UnitySerializedFile header) {
            if (header.Version <= UnitySerializedFileVersion.ScriptTypeIndex) {
                return ImmutableList<UnityScriptInfo>.Empty;
            }

            return new List<UnityScriptInfo>();
        }
    }
}
