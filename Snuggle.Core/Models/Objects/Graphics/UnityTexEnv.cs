using JetBrains.Annotations;
using Snuggle.Core.Implementations;
using Snuggle.Core.IO;
using Snuggle.Core.Models.Objects.Math;

namespace Snuggle.Core.Models.Objects.Graphics {
    [PublicAPI]
    public record UnityTexEnv(
        PPtr<Texture> Texture,
        Vector2 Scale,
        Vector2 Offset) {
        public static UnityTexEnv Default { get; } = new(PPtr<Texture>.Null, Vector2.Zero, Vector2.Zero);

        public static UnityTexEnv FromReader(BiEndianBinaryReader reader, SerializedFile file) {
            var texture = PPtr<Texture>.FromReader(reader, file);
            var scale = reader.ReadStruct<Vector2>();
            var offset = reader.ReadStruct<Vector2>();

            return new UnityTexEnv(texture, scale, offset);
        }
    }
}
