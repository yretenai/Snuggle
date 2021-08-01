using System;
using System.IO;
using DragonLib.Imaging.DXGI;
using Equilibrium.Extensions;
using Equilibrium.Interfaces;
using Equilibrium.IO;
using Equilibrium.Meta;
using Equilibrium.Models;
using Equilibrium.Models.Objects;
using Equilibrium.Models.Objects.Graphics;
using Equilibrium.Models.Serialization;
using Equilibrium.Options;
using JetBrains.Annotations;

namespace Equilibrium.Implementations {
    [PublicAPI, ObjectImplementation(UnityClassId.Texture2D)]
    public class Texture2D : Texture, ISerializedResource {
        public Texture2D(BiEndianBinaryReader reader, UnityObjectInfo info, SerializedFile serializedFile) : base(reader, info, serializedFile) {
            Width = reader.ReadInt32();
            Height = reader.ReadInt32();
            TextureDataSize = reader.ReadInt32();

            if (serializedFile.Version >= UnityVersionRegister.Unity2020_1) {
                IsStrippedMips = reader.ReadBoolean();
                reader.Align();
            }

            TextureFormat = (TextureFormat) reader.ReadInt32();
            if (serializedFile.Version <= UnityVersionRegister.Unity5_2) {
                if (reader.ReadBoolean()) {
                    MipCount = (int) (Math.Log(Math.Max(Width, Height)) / Math.Log(2));
                }
            } else {
                MipCount = reader.ReadInt32();
            }

            IsReadable = reader.ReadBoolean();
            if (serializedFile.Version >= UnityVersionRegister.Unity2020_1) {
                IsPreProcessed = reader.ReadBoolean();
            }

            if (serializedFile.Version >= UnityVersionRegister.Unity2019_3) {
                IgnoreTextureLimit = reader.ReadBoolean();
            }

            if (serializedFile.Version <= UnityVersionRegister.Unity5_4) {
                IsReadAllowed = reader.ReadBoolean();
            }

            if (serializedFile.Version >= UnityVersionRegister.Unity2018_2) {
                IsStreaming = reader.ReadBoolean();
            }

            reader.Align();

            if (serializedFile.Version >= UnityVersionRegister.Unity2018_2) {
                StreamingPriority = reader.ReadInt32();
            }

            TextureCount = reader.ReadInt32();
            TextureDimension = (TextureDimension) reader.ReadInt32();
            TextureSettings = GLTextureSettings.FromReader(reader, serializedFile);
            LightmapFormat = (LightmapFormat) reader.ReadInt32();
            ColorSpace = (ColorSpace) reader.ReadInt32();
            if (serializedFile.Version >= UnityVersionRegister.Unity2020_2) {
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

        public void Serialize(BiEndianBinaryWriter writer, string fileName, BiEndianBinaryWriter resourceStream, string resourceName, UnityVersion targetVersion, FileSerializationOptions options) {
            base.Serialize(writer, fileName, targetVersion, options);
            writer.Write(Width);
            writer.Write(Height);
            writer.Write(TextureData.Length);

            if (targetVersion >= UnityVersionRegister.Unity2020_1) {
                writer.Write(IsStrippedMips);
                writer.Write(IsStrippedMips);
                writer.Align();
            }

            writer.Write((int) TextureFormat);
            if (targetVersion <= UnityVersionRegister.Unity5_2) {
                writer.Write(MipCount > 1);
            } else {
                writer.Write(MipCount);
            }

            writer.Write(IsReadable);
            if (targetVersion >= UnityVersionRegister.Unity2020_1) {
                writer.Write(IsPreProcessed);
            }

            if (targetVersion >= UnityVersionRegister.Unity2019_3) {
                writer.Write(IgnoreTextureLimit);
            }

            if (targetVersion <= UnityVersionRegister.Unity5_4) {
                writer.Write(IsReadAllowed);
            }

            if (targetVersion >= UnityVersionRegister.Unity2018_2) {
                writer.Write(IsStreaming);
            }

            writer.Align();

            if (targetVersion >= UnityVersionRegister.Unity2018_2) {
                writer.Write(StreamingPriority);
            }

            writer.Write(TextureCount);
            writer.Write((int) TextureDimension);
            TextureSettings.ToWriter(writer, SerializedFile, targetVersion);
            writer.Write((int) LightmapFormat);
            writer.Write((int) ColorSpace);
            if (targetVersion >= UnityVersionRegister.Unity2020_2) {
                writer.WriteMemory(PlatformData);
            }

            if (TextureData.Length > options.ResourceDataThreshold) {
                writer.Write(0);
                new StreamingInfo(resourceStream.BaseStream.Position, TextureData.Length, resourceName).ToWriter(writer, SerializedFile, targetVersion);
                resourceStream.WriteMemory(TextureData);
                resourceStream.Align(options.Alignment);
            } else {
                new StreamingInfo(writer.BaseStream.Position, TextureData.Length, fileName).ToWriter(writer, SerializedFile, targetVersion);
                writer.WriteMemory(TextureData);
            }
        }

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

        public override void Free() {
            if (IsMutated) {
                return;
            }

            base.Free();
            TextureData = Memory<byte>.Empty;
        }

        public Stream ToDDS() {
            if (ShouldDeserialize) {
                throw new InvalidDataException();
            }

            return new MemoryStream(DXGI.BuildDDS(TextureFormat.ToD3DPixelFormat(),
                    MipCount,
                    Width,
                    Height,
                    TextureCount,
                    TextureData.Span)
                .ToArray()) { Position = 0 };
        }

        public void ImportDDS(Stream stream, bool leaveOpen = false) {
            using var reader = new BiEndianBinaryReader(stream, leaveOpen);
            var header = reader.ReadStruct<DDSImageHeader>();

            IsMutated = true;

            Width = header.Width;
            Height = header.Height;
            MipCount = header.MipmapCount;

            switch (header.Format.FourCC) {
                case 0x30315844: { // DX10
                    var dx10 = reader.ReadStruct<DXT10Header>();
                    TextureFormat = ((DXGIPixelFormat) dx10.Format).ToTextureFormat();
                    TextureCount = dx10.Size;
                    break;
                }
                case 0x31545844: // DXT1
                    TextureFormat = TextureFormat.DXT1;
                    break;
                case 0x34545844: // DXT4
                case 0x35545844: // DXT5
                    TextureFormat = TextureFormat.DXT5;
                    break;
                case 0x31495441: // ATI1
                    TextureFormat = TextureFormat.BC4;
                    break;
                case 0x32495441: // ATI2
                    TextureFormat = TextureFormat.BC5;
                    break;
                default:
                    throw new NotSupportedException();
            }

            TextureData = reader.ReadMemory(reader.Unconsumed);

            if (!leaveOpen) {
                stream.Close();
            }
        }

        public static Texture2D FromDDS(UnityObjectInfo info, SerializedFile file, Stream stream, bool leaveOpen = false) {
            var texture2D = new Texture2D(info, file) {
                Name = "Texture2D",
            };

            texture2D.ImportDDS(stream, leaveOpen);
            if (!leaveOpen) {
                stream.Close();
            }

            return texture2D;
        }

        public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), Width, Height, TextureFormat, TextureData.Length, StreamingInfo);

        public Stream ToKTX2() => throw new NotImplementedException();
    }
}
