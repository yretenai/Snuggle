using JetBrains.Annotations;
using Snuggle.Core.Implementations;
using Snuggle.Core.IO;

namespace Snuggle.Core.Models.Objects.Settings; 

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