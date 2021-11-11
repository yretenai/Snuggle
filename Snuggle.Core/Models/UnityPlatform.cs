using JetBrains.Annotations;

namespace Snuggle.Core.Models {
    [PublicAPI]
    public enum UnityPlatform {
        NoTarget = -2,
        Unknown = -1,
        Editor = 0,
        DashboardWidget = 1,
        StandaloneOSX = 2,
        StandaloneOSXPPC = 3,
        StandaloneOSXIntel = 4,
        StandaloneWindows,
        WebPlayer,
        WebPlayerStreamed,
        Wii = 8,

        // ReSharper disable once InconsistentNaming
        iOS = 9,
        PS3,

        // ReSharper disable once InconsistentNaming
        XBOX360,
        Android = 13,
        StandaloneGLESEmu = 14,
        NaCl = 16,
        StandaloneLinux = 17,
        FlashPlayer = 18,
        StandaloneWindows64 = 19,
        WebGL,
        WSAPlayer,
        StandaloneLinux64 = 24,
        StandaloneLinuxUniversal,
        WP8Player,
        StandaloneOSXIntel64,
        BlackBerry,
        Tizen,
        PSP2,
        PS4,
        PSM,
        XboxOne,
        SamsungTV,
        N3DS,
        WiiU,

        // ReSharper disable once InconsistentNaming
        tvOS,
        Switch,
        Lumin,
        Stadia,
        CloudRendering,
        GameCoreXboxSeries,
        GameCoreXboxOne,
        PS5,
    }
}
