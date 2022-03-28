using Snuggle.Core.IO;
using Snuggle.Core.Meta;

namespace Snuggle.Core.Models.Objects.Settings;

public record OculusSettings(bool SharedDepthBuffer, bool DashSupport, bool LowOverheadMode, bool ProtectedContext, bool V2Signing) {
    public static OculusSettings Default { get; } = new(false, false, false, false, false);

    public static OculusSettings FromReader(BiEndianBinaryReader reader, SerializedFile file) {
        var shared = reader.ReadBoolean();
        var dash = reader.ReadBoolean();
        var lowOverhead = false;
        var protectedContext = false;
        var signing = false;
        if (file.Version >= UnityVersionRegister.Unity2018_4 && file.Version < UnityVersionRegister.Unity2019_1 || file.Version >= UnityVersionRegister.Unity2019_2) {
            lowOverhead = reader.ReadBoolean();
        }

        if (file.Version >= UnityVersionRegister.Unity2018_4 && file.Version < UnityVersionRegister.Unity2019_1 || file.Version >= UnityVersionRegister.Unity2019_3) {
            protectedContext = reader.ReadBoolean();
            signing = reader.ReadBoolean();
        }

        reader.Align();
        return new OculusSettings(shared, dash, lowOverhead, protectedContext, signing);
    }
}
