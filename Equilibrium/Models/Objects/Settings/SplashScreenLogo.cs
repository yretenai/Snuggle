using Equilibrium.Implementations;
using Equilibrium.IO;
using JetBrains.Annotations;

namespace Equilibrium.Models.Objects.Settings {
    [PublicAPI]
    public record SplashScreenLogo(
        PPtr<SerializedObject> Logo,
        float Duration) {
        public static SplashScreenLogo Default { get; } = new(PPtr<SerializedObject>.Null, 0);

        public static SplashScreenLogo FromReader(BiEndianBinaryReader reader, SerializedFile serializedFile) {
            var ptr = PPtr<SerializedObject>.FromReader(reader, serializedFile);
            var duration = reader.ReadSingle();
            return new SplashScreenLogo(ptr, duration);
        }
    }
}
