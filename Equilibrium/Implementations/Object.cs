using Equilibrium.IO;
using Equilibrium.Models.IO;
using JetBrains.Annotations;

namespace Equilibrium.Implementations {
    [PublicAPI]
    public class Object : ISerialized {
        public Object(BiEndianBinaryReader reader) { }
        public SerializedFile SerializedFile { get; init; }
        public long PathId { get; init; }
        public virtual bool ShouldDeserialize { get; set; }

        public virtual void Deserialize(BiEndianBinaryReader reader) {
            ShouldDeserialize = false;
        }
    }
}
