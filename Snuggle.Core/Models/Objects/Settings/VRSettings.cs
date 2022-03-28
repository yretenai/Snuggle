using Snuggle.Core.IO;
using Snuggle.Core.Meta;

namespace Snuggle.Core.Models.Objects.Settings;

public record VRSettings(GoogleCardboardSettings Cardboard, GoogleDaydreamSettings Daydream, HoloLensVRSettings HoloLens, LuminSettings Lumin, OculusSettings Oculus, bool Enable360StereoCapture) {
    public static VRSettings Default { get; } = new(GoogleCardboardSettings.Default, GoogleDaydreamSettings.Default, HoloLensVRSettings.Default, LuminSettings.Default, OculusSettings.Default, false);

    public static VRSettings FromReader(BiEndianBinaryReader reader, SerializedFile file) {
        var enable360StereoCapture = true;
        var cardboard = GoogleCardboardSettings.Default;
        var daydream = GoogleDaydreamSettings.Default;
        var hololens = HoloLensVRSettings.Default;
        var lumin = LuminSettings.Default;
        var oculus = OculusSettings.Default;

        if (file.Version < UnityVersionRegister.Unity2020_2) {
            cardboard = GoogleCardboardSettings.FromReader(reader, file);
            daydream = GoogleDaydreamSettings.FromReader(reader, file);
            hololens = HoloLensVRSettings.FromReader(reader, file);

            if (file.Version >= UnityVersionRegister.Unity2018_1 && file.Version < UnityVersionRegister.Unity2018_2) {
                enable360StereoCapture = reader.ReadBoolean();
                reader.Align();
            }

            if (file.Version >= UnityVersionRegister.Unity2019_1) {
                lumin = LuminSettings.FromReader(reader, file);
            }

            if (file.Version >= UnityVersionRegister.Unity2017_3) {
                oculus = OculusSettings.FromReader(reader, file);
            }
        }

        if (file.Version >= UnityVersionRegister.Unity2018_2) {
            enable360StereoCapture = reader.ReadBoolean();
            reader.Align();
        }

        return new VRSettings(cardboard, daydream, hololens, lumin, oculus, enable360StereoCapture);
    }
}
