using JetBrains.Annotations;

namespace Equilibrium.Models.Bundle {
    [PublicAPI]
    public record UnityBundle(
        string Signature,
        int FormatVersion,
        string EngineVersion,
        string EngineRevision) {
        public UnityFormat Format { get; } = (UnityFormat) Signature[5];

        public static UnityBundle FromReader(BiEndianBinaryReader reader) {
            return new(
                reader.ReadNullString(),
                reader.ReadInt32(),
                reader.ReadNullString(),
                reader.ReadNullString());
        }
    }
}
