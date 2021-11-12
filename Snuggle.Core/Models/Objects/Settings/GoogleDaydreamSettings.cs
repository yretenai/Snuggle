using JetBrains.Annotations;
using Snuggle.Core.IO;
using Snuggle.Core.Meta;

namespace Snuggle.Core.Models.Objects.Settings; 

[PublicAPI]
public record GoogleDaydreamSettings(
    int DepthFormat,
    bool UseSustainedPerformanceMode,
    bool EnableVideoLayer,
    bool UseProtectedVideoMemory,
    int MinimumSupportedHeadTracking,
    int MaximumSupportedHeadTracking) {
    public static GoogleDaydreamSettings Default { get; } = new(0, false, false, false, 0, 0);

    public static GoogleDaydreamSettings FromReader(BiEndianBinaryReader reader, SerializedFile file) {
        var format = reader.ReadInt32();
        var performace = reader.ReadBoolean();
        var video = false;
        var protectedMemory = false;
        if (file.Version >= UnityVersionRegister.Unity2017_2) {
            video = reader.ReadBoolean();
            protectedMemory = reader.ReadBoolean();
        }

        reader.Align();
        var minimum = 0;
        var maximum = 0;
        if (file.Version >= UnityVersionRegister.Unity2017_3) {
            minimum = reader.ReadInt32();
            maximum = reader.ReadInt32();
        }

        return new GoogleDaydreamSettings(format, performace, video, protectedMemory, minimum, maximum);
    }
}