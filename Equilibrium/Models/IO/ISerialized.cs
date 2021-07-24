using Equilibrium.IO;
using JetBrains.Annotations;

namespace Equilibrium.Models.IO {
    [PublicAPI]
    public interface ISerialized {
        public SerializedFile SerializedFile { get; init; }
        public long PathId { get; init; }
        public bool ShouldDeserialize { get; set; }

        public void Deserialize(BiEndianBinaryReader reader);

        public void Deserialize() {
            using var reader = BiEndianBinaryReader.FromSpan(SerializedFile.OpenFile(PathId), SerializedFile.Header.IsBigEndian);
            Deserialize(reader);
        }
    }
}
