using System.Collections.Generic;
using Equilibrium.Implementations;
using Equilibrium.IO;
using Equilibrium.Meta;
using Equilibrium.Models.Objects.Math;
using JetBrains.Annotations;

namespace Equilibrium.Models.Objects.Settings {
    [PublicAPI]
    public record SplashScreenSettings(
        ColorRGBA BackgroundColor,
        bool Show,
        bool ShowLogo,
        float OverlayOpacity,
        int Animate,
        int LogoStyle,
        int DrawMode,
        float BackgroundAnimationZoomSpeed,
        float LogoAnimaitonZoomSpeed,
        float BackgroundLandscapeAspectRatio,
        float BackgroundPortraitAspectRatio,
        Rect LandscapeUV,
        Rect PortraitUV,
        List<PPtr<SerializedObject>> Logos,
        PPtr<Texture2D> BackgroundLandscape,
        PPtr<Texture2D> BackgroundPortrait,
        PPtr<Texture2D> VRSplashScreen,
        PPtr<Texture2D> HolographicTrackingLossScreen) {
        public static SplashScreenSettings Default { get; } =
            new(ColorRGBA.Zero,
                true,
                true,
                1,
                1,
                1,
                0,
                1,
                1,
                1,
                1,
                Rect.Zero,
                Rect.Zero,
                new List<PPtr<SerializedObject>>(),
                PPtr<Texture2D>.Null,
                PPtr<Texture2D>.Null,
                PPtr<Texture2D>.Null,
                PPtr<Texture2D>.Null);

        public static SplashScreenSettings FromReader(BiEndianBinaryReader reader, SerializedFile serializedFile) {
            var style = serializedFile.Version >= UnityVersionRegister.Unity5_4 && serializedFile.Version < UnityVersionRegister.Unity5_5 ? reader.ReadInt32() : 0;
            var color = serializedFile.Version >= UnityVersionRegister.Unity5_5 ? reader.ReadStruct<ColorRGBA>() : ColorRGBA.Zero;
            var show = reader.ReadBoolean();
            var logo = serializedFile.Version < UnityVersionRegister.Unity5_5 || reader.ReadBoolean();
            reader.Align();

            var opacity = 0f;
            var animate = 1;
            var mode = 1;
            var bgZoom = 1f;
            var logoZoom = 1f;
            var landscapeAspect = 1f;
            var portraitAspect = 1f;
            Rect landscapeUv;
            Rect portraitUv;
            var logos = new List<PPtr<SerializedObject>>();
            PPtr<Texture2D> backgroundLandscape;
            PPtr<Texture2D> backgroundPortait;
            if (serializedFile.Version >= UnityVersionRegister.Unity5_5) {
                opacity = reader.ReadSingle();
                animate = reader.ReadInt32();
                style = reader.ReadInt32();
                mode = reader.ReadInt32();
                bgZoom = reader.ReadSingle();
                logoZoom = reader.ReadSingle();
                landscapeAspect = reader.ReadSingle();
                portraitAspect = reader.ReadSingle();
                landscapeUv = reader.ReadStruct<Rect>();
                portraitUv = reader.ReadStruct<Rect>();
                var logoCount = reader.ReadInt32();
                logos.EnsureCapacity(logoCount);
                for (var i = 0; i < logoCount; ++i) {
                    logos.Add(PPtr<SerializedObject>.FromReader(reader, serializedFile));
                }

                backgroundLandscape = PPtr<Texture2D>.FromReader(reader, serializedFile);
                backgroundPortait = PPtr<Texture2D>.FromReader(reader, serializedFile);
            } else {
                landscapeUv = Rect.Zero;
                portraitUv = Rect.Zero;
                backgroundLandscape = PPtr<Texture2D>.Null;
                backgroundPortait = PPtr<Texture2D>.Null;
            }

            PPtr<Texture2D> vrSplash = serializedFile.Version >= UnityVersionRegister.Unity5_3 ? PPtr<Texture2D>.FromReader(reader, serializedFile) : PPtr<Texture2D>.Null;
            PPtr<Texture2D> arLoss = serializedFile.Version >= UnityVersionRegister.Unity5_5 ? PPtr<Texture2D>.FromReader(reader, serializedFile) : PPtr<Texture2D>.Null;

            return new SplashScreenSettings(color, show, logo, opacity, animate, style, mode, bgZoom, logoZoom, landscapeAspect, portraitAspect, landscapeUv, portraitUv, logos, backgroundLandscape, backgroundPortait, vrSplash, arLoss);
        }
    }
}
