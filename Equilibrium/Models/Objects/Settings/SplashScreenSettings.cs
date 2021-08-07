using System.Collections.Generic;
using Equilibrium.Implementations;
using Equilibrium.IO;
using Equilibrium.Models.Objects.Math;
using JetBrains.Annotations;

namespace Equilibrium.Models.Objects.Settings {
    [PublicAPI]
    public record SplashScreenSettings(
        ColorRGBA BackgroundColor,
        bool Show,
        bool ShowLogo,
        float OverlayOpacity,
        bool Animate,
        int LogoStyle,
        int DrawMode,
        float BackgroundAnimationZoomSpeed,
        float LogoAnimaitonZoomSpeed,
        float BackgroundLandscapeAspectRatio,
        float BackgroundPortraitAspectRatio,
        SplashScreenUV LandscapeUV,
        SplashScreenUV PortraitUV,
        List<PPtr<SerializedObject>> Logos) {
        public static SplashScreenSettings Default { get; } = new(ColorRGBA.Zero, true, true, 1, true, 1, 0, 1, 1, 1, 1, SplashScreenUV.Default, SplashScreenUV.Default, new List<PPtr<SerializedObject>>());

        public static SplashScreenSettings FromReader(BiEndianBinaryReader reader, SerializedFile serializedFile) {
            var color = reader.ReadStruct<ColorRGBA>();
            var show = reader.ReadBoolean();
            var logo = reader.ReadBoolean();
            reader.Align();
            var opacity = reader.ReadSingle();
            var animate = reader.ReadBoolean();
            reader.Align();
            var style = reader.ReadInt32();
            var mode = reader.ReadInt32();
            var bgZoom = reader.ReadSingle();
            var logoZoom = reader.ReadSingle();
            var landscapeAspect = reader.ReadSingle();
            var portraitAspect = reader.ReadSingle();
            var landscapeUv = SplashScreenUV.FromReader(reader, serializedFile);
            var portraitUv = SplashScreenUV.FromReader(reader, serializedFile);
            var logoCount = reader.ReadInt32();
            var logos = new List<PPtr<SerializedObject>>();
            logos.EnsureCapacity(logoCount);
            for (var i = 0; i < logoCount; ++i) {
                logos.Add(PPtr<SerializedObject>.FromReader(reader, serializedFile));
            }

            return new SplashScreenSettings(color, show, logo, opacity, animate, style, mode, bgZoom, logoZoom, landscapeAspect, portraitAspect, landscapeUv, portraitUv, logos);
        }
    }
}
