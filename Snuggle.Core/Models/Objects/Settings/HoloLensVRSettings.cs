using JetBrains.Annotations;
using Snuggle.Core.IO;
using Snuggle.Core.Meta;

namespace Snuggle.Core.Models.Objects.Settings {
    [PublicAPI]
    public record HoloLensVRSettings(
        int DepthFormat,
        bool DepthBufferSharingEnabled) {
        public static HoloLensVRSettings Default { get; } = new(0, false);

        public static HoloLensVRSettings FromReader(BiEndianBinaryReader reader, SerializedFile file) {
            var format = reader.ReadInt32();
            var depthBufferSharingEnabled = false;
            if (file.Version >= UnityVersionRegister.Unity2017_3) {
                depthBufferSharingEnabled = reader.ReadBoolean();
                reader.Align();
            }

            return new HoloLensVRSettings(format, depthBufferSharingEnabled);
        }
    }
}
