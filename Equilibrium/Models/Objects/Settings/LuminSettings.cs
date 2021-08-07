using Equilibrium.IO;
using JetBrains.Annotations;

namespace Equilibrium.Models.Objects.Settings {
    [PublicAPI]
    public record LuminSettings(
        int DepthFormat,
        int FrameTiming,
        bool EnableGLCache,
        uint GLCacheMaxBlobSize,
        uint GLCacheMaxFileSize) {
        public static LuminSettings Default { get; } = new(0, 0, false, 0, 0);

        public static LuminSettings FromReader(BiEndianBinaryReader reader, SerializedFile file) {
            var depth = reader.ReadInt32();
            var timing = reader.ReadInt32();
            var cache = reader.ReadBoolean();
            reader.Align();
            var blob = reader.ReadUInt32();
            var size = reader.ReadUInt32();
            return new LuminSettings(depth, timing, cache, blob, size);
        }
    }
}
