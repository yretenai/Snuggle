using Equilibrium.IO;
using Equilibrium.Meta;
using JetBrains.Annotations;

namespace Equilibrium.Models.Objects.Settings {
    [PublicAPI]
    public record SplashScreenUV(float X, float Y, float Width, float Height) {
        public static SplashScreenUV Default { get; } = new(0, 0, 1, 1);

        // version 2, if you find version 1 please tell me the unity verison.
        public static SplashScreenUV FromReader(BiEndianBinaryReader reader, SerializedFile file) {
            var x = reader.ReadSingle();
            var y = reader.ReadSingle();
            var w = reader.ReadSingle();
            var h = reader.ReadSingle();
            return new SplashScreenUV(x, y, w, h);
        }

        public void ToWriter(BiEndianBinaryWriter writer, SerializedFile serializedFile, UnityVersion targetVersion) {
            writer.Write(X);
            writer.Write(Y);
            writer.Write(Width);
            writer.Write(Height);
        }
    }
}
