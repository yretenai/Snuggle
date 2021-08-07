using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        // There are a billion and one settings.
        // I have no idea which version is which.
        public PlayerSettings(BiEndianBinaryReader reader, UnityObjectInfo info, SerializedFile serializedFile) : this(info, serializedFile) {
            IsMutated = false;

            try {
                if (serializedFile.Version == UnityVersionRegister.Unity5_4 && serializedFile.Version.Type != UnityBuildType.Beta ||
                    serializedFile.Version > UnityVersionRegister.Unity5_4) {
                    ProjectGuid = reader.ReadStruct<Guid>();
                }

                AndroidProfiler = reader.ReadBoolean();
                AndroidFilterTouchesWhenObscured = reader.ReadBoolean();
                AndroidEnableSustainedPerformanceMode = reader.ReadBoolean();
                reader.Align();

                DefaultScreenOrientation = reader.ReadInt32();
                TargetDevice = reader.ReadInt32();

                if (serializedFile.Version >= UnityVersionRegister.Unity5_3) {
                    UseOnDemandResources = reader.ReadByte();
                } else {
                    UseOnDemandResources = reader.ReadInt32();
                }

                reader.Align();

                AccelerometerFrequency = reader.ReadInt32();

                CompanyName = reader.ReadString32();
                ProductName = reader.ReadString32();
                DefaultCursor = PPtr<SerializedObject>.FromReader(reader, serializedFile);
                CursorHotspot = reader.ReadStruct<Vector2>();

                SplashScreenSettings = SplashScreenSettings.FromReader(reader, serializedFile);

                reader.BaseStream.Seek(4, SeekOrigin.Current);
                // hackfix to skip weird data
                while (reader.Unconsumed > 4) {
                    if (reader.ReadInt32() != 0) {
                        break;
                    }
                }

                if (reader.Unconsumed < 4) {
                    return;
                }

                reader.BaseStream.Seek(-4, SeekOrigin.Current);

                DefaultScreenWidth = reader.ReadInt32();
                DefaultScreenHeight = reader.ReadInt32();
                DefaultScreenWidthWeb = reader.ReadInt32();
                DefaultScreenHeightWeb = reader.ReadInt32();

                // hackfix to skip variant data
                while (reader.Unconsumed > 4) {
                    if (reader.ReadInt32() == 1818391920) {
                        break;
                    }
                }

                if (reader.Unconsumed < 4) {
                    return;
                }

                reader.BaseStream.Seek(-8, SeekOrigin.Current);

                MacAppStoreCategory = reader.ReadString32();

                // This is a dirty hackfix to get this string. A lot of stuff in this range changes a lot.
                while (reader.Unconsumed > 4) {
                    var cursor = reader.BaseStream.Position;
                    var size = reader.ReadInt32();
                    if (size is > 1 and < 0x100) {
                        reader.BaseStream.Seek(cursor, SeekOrigin.Begin);
                        var str = reader.ReadString32();
                        if (str.All(x => char.IsPunctuation(x) || char.IsWhiteSpace(x) || char.IsLetterOrDigit(x))) {
                            reader.BaseStream.Seek(cursor, SeekOrigin.Begin);
                            break;
                        }

                        reader.BaseStream.Seek(cursor + 4, SeekOrigin.Begin);
                    }
                }

                if (reader.Unconsumed <= 4) {
                    return;
                }

                BundleVersion = reader.ReadString32();

                var preloadAssetCount = reader.ReadInt32();
                PreloadedAssets.EnsureCapacity(preloadAssetCount);
                for (var i = 0; i < preloadAssetCount; ++i) {
                    PreloadedAssets.Add(PPtr<SerializedObject>.FromReader(reader, serializedFile));
                }

                // and another one.
                while (reader.Unconsumed > 4) {
                    if (reader.ReadInt32() == 1074161254) {
                        break;
                    }
                }

                if (reader.Unconsumed <= 4) {
                    return;
                }

                if (reader.ReadInt32() != 0) {
                    reader.BaseStream.Seek(-4, SeekOrigin.Current);
                }

                ApplicationId = reader.ReadString32();
                BuildNumber = reader.ReadInt32();
                if (serializedFile.Version >= UnityVersionRegister.Unity2020) {
                    var graphicsPresetNameCount = reader.ReadInt32();
                    GraphicsPresetNames.EnsureCapacity(graphicsPresetNameCount);
                    for (var i = 0; i < graphicsPresetNameCount; ++i) {
                        GraphicsPresetNames.Add(reader.ReadString32());
                    }
                }

                DisplayName = reader.ReadString32();
            } catch (Exception e) {
                serializedFile.Options.Logger.Error("PlayerSettings", "Failed loading PlayerSettings, this is normal", e);
            }
        }

        public PlayerSettings(UnityObjectInfo info, SerializedFile serializedFile) : base(info, serializedFile) {
            CompanyName = string.Empty;
            ProductName = string.Empty;
            DefaultCursor = PPtr<SerializedObject>.Null;
            CursorHotspot = Vector2.Zero;
            SplashScreenSettings = SplashScreenSettings.Default;
            MacAppStoreCategory = string.Empty;
            BundleVersion = string.Empty;
            PreloadedAssets = new List<PPtr<SerializedObject>>();
            ApplicationId = string.Empty;
            GraphicsPresetNames = new List<string>();
            DisplayName = string.Empty;
        }

        public bool AndroidProfiler { get; set; }
        public bool AndroidFilterTouchesWhenObscured { get; set; }
        public bool AndroidEnableSustainedPerformanceMode { get; set; }
        public int DefaultScreenOrientation { get; set; }
        public int TargetDevice { get; set; }
        public int UseOnDemandResources { get; set; }
        public int AccelerometerFrequency { get; set; }
        public string CompanyName { get; set; }
        public string ProductName { get; set; }
        public PPtr<SerializedObject> DefaultCursor { get; set; }
        public Vector2 CursorHotspot { get; set; }
        public SplashScreenSettings SplashScreenSettings { get; set; }
        public int DefaultScreenWidth { get; set; }
        public int DefaultScreenHeight { get; set; }
        public int DefaultScreenWidthWeb { get; set; }
        public int DefaultScreenHeightWeb { get; set; }
        public string MacAppStoreCategory { get; set; }
        public string BundleVersion { get; set; }
        public List<PPtr<SerializedObject>> PreloadedAssets { get; set; }
        public string ApplicationId { get; set; }
        public int BuildNumber { get; set; }
        public List<string> GraphicsPresetNames { get; set; }
        public string DisplayName { get; set; }
        public Guid ProjectGuid { get; set; }

        public override void Serialize(BiEndianBinaryWriter writer, AssetSerializationOptions options) {
            base.Serialize(writer, options);
            throw new NotSupportedException();
        }
    }
}
