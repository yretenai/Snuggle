using System;
using System.IO;
using Equilibrium.IO;
using Equilibrium.Meta;
using Equilibrium.Meta.Interfaces;
using Equilibrium.Meta.Options;
using Equilibrium.Models;
using Equilibrium.Models.Objects;
using Equilibrium.Models.Objects.Graphics;
using Equilibrium.Models.Serialization;
using JetBrains.Annotations;

namespace Equilibrium.Implementations {
    [PublicAPI, ObjectImplementation(UnityClassId.Texture2D)]
    public class Texture2D : Texture, ISerializedResource {
        public Texture2D(BiEndianBinaryReader reader, UnityObjectInfo info, SerializedFile serializedFile) : base(reader, info, serializedFile) {
            Width = reader.ReadInt32();
            Height = reader.ReadInt32();
            TextureDataSize = reader.ReadInt32();

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

            TextureCount = reader.ReadInt32();
            TextureDimension = (TextureDimension) reader.ReadInt32();
            TextureSettings = GLTextureSettings.FromReader(reader, serializedFile);
            LightmapFormat = (LightmapFormat) reader.ReadInt32();
            ColorSpace = (ColorSpace) reader.ReadInt32();
            if (serializedFile.Version >= new UnityVersion(2020, 2)) {
                PlatformDataStart = reader.BaseStream.Position;
                reader.BaseStream.Seek(reader.ReadInt32(), SeekOrigin.Current);
            }

            TextureDataStart = reader.BaseStream.Position;
            reader.BaseStream.Seek(reader.ReadInt32(), SeekOrigin.Current);
            StreamingInfo = StreamingInfo.FromReader(reader, serializedFile);
        }

        public Texture2D(UnityObjectInfo info, SerializedFile serializedFile) : base(info, serializedFile) {
            TextureSettings = GLTextureSettings.Default;
            StreamingInfo = StreamingInfo.Default;
        }

        public int Width { get; set; }
        public int Height { get; set; }
        public int TextureDataSize { get; set; }
        public bool IsStrippedMips { get; set; }
        public TextureFormat TextureFormat { get; set; }
        public int MipCount { get; set; } = 1;
        public bool IsReadable { get; set; }
        public bool IsPreProcessed { get; set; }
        public bool IgnoreTextureLimit { get; set; }
        public bool IsReadAllowed { get; set; }
        public bool IsStreaming { get; set; }
        public int StreamingPriority { get; set; }
        public int TextureCount { get; set; }
        public TextureDimension TextureDimension { get; set; }
        public GLTextureSettings TextureSettings { get; set; }
        public LightmapFormat LightmapFormat { get; set; }
        public ColorSpace ColorSpace { get; set; }
        public StreamingInfo StreamingInfo { get; set; }
        private long TextureDataStart { get; set; }
        private long PlatformDataStart { get; set; }
        public Memory<byte> PlatformData { get; set; } = Memory<byte>.Empty;
        public Memory<byte> TextureData { get; set; } = Memory<byte>.Empty;
        public override bool ShouldDeserialize => base.ShouldDeserialize || TextureData.IsEmpty;

        public override void Deserialize(BiEndianBinaryReader reader, ObjectDeserializationOptions options) {
            base.Deserialize(reader, options);
            if (PlatformDataStart > 0) {
                reader.BaseStream.Seek(PlatformDataStart, SeekOrigin.Begin);
                PlatformData = reader.ReadMemory(reader.ReadInt32());
            }

            if (TextureDataStart > 0) {
                if (StreamingInfo.Size == 0) {
                    reader.BaseStream.Seek(TextureDataStart, SeekOrigin.Begin);
                    TextureData = reader.ReadMemory(reader.ReadInt32());
                } else {
                    TextureData = StreamingInfo.GetData(SerializedFile.Assets, options);
                }
            }
        }

        public void Serialize(BiEndianBinaryWriter writer, string fileName, BiEndianBinaryWriter resourceStream, string resourceName, UnityVersion? targetVersion, FileSerializationOptions options) {
            base.Serialize(writer, fileName, targetVersion, options);
            writer.Write(Width);
            writer.Write(Height);
            writer.Write(TextureData.Length);

            var version = targetVersion ?? SerializedFile.Version;

            if (version >= new UnityVersion(2020, 1)) {
                writer.Write(IsStrippedMips);
                writer.Write(IsStrippedMips);
                writer.Align();
            }

            writer.Write((int) TextureFormat);
            if (version <= new UnityVersion(5, 2)) {
                writer.Write(MipCount > 1);
            } else {
                writer.Write(MipCount);
            }

            writer.Write(IsReadable);
            if (version >= new UnityVersion(2020, 1)) {
                writer.Write(IsPreProcessed);
            }

            if (version >= new UnityVersion(2019, 3)) {
                writer.Write(IgnoreTextureLimit);
            }

            if (version <= new UnityVersion(5, 4)) {
                writer.Write(IsReadAllowed);
            }

            if (version >= new UnityVersion(2018, 2)) {
                writer.Write(IsStreaming);
            }

            writer.Align();

            if (version >= new UnityVersion(2018, 2)) {
                writer.Write(StreamingPriority);
            }

            writer.Write(TextureCount);
            writer.Write((int) TextureDimension);
            TextureSettings.ToWriter(writer, SerializedFile, version);
            writer.Write((int) LightmapFormat);
            writer.Write((int) ColorSpace);
            if (version >= new UnityVersion(2020, 2)) {
                writer.WriteMemory(PlatformData);
            }

            if (TextureData.Length > options.ResourceDataThreshold) {
                writer.Write(0);
                new StreamingInfo(resourceStream.BaseStream.Position, TextureData.Length, resourceName).ToWriter(writer, SerializedFile, version);
                resourceStream.WriteMemory(TextureData);
                resourceStream.Align(options.Alignment);
            } else {
                new StreamingInfo(writer.BaseStream.Position, TextureData.Length, fileName).ToWriter(writer, SerializedFile, version);
                writer.WriteMemory(TextureData);
            }
        }

        public override void Free() {
            base.Free();
            TextureData = Memory<byte>.Empty;
        }

        public Stream ToDDS() {
            throw new NotImplementedException();
        }

        public Stream ToKTX2() {
            throw new NotImplementedException();
        }
    }
}
