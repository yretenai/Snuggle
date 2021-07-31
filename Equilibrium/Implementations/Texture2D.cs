using System;
using System.IO;
using Equilibrium.IO;
using Equilibrium.Meta;
using Equilibrium.Models;
using Equilibrium.Models.Objects;
using Equilibrium.Models.Objects.Graphics;
using Equilibrium.Models.Serialization;
using JetBrains.Annotations;

namespace Equilibrium.Implementations {
    [PublicAPI, ObjectImplementation(UnityClassId.Texture2D)]
    public class Texture2D : Texture {
        public Texture2D([NotNull] BiEndianBinaryReader reader, [NotNull] UnityObjectInfo info, [NotNull] SerializedFile serializedFile) : base(reader, info, serializedFile) {
            Width = reader.ReadInt32();
            Height = reader.ReadInt32();
            ImageDataSize = reader.ReadInt32();

            if (serializedFile.Version >= new UnityVersion(2020, 1)) {
                IsStrippedMips = reader.ReadBoolean();
                reader.Align();
            }

            TextureFormat = (TextureFormat) reader.ReadInt32();
            if (serializedFile.Version <= new UnityVersion(5, 2)) {
                if (reader.ReadBoolean()) {
                    MipCount = (int) (Math.Log(Math.Max(Width, Height)) / Math.Log(2));
                }
            } else {
                MipCount = reader.ReadInt32();
            }

            IsReadable = reader.ReadBoolean();
            if (serializedFile.Version >= new UnityVersion(2020, 1)) {
                IsPreProcessed = reader.ReadBoolean();
            }

            if (serializedFile.Version >= new UnityVersion(2019, 3)) {
                IgnoreTextureLimit = reader.ReadBoolean();
            }

            if (serializedFile.Version <= new UnityVersion(5, 4)) {
                IsReadAllowed = reader.ReadBoolean();
            }

            if (serializedFile.Version >= new UnityVersion(2018, 2)) {
                IsStreaming = reader.ReadBoolean();
            }

            reader.Align();

            if (serializedFile.Version >= new UnityVersion(2018, 2)) {
                StreamingPriority = reader.ReadInt32();
            }

            ImageCount = reader.ReadInt32();
            TextureDimension = (TextureDimension) reader.ReadInt32();
            TextureSettings = GLTextureSettings.FromReader(reader, serializedFile);
            LightmapFormat = (LightmapFormat) reader.ReadInt32();
            ColorSpace = (ColorSpace) reader.ReadInt32();
            if (serializedFile.Version >= new UnityVersion(2020, 2)) {
                PlatformDataStart = reader.BaseStream.Position;
                reader.BaseStream.Seek(reader.ReadInt32(), SeekOrigin.Current);
            }

            ImageDataStart = reader.BaseStream.Position;
            reader.BaseStream.Seek(reader.ReadInt32(), SeekOrigin.Current);
            StreamingInfo = StreamingInfo.FromReader(reader, serializedFile);
        }

        public Texture2D([NotNull] UnityObjectInfo info, [NotNull] SerializedFile serializedFile) : base(info, serializedFile) {
            TextureSettings = GLTextureSettings.Default;
            StreamingInfo = StreamingInfo.Default;
        }

        public int Width { get; set; }
        public int Height { get; set; }
        public int ImageDataSize { get; set; }
        public bool IsStrippedMips { get; set; }
        public TextureFormat TextureFormat { get; set; }
        public int MipCount { get; set; } = 1;
        public bool IsReadable { get; set; }
        public bool IsPreProcessed { get; set; }
        public bool IgnoreTextureLimit { get; set; }
        public bool IsReadAllowed { get; set; }
        public bool IsStreaming { get; set; }
        public int StreamingPriority { get; set; }
        public int ImageCount { get; set; }
        public TextureDimension TextureDimension { get; set; }
        public GLTextureSettings TextureSettings { get; set; }
        public LightmapFormat LightmapFormat { get; set; }
        public ColorSpace ColorSpace { get; set; }
        public StreamingInfo StreamingInfo { get; set; }
        private long ImageDataStart { get; set; }
        private long PlatformDataStart { get; set; }
        public Memory<byte> PlatformData { get; set; } = Memory<byte>.Empty;
        public Memory<byte> ImageData { get; set; } = Memory<byte>.Empty;
        public override bool ShouldDeserialize => base.ShouldDeserialize || ImageData.IsEmpty;

        public override void Free() {
            base.Free();
            ImageData = Memory<byte>.Empty;
        }
    }
}
