using System;
using System.Collections.Generic;
using Equilibrium.IO;
using Equilibrium.Meta;
using Equilibrium.Models;
using Equilibrium.Models.Objects.Math;
using Equilibrium.Models.Objects.Settings;
using Equilibrium.Models.Serialization;
using Equilibrium.Options;
using JetBrains.Annotations;

namespace Equilibrium.Implementations {
    [PublicAPI, ObjectImplementation(UnityClassId.PlayerSettings)]
    public class PlayerSettings : SerializedObject {
        public PlayerSettings(BiEndianBinaryReader reader, UnityObjectInfo info, SerializedFile serializedFile) : this(info, serializedFile) {
            IsMutated = false;

            if (SerializedFile.Version >= UnityVersionRegister.Unity5_4) {
                ProductGUID = reader.ReadStruct<Guid>();
            }

            AndroidProfiler = reader.ReadBoolean();

            if (SerializedFile.Version >= UnityVersionRegister.Unity2017_2) {
                AndroidFilterTouchesWhenObscured = reader.ReadBoolean();
            }

            if (SerializedFile.Version >= UnityVersionRegister.Unity2018_1) {
                AndroidEnableSustainedPerformanceMode = reader.ReadBoolean();
            }

            reader.Align();

            DefaultScreenOrientation = reader.ReadInt32();
            TargetDevice = reader.ReadInt32();
            if (SerializedFile.Version < UnityVersionRegister.Unity5_3) {
                TargetResolution = reader.ReadInt32();
            }

            if (SerializedFile.Version >= UnityVersionRegister.Unity5_3) {
                UseOnDemandResources = reader.ReadBoolean();
                reader.Align();
            }

            AccelerometerFrequency = reader.ReadInt32();
            CompanyName = reader.ReadString32();
            ProductName = reader.ReadString32();
            DefaultCursor = PPtr<Texture2D>.FromReader(reader, serializedFile);
            CursorHotspot = reader.ReadStruct<Vector2>();
            SplashScreenSettings = SplashScreenSettings.FromReader(reader, serializedFile);
            DefaultScreenWidth = reader.ReadInt32();
            DefaultScreenHeight = reader.ReadInt32();
            DefaultScreenHeightWeb = reader.ReadInt32();
            DefaultScreenWidthWeb = reader.ReadInt32();
            RenderingPath = reader.ReadInt32();
            if (SerializedFile.Version < UnityVersionRegister.Unity5_5) {
                MobileRenderingPath = reader.ReadInt32();
            }

            ActiveColorSpace = reader.ReadInt32();

            MTRendering = reader.ReadBoolean();
            if (SerializedFile.Version < UnityVersionRegister.Unity2017_2 ||
                SerializedFile.Version >= UnityVersionRegister.Unity2018_4 &&
                SerializedFile.Version < UnityVersionRegister.Unity2019_1 ||
                SerializedFile.Version >= UnityVersionRegister.Unity2019_4) { // xD
                MobileMTRendering = reader.ReadBoolean();
            }

            if (SerializedFile.Version >= UnityVersionRegister.Unity2020_2) {
                AID = reader.ReadArray<byte>(16).ToArray();
                reader.Align();
                PlayerMinOpenGLESVersion = reader.ReadInt32();
            }

            reader.Align();

            if (SerializedFile.Version >= UnityVersionRegister.Unity2020_1) {
                MipStripping = reader.ReadBoolean();
                reader.Align();
                NumberOfMipsStripped = reader.ReadInt32();
            }

            if (SerializedFile.Version < UnityVersionRegister.Unity5_4) {
                Stereoscopic3D = reader.ReadBoolean();
            }

            reader.Align();

            if (SerializedFile.Version >= UnityVersionRegister.Unity5_4) {
                var stackTraceCount = reader.ReadInt32();
                StackTraceTypes = reader.ReadArray<int>(stackTraceCount).ToArray();
            }

            IosShowActivityIndicatorOnLoading = reader.ReadInt32();
            AndroidShowActivityIndicatorOnLoading = reader.ReadInt32();
            if (SerializedFile.Version >= UnityVersionRegister.Unity5_5 &&
                SerializedFile.Version < UnityVersionRegister.Unity2018_2) {
                TizenShowActivityIndicatorOnLoading = reader.ReadInt32();
            }

            if (SerializedFile.Version < UnityVersionRegister.Unity2018_4 || // ??? removed for one version?
                SerializedFile.Version >= UnityVersionRegister.Unity2019_1 && // ??? added for one version?
                SerializedFile.Version < UnityVersionRegister.Unity2019_2) {
                IOSAppInBackgroundBehavior = reader.ReadInt32();
            }

            if (SerializedFile.Version < UnityVersionRegister.Unity2019_3) {
                DisplayResolutionDialog = reader.ReadInt32();
            }

            if (SerializedFile.Version >= UnityVersionRegister.Unity2018_4 &&
                SerializedFile.Version < UnityVersionRegister.Unity2019_1 || // Dear Unity, please stop.
                SerializedFile.Version >= UnityVersionRegister.Unity2019_2) {
                IOSUseCustomAppBackgroundBehavior = reader.ReadBoolean();
            }

            if (SerializedFile.Version >= UnityVersionRegister.Unity5_3) {
                IOSAllowHttpDownload = reader.ReadBoolean();
            }

            AllowedAutorotateToPortrait = reader.ReadBoolean();
            AllowedAutorotateToPortraitUpsideDown = reader.ReadBoolean();
            AllowedAutorotateToLandscapeRight = reader.ReadBoolean();
            AllowedAutorotateToLandscapeLeft = reader.ReadBoolean();
            UseOSAutorotation = reader.ReadBoolean();
            Use32BitDisplayBuffer = reader.ReadBoolean();
            if (SerializedFile.Version >= UnityVersionRegister.Unity2017_3) {
                PreserveFramebufferAlpha = reader.ReadBoolean();
            }

            DisableDepthAndStencilBuffers = reader.ReadBoolean();
            if (SerializedFile.Version >= UnityVersionRegister.Unity2018_3) {
                AndroidStartInFullscreen = reader.ReadBoolean();
                AndroidRenderOutsideSafeArea = reader.ReadBoolean();
            }

            if (SerializedFile.Version >= UnityVersionRegister.Unity2019_2) {
                AndroidUseSwappy = reader.ReadBoolean();
            }

            reader.Align();

            if (SerializedFile.Version >= UnityVersionRegister.Unity2017_2) {
                AndroidBlitType = reader.ReadInt32();
            }

            if (SerializedFile.Version < UnityVersionRegister.Unity2018_1) {
                DefaultIsFullScreen = reader.ReadBoolean();
            }

            DefaultIsNativeResolution = reader.ReadBoolean();
            if (SerializedFile.Version >= UnityVersionRegister.Unity2017_2) {
                MacRetinaSupport = reader.ReadBoolean();
            }

            RunInBackground = reader.ReadBoolean();
            CaptureSingleScreen = reader.ReadBoolean();
            MuteOtherAudioSources = reader.ReadBoolean();
            PrepareIOSForRecording = reader.ReadBoolean();
            if (SerializedFile.Version >= UnityVersionRegister.Unity2017_1) {
                ForceIOSSpeakersWhenRecording = reader.ReadBoolean();
            }

            if (SerializedFile.Version >= UnityVersionRegister.Unity2017_3) {
                reader.Align();
                DeferSystemGesturesMode = reader.ReadInt32();
                HideHomeButton = reader.ReadBoolean();
            }

            SubmitAnalytics = reader.ReadBoolean();
            UsePlayerLog = reader.ReadBoolean();
            BakeCollisionMeshes = reader.ReadBoolean();
            ForceSingleInstance = reader.ReadBoolean();
            UseFlipModelSwapchain = reader.ReadBoolean();
            ResizableWindow = reader.ReadBoolean();
            UseMacAppStoreValidation = reader.ReadBoolean();
            if (SerializedFile.Version >= UnityVersionRegister.Unity2017_1) {
                reader.Align();
                MacAppStoreCategory = reader.ReadString32();
            }

            GPUSkinning = reader.ReadBoolean();
            if (SerializedFile.Version > UnityVersionRegister.Unity5_4 &&
                SerializedFile.Version < UnityVersionRegister.Unity2019_3) {
                GraphicsJobs = reader.ReadBoolean();
            }

            XboxPIXTextureCapture = reader.ReadBoolean();
            XboxEnableAvatar = reader.ReadBoolean();
            XboxEnableKinect = reader.ReadBoolean();
            XboxEnableKinectAutoTracking = reader.ReadBoolean();
            XboxEnableFitness = reader.ReadBoolean();
            VisibleInBackground = reader.ReadBoolean();
            if (SerializedFile.Version >= UnityVersionRegister.Unity5_3) {
                AllowFullscreenSwitch = reader.ReadBoolean();
            }

            reader.Align();

            if (SerializedFile.Version >= UnityVersionRegister.Unity5_6 &&
                SerializedFile.Version < UnityVersionRegister.Unity2019_3) {
                GraphicsJobMode = reader.ReadInt32();
            }

            if (SerializedFile.Version < UnityVersionRegister.Unity2018_1) {
                MacFullscreenMode = reader.ReadInt32();
                if (SerializedFile.Version < UnityVersionRegister.Unity2017_3) {
                    D3D9FullscreenMode = reader.ReadInt32();
                }

                D3D11FullscreenMode = reader.ReadInt32();
            } else {
                FullscreenMode = reader.ReadInt32();
            }

            XboxSpeechDB = reader.ReadUInt32();

            XboxEnableHeadOrientation = reader.ReadBoolean();
            if (SerializedFile.Version >= UnityVersionRegister.Unity5_3) {
                reader.Align(); // ?????
            }

            XboxEnableGuest = reader.ReadBoolean();
            reader.Align();

            if (SerializedFile.Version >= UnityVersionRegister.Unity5_3) {
                XboxEnablePIXSampling = reader.ReadBoolean();
                reader.Align();
            }

            if (SerializedFile.Version >= UnityVersionRegister.Unity2017_2) {
                MetalFramebufferOnly = reader.ReadBoolean();
            }

            if (SerializedFile.Version >= UnityVersionRegister.Unity5_2 &&
                SerializedFile.Version < UnityVersionRegister.Unity2018_3) {
                N3DSDisableStereoscopicView = reader.ReadBoolean();
                N3DSEnableSharedListOpt = reader.ReadBoolean();
                N3DSEnableVSync = reader.ReadBoolean();

                if (SerializedFile.Version >= UnityVersionRegister.Unity5_3) {
                    if (SerializedFile.Version < UnityVersionRegister.Unity5_6) {
                        UIUse16BitDepthBuffer = reader.ReadBoolean();
                    }

                    if (SerializedFile.Version < UnityVersionRegister.Unity2017_3) {
                        IgnoreAlphaClear = reader.ReadBoolean();
                    }
                }
            }

            reader.Align();

            XboxOneResolution = reader.ReadInt32();

            if (SerializedFile.Version >= UnityVersionRegister.Unity2017_3) {
                XboxOneSResolution = reader.ReadInt32();
                XboxOneXResolution = reader.ReadInt32();
            }

            XboxOneMonoLoggingLevel = reader.ReadInt32();
            if (SerializedFile.Version < UnityVersionRegister.Unity5_5) {
                PS3SplashScreen = PPtr<Texture2D>.FromReader(reader, SerializedFile);
            } else {
                XboxOneLoggingLevel = reader.ReadInt32();
            }

            if (SerializedFile.Version >= UnityVersionRegister.Unity2017_1) {
                XboxOneDisableEsram = reader.ReadBoolean();
                reader.Align();

                if (SerializedFile.Version >= UnityVersionRegister.Unity2018_4 &&
                    SerializedFile.Version < UnityVersionRegister.Unity2019_1 ||
                    SerializedFile.Version >= UnityVersionRegister.Unity2019_3) {
                    XboxOneEnableTypeOptimization = reader.ReadBoolean();
                    reader.Align();
                }

                if (SerializedFile.Version >= UnityVersionRegister.Unity2017_2) {
                    XboxOnePresentImmediateThreshold = reader.ReadUInt32();

                    if (SerializedFile.Version >= UnityVersionRegister.Unity2018_1) {
                        SwitchQueueCommandMemory = reader.ReadInt32();
                        if (SerializedFile.Version >= UnityVersionRegister.Unity2018_4) {
                            SwitchQueueControlMemory = reader.ReadInt32();
                            SwitchQueueComputeMemory = reader.ReadInt32();
                            SwitchNVNShaderPoolsGranularity = reader.ReadInt32();
                            SwitchNVNDefaultPoolsGranularity = reader.ReadInt32();
                            SwitchNVNOtherPoolsGranularity = reader.ReadInt32();
                            if (SerializedFile.Version < UnityVersionRegister.Unity2019_1 ||
                                SerializedFile.Version >= UnityVersionRegister.Unity2019_4) {
                                SwitchNVNMaxPublicTextureIdCount = reader.ReadInt32();
                                SwitchNVNMaxPublicSamplerIdCount = reader.ReadInt32();
                            }

                            if (SerializedFile.Version >= UnityVersionRegister.Unity2019_4) {
                                if (SerializedFile.Options.Game != UnityGame.PokemonUnite) {
                                    StadiaPresentMode = reader.ReadInt32();
                                    StadiaTargetFramerate = reader.ReadInt32();
                                }
                            }

                            if (SerializedFile.Version > UnityVersionRegister.Unity2019_3) {
                                VulkanNumSwapchainBuffers = reader.ReadInt32();
                            }
                        }
                    }
                }
            }

            if (SerializedFile.Version < UnityVersionRegister.Unity2018_3) {
                VideoMemoryForVertexBuffers = reader.ReadInt32();
                PSP2PowerMode = reader.ReadInt32();
                PSP2AcquireBGM = reader.ReadBoolean();
                reader.Align();
            }

            if (SerializedFile.Version >= UnityVersionRegister.Unity2018_2) {
                VulkanEnableSetSRGBWrite = reader.ReadBoolean();
                if (SerializedFile.Version < UnityVersionRegister.Unity2018_3) {
                    VulkanUseSWCommandBuffers = reader.ReadBoolean();
                }

                if (SerializedFile.Version >= UnityVersionRegister.Unity2020_2) {
                    VulkanEnablePreTransform = reader.ReadBoolean();
                }

                if (SerializedFile.Version >= UnityVersionRegister.Unity2019_4 &&
                    SerializedFile.Version < UnityVersionRegister.Unity2020_1 ||
                    SerializedFile.Version >= UnityVersionRegister.Unity2020_2) {
                    VulkanEnableLateAcquireNextImage = reader.ReadBoolean();
                }

                reader.Align();
            }

            if (SerializedFile.Version >= UnityVersionRegister.Unity5_2 &&
                SerializedFile.Version < UnityVersionRegister.Unity2018_1) {
                WiiUTVResolution = reader.ReadInt32();
                WiiUGamePadMSAA = reader.ReadInt32();

                WiiUSupportsNunchuk = reader.ReadBoolean();
                WiiUSupportsClassicController = reader.ReadBoolean();
                WiiUSupportsBalanceBoard = reader.ReadBoolean();
                WiiUSupportsMotionPlus = reader.ReadBoolean();
                WiiUSupportsProController = reader.ReadBoolean();
                WiiUAllowScreenCapture = reader.ReadBoolean();
                reader.Align();

                WiiUControllerCount = reader.ReadInt32();
            }

            SupportedAspectRatios = AspectRatios.FromReader(reader, SerializedFile);
            if (SerializedFile.Version < UnityVersionRegister.Unity5_6) {
                BundleIdentifier = reader.ReadString32();
            }

            BundleVersion = reader.ReadString32();

            var preloadAssetCount = reader.ReadInt32();
            PreloadedAssets.AddRange(PPtr<SerializedObject>.ArrayFromReader(reader, SerializedFile, preloadAssetCount));

            if (SerializedFile.Version < UnityVersionRegister.Unity5_5) {
                MetroEnableIndependentInputSource = reader.ReadBoolean();
            } else {
                MetroInputSource = reader.ReadInt32();
                if (SerializedFile.Version >= UnityVersionRegister.Unity2017_3) {
                    WSATransparentSwapChain = reader.ReadBoolean();
                    reader.Align();
                }

                HolographicPauseOnTrackingLoss = reader.ReadBoolean();
            }

            if (SerializedFile.Version < UnityVersionRegister.Unity5_4) {
                MetroEnableLowLatencyPresentationAPI = reader.ReadBoolean();
            }

            XboxOneDisableKinectGpuReservation = reader.ReadBoolean();
            if (SerializedFile.Version >= UnityVersionRegister.Unity5_5) {
                XboxOneEnable7ThCore = reader.ReadBoolean();
                reader.Align();

                if (SerializedFile.Version >= UnityVersionRegister.Unity2018_3 &&
                    SerializedFile.Version < UnityVersionRegister.Unity2019_1) {
                    IsWSAHolographicRemotingEnabled = reader.ReadBoolean();
                    reader.Align();
                }
            }

            reader.Align();

            if (SerializedFile.Version < UnityVersionRegister.Unity5_4) {
                VirtualRealitySupported = reader.ReadBoolean();
            } else {
                if (SerializedFile.Version < UnityVersionRegister.Unity5_5) {
                    SinglePassStereoRendering = reader.ReadBoolean();
                }

                if (SerializedFile.Version >= UnityVersionRegister.Unity5_6) {
                    VRSettings = VRSettings.FromReader(reader, SerializedFile);
                }

                if (SerializedFile.Version >= UnityVersionRegister.Unity2019_1) {
                    IsWSAHolographicRemotingEnabled = reader.ReadBoolean();
                    reader.Align();
                }

                if (SerializedFile.Version < UnityVersionRegister.Unity2019_3) {
                    ProtectGraphicsMemory = reader.ReadBoolean();
                }

                if (SerializedFile.Version >= UnityVersionRegister.Unity2018_3) {
                    EnableFrameTimingStats = reader.ReadBoolean();
                    reader.Align();
                }

                reader.Align();

                if (SerializedFile.Version >= UnityVersionRegister.Unity5_6) {
                    UseHDRDisplay = reader.ReadBoolean();
                    reader.Align();
                    if (SerializedFile.Version >= UnityVersionRegister.Unity2019_3) {
                        D3DHDRBitDepth = reader.ReadInt32();
                    }

                    if (SerializedFile.Version >= UnityVersionRegister.Unity2017_2) {
                        var colorGamutCount = reader.ReadInt32();
                        ColorGamuts = reader.ReadArray<int>(colorGamutCount).ToArray();
                    }
                }
            }

            reader.Align();

            if (SerializedFile.Version >= UnityVersionRegister.Unity5_6) {
                TargetPixelDensity = reader.ReadInt32();
                ResolutionScalingMode = reader.ReadInt32();

                if (SerializedFile.Version >= UnityVersionRegister.Unity2017_2) {
                    AndroidSupportedAspectRatio = reader.ReadInt32();
                    AndroidMaxAspectRatio = reader.ReadSingle();
                }
            }

            if (SerializedFile.Version >= UnityVersionRegister.Unity2020_2) {
                ActiveInputHandler = reader.ReadInt32();
            }

            CloudProjectId = reader.ReadString32();
            if (SerializedFile.Version < UnityVersionRegister.Unity5_2) {
                ProjectId = reader.ReadString32();
            }

            if (SerializedFile.Version >= UnityVersionRegister.Unity2018_3) {
                FramebufferDepthMemorylessMode = reader.ReadInt32();
            }

            if (SerializedFile.Version >= UnityVersionRegister.Unity2020_2) {
                var qualitySettingsNameCount = reader.ReadInt32();
                QualitySettingsNames.EnsureCapacity(qualitySettingsNameCount);
                for (var i = 0; i < qualitySettingsNameCount; ++i) {
                    QualitySettingsNames.Add(reader.ReadString32());
                }
            }

            ProjectName = reader.ReadString32();
            OrganizationId = reader.ReadString32();

            CloudEnabled = reader.ReadBoolean();

            if (SerializedFile.Version >= UnityVersionRegister.Unity5_6 &&
                SerializedFile.Version < UnityVersionRegister.Unity2017_1) {
                EnableNewInputSystem = reader.ReadBoolean();
            }

            if (SerializedFile.Version >= UnityVersionRegister.Unity2017_1 &&
                SerializedFile.Version < UnityVersionRegister.Unity2020_2) {
                DisableOldInputManagerSupport = reader.ReadBoolean();
            }

            if (SerializedFile.Version >= UnityVersionRegister.Unity2018_3) {
                LegacyClampBlendShapeWeights = reader.ReadBoolean();
            }

            if (SerializedFile.Version >= UnityVersionRegister.Unity2020_1) {
                VirtualTexturingSupportEnabled = reader.ReadBoolean();
            }

            reader.Align();
        }

        public PlayerSettings(UnityObjectInfo info, SerializedFile serializedFile) : base(info, serializedFile) {
            CompanyName = string.Empty;
            ProductName = string.Empty;
            DefaultCursor = PPtr<Texture2D>.Null;
            SplashScreenSettings = SplashScreenSettings.Default;
            AID = new byte[16];
            StackTraceTypes = Array.Empty<int>();
            MacAppStoreCategory = string.Empty;
            PS3SplashScreen = PPtr<Texture2D>.Null;
            SupportedAspectRatios = AspectRatios.Default;
            BundleIdentifier = string.Empty;
            BundleVersion = string.Empty;
            VRSettings = VRSettings.Default;
            ColorGamuts = Array.Empty<int>();
            CloudProjectId = string.Empty;
            ProjectId = string.Empty;
            ProjectName = string.Empty;
            OrganizationId = string.Empty;
        }

        public Guid ProductGUID { get; set; }
        public bool AndroidProfiler { get; set; }
        public bool AndroidFilterTouchesWhenObscured { get; set; }
        public bool AndroidEnableSustainedPerformanceMode { get; set; }
        public int DefaultScreenOrientation { get; set; }
        public int TargetDevice { get; set; }
        public int TargetResolution { get; set; }
        public bool UseOnDemandResources { get; set; }
        public int AccelerometerFrequency { get; set; }
        public string CompanyName { get; set; }
        public string ProductName { get; set; }
        public PPtr<Texture2D> DefaultCursor { get; set; }
        public Vector2 CursorHotspot { get; set; }
        public SplashScreenSettings SplashScreenSettings { get; set; }
        public int DefaultScreenWidth { get; set; }
        public int DefaultScreenHeight { get; set; }
        public int DefaultScreenWidthWeb { get; set; }
        public int DefaultScreenHeightWeb { get; set; }
        public int RenderingPath { get; set; }
        public int MobileRenderingPath { get; set; }
        public int ActiveColorSpace { get; set; }
        public bool MTRendering { get; set; }
        public bool MobileMTRendering { get; set; }
        public bool MipStripping { get; set; }
        public int NumberOfMipsStripped { get; set; }
        public byte[] AID { get; set; }
        public int PlayerMinOpenGLESVersion { get; set; }
        public bool Stereoscopic3D { get; set; }
        public int[] StackTraceTypes { get; set; }
        public int IosShowActivityIndicatorOnLoading { get; set; }
        public int AndroidShowActivityIndicatorOnLoading { get; set; }
        public int TizenShowActivityIndicatorOnLoading { get; set; }
        public int IOSAppInBackgroundBehavior { get; set; }
        public int DisplayResolutionDialog { get; set; }
        public bool IOSUseCustomAppBackgroundBehavior { get; set; }
        public bool IOSAllowHttpDownload { get; set; }
        public bool AllowedAutorotateToPortrait { get; set; }
        public bool AllowedAutorotateToPortraitUpsideDown { get; set; }
        public bool AllowedAutorotateToLandscapeRight { get; set; }
        public bool AllowedAutorotateToLandscapeLeft { get; set; }
        public bool UseOSAutorotation { get; set; }
        public bool Use32BitDisplayBuffer { get; set; }
        public bool PreserveFramebufferAlpha { get; set; }
        public bool DisableDepthAndStencilBuffers { get; set; }
        public bool AndroidStartInFullscreen { get; set; }
        public bool AndroidRenderOutsideSafeArea { get; set; }
        public bool AndroidUseSwappy { get; set; }
        public int AndroidBlitType { get; set; }
        public bool DefaultIsFullScreen { get; set; }
        public bool DefaultIsNativeResolution { get; set; }
        public bool MacRetinaSupport { get; set; }
        public bool RunInBackground { get; set; }
        public bool CaptureSingleScreen { get; set; }
        public bool MuteOtherAudioSources { get; set; }
        public bool PrepareIOSForRecording { get; set; }
        public bool ForceIOSSpeakersWhenRecording { get; set; }
        public int DeferSystemGesturesMode { get; set; }
        public bool HideHomeButton { get; set; }
        public bool SubmitAnalytics { get; set; }
        public bool UsePlayerLog { get; set; }
        public bool BakeCollisionMeshes { get; set; }
        public bool ForceSingleInstance { get; set; }
        public bool UseFlipModelSwapchain { get; set; }
        public bool ResizableWindow { get; set; }
        public bool UseMacAppStoreValidation { get; set; }
        public string MacAppStoreCategory { get; set; }
        public bool GPUSkinning { get; set; }
        public bool GraphicsJobs { get; set; }
        public bool XboxPIXTextureCapture { get; set; }
        public bool XboxEnableAvatar { get; set; }
        public bool XboxEnableKinect { get; set; }
        public bool XboxEnableKinectAutoTracking { get; set; }
        public bool XboxEnableFitness { get; set; }
        public bool VisibleInBackground { get; set; }
        public bool AllowFullscreenSwitch { get; set; }
        public int GraphicsJobMode { get; set; }
        public int MacFullscreenMode { get; set; }
        public int D3D9FullscreenMode { get; set; }
        public int D3D11FullscreenMode { get; set; }
        public int FullscreenMode { get; set; }
        public uint XboxSpeechDB { get; set; }
        public bool XboxEnableHeadOrientation { get; set; }
        public bool XboxEnableGuest { get; set; }
        public bool XboxEnablePIXSampling { get; set; }
        public bool MetalFramebufferOnly { get; set; }
        public bool N3DSDisableStereoscopicView { get; set; }
        public bool N3DSEnableSharedListOpt { get; set; }
        public bool N3DSEnableVSync { get; set; }
        public bool UIUse16BitDepthBuffer { get; set; }
        public bool IgnoreAlphaClear { get; set; }
        public int XboxOneResolution { get; set; }
        public int XboxOneSResolution { get; set; }
        public int XboxOneXResolution { get; set; }
        public int XboxOneMonoLoggingLevel { get; set; }
        public int XboxOneLoggingLevel { get; set; }
        public bool XboxOneDisableEsram { get; set; }
        public bool XboxOneEnableTypeOptimization { get; set; }
        public uint XboxOnePresentImmediateThreshold { get; set; }
        public int SwitchQueueCommandMemory { get; set; }
        public int SwitchQueueControlMemory { get; set; }
        public int SwitchQueueComputeMemory { get; set; }
        public int SwitchNVNShaderPoolsGranularity { get; set; }
        public int SwitchNVNDefaultPoolsGranularity { get; set; }
        public int SwitchNVNOtherPoolsGranularity { get; set; }
        public int SwitchNVNMaxPublicTextureIdCount { get; set; }
        public int SwitchNVNMaxPublicSamplerIdCount { get; set; }
        public int StadiaPresentMode { get; set; }
        public int StadiaTargetFramerate { get; set; }
        public int VulkanNumSwapchainBuffers { get; set; }
        public PPtr<Texture2D> PS3SplashScreen { get; set; }
        public int VideoMemoryForVertexBuffers { get; set; }
        public int PSP2PowerMode { get; set; }
        public bool PSP2AcquireBGM { get; set; }
        public bool VulkanEnableSetSRGBWrite { get; set; }
        public bool VulkanEnableLateAcquireNextImage { get; set; }
        public bool VulkanUseSWCommandBuffers { get; set; }
        public bool VulkanEnablePreTransform { get; set; }
        public int WiiUTVResolution { get; set; }
        public int WiiUGamePadMSAA { get; set; }
        public bool WiiUSupportsNunchuk { get; set; }
        public bool WiiUSupportsClassicController { get; set; }
        public bool WiiUSupportsBalanceBoard { get; set; }
        public bool WiiUSupportsMotionPlus { get; set; }
        public bool WiiUSupportsProController { get; set; }
        public bool WiiUAllowScreenCapture { get; set; }
        public int WiiUControllerCount { get; set; }
        public AspectRatios SupportedAspectRatios { get; set; }
        public string BundleIdentifier { get; set; }
        public string BundleVersion { get; set; }
        public List<PPtr<SerializedObject>> PreloadedAssets { get; set; } = new();
        public bool MetroEnableIndependentInputSource { get; set; }
        public int MetroInputSource { get; set; }
        public bool WSATransparentSwapChain { get; set; }
        public bool HolographicPauseOnTrackingLoss { get; set; }
        public bool MetroEnableLowLatencyPresentationAPI { get; set; }
        public bool XboxOneDisableKinectGpuReservation { get; set; }
        public bool XboxOneEnable7ThCore { get; set; }
        public bool IsWSAHolographicRemotingEnabled { get; set; }
        public VRSettings VRSettings { get; set; }
        public bool VirtualRealitySupported { get; set; }
        public bool SinglePassStereoRendering { get; set; }
        public bool ProtectGraphicsMemory { get; set; }
        public bool EnableFrameTimingStats { get; set; }
        public bool UseHDRDisplay { get; set; }
        public int D3DHDRBitDepth { get; set; }
        public int[] ColorGamuts { get; set; }
        public int TargetPixelDensity { get; set; }
        public int ResolutionScalingMode { get; set; }
        public int AndroidSupportedAspectRatio { get; set; }
        public float AndroidMaxAspectRatio { get; set; }
        public int ActiveInputHandler { get; set; }
        public string CloudProjectId { get; set; }
        public string ProjectId { get; set; }
        public int FramebufferDepthMemorylessMode { get; set; }
        public List<string> QualitySettingsNames { get; set; } = new();
        public string ProjectName { get; set; }
        public string OrganizationId { get; set; }
        public bool CloudEnabled { get; set; }
        public bool EnableNewInputSystem { get; set; }
        public bool DisableOldInputManagerSupport { get; set; }
        public bool LegacyClampBlendShapeWeights { get; set; }
        public bool VirtualTexturingSupportEnabled { get; set; }

        public override void Serialize(BiEndianBinaryWriter writer, AssetSerializationOptions options) {
            base.Serialize(writer, options);
            throw new NotSupportedException();
        }
    }
}
