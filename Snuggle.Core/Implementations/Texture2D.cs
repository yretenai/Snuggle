using System;
using System.IO;
using System.Text.Json.Serialization;
using JetBrains.Annotations;
using Snuggle.Core.Exceptions;
using Snuggle.Core.Game.Unite;
using Snuggle.Core.Interfaces;
using Snuggle.Core.IO;
using Snuggle.Core.Meta;
using Snuggle.Core.Models;
using Snuggle.Core.Models.Objects;
using Snuggle.Core.Models.Objects.Graphics;
using Snuggle.Core.Models.Serialization;
using Snuggle.Core.Options;

namespace Snuggle.Core.Implementations;

[PublicAPI]
[ObjectImplementation(UnityClassId.Texture2D)]
public class Texture2D : Texture, ISerializedResource {
    public Texture2D(BiEndianBinaryReader reader, UnityObjectInfo info, SerializedFile serializedFile) : base(reader, info, serializedFile) {
        Width = reader.ReadInt32();
        Height = reader.ReadInt32();
        TextureDataSize = reader.ReadUInt32();

        if (serializedFile.Version >= UnityVersionRegister.Unity2020_1) {
            IsStrippedMips = reader.ReadBoolean();
            reader.Align();
        }

        TextureFormat = (TextureFormat) reader.ReadInt32();
        if (serializedFile.Version >= UnityVersionRegister.Unity5_2) {
            MipCount = reader.ReadInt32();
        } else {
            if (reader.ReadBoolean()) {
                MipCount = (int) Math.Log2(Math.Max(Width, Height));
            }
        }

        IsReadable = reader.ReadBoolean();

        if (serializedFile.Version < UnityVersionRegister.Unity5_5) {
            IsReadAllowed = reader.ReadBoolean();
        }

        if (serializedFile.Version >= UnityVersionRegister.Unity2020_1) {
            IsPreProcessed = reader.ReadBoolean();
        }

        if (serializedFile.Version >= UnityVersionRegister.Unity2019_3) {
            IgnoreTextureLimit = reader.ReadBoolean();
        }

        if (serializedFile.Version >= UnityVersionRegister.Unity2018_2) {
            IsStreamingMipmaps = reader.ReadBoolean();
        }

        reader.Align();

        if (serializedFile.Version >= UnityVersionRegister.Unity2018_2) {
            StreamingPriority = reader.ReadInt32();
        }

        TextureCount = reader.ReadInt32();
        TextureDimension = (TextureDimension) reader.ReadInt32();
        TextureSettings = GLTextureSettings.FromReader(reader, serializedFile);
        LightmapFormat = (LightmapFormat) reader.ReadInt32();

        if (serializedFile.Options.Game == UnityGame.PokemonUnite) {
            var container = GetExtraContainer<UniteTexture2DExtension>(UnityClassId.Texture2D);
            container.UnknownValue = reader.ReadInt32();
        }

        ColorSpace = (ColorSpace) reader.ReadInt32();
        if (serializedFile.Version >= UnityVersionRegister.Unity2020_2) {
            PlatformDataStart = reader.BaseStream.Position;
            reader.BaseStream.Seek(reader.ReadInt32(), SeekOrigin.Current);
        }

        TextureDataStart = reader.BaseStream.Position;
        reader.BaseStream.Seek(reader.ReadInt32(), SeekOrigin.Current);

        if (serializedFile.Version > UnityVersionRegister.Unity5_3) {
            StreamDataOffset = reader.BaseStream.Position;
            StreamData = StreamingInfo.FromReader(reader, serializedFile);
        } else {
            StreamDataOffset = -1;
            StreamData = StreamingInfo.Null;
        }
    }

    public Texture2D(UnityObjectInfo info, SerializedFile serializedFile) : base(info, serializedFile) {
        TextureSettings = GLTextureSettings.Default;
        StreamData = StreamingInfo.Null;
        StreamDataOffset = -1;
    }

    public int Width { get; set; }
    public int Height { get; set; }
    public uint TextureDataSize { get; set; }
    public bool IsStrippedMips { get; set; }
    public TextureFormat TextureFormat { get; set; }
    public int MipCount { get; set; } = 1;
    public bool IsReadable { get; set; }
    public bool IsPreProcessed { get; set; }
    public bool IgnoreTextureLimit { get; set; }
    public bool IsReadAllowed { get; set; }
    public bool IsStreamingMipmaps { get; set; }
    public int StreamingPriority { get; set; }
    public int TextureCount { get; set; }
    public TextureDimension TextureDimension { get; set; }
    public GLTextureSettings TextureSettings { get; set; }
    public LightmapFormat LightmapFormat { get; set; }
    public ColorSpace ColorSpace { get; set; }
    private long TextureDataStart { get; init; } = -1;
    private long PlatformDataStart { get; init; } = -1;

    [JsonIgnore]
    public Memory<byte>? PlatformData { get; set; }

    [JsonIgnore]
    public Memory<byte>? TextureData { get; set; }

    private bool ShouldDeserializePlatformData => PlatformDataStart > -1 && PlatformData == null;
    private bool ShouldDeserializeTextureData => (TextureDataStart > -1 || !StreamData.IsNull) && TextureData == null;

    [JsonIgnore]
    public override bool ShouldDeserialize => base.ShouldDeserialize || ShouldDeserializePlatformData || ShouldDeserializeTextureData;

    public long StreamDataOffset { get; set; }
    public StreamingInfo StreamData { get; set; }

    public override void Serialize(BiEndianBinaryWriter writer, AssetSerializationOptions options) {
        throw new InvalidOperationException("Use Serialize(BiEndianBinaryWriter writer, BiEndianBinaryWriter resourceStream, AssetSerializationOptions options)");
    }

    public void Serialize(BiEndianBinaryWriter writer, BiEndianBinaryWriter resourceStream, AssetSerializationOptions options) {
        if (ShouldDeserialize) {
            throw new IncompleteDeserializationException();
        }

        base.Serialize(writer, options);
        writer.Write(Width);
        writer.Write(Height);
        writer.Write(TextureData!.Value.Length);

        if (options.TargetVersion >= UnityVersionRegister.Unity2020_1) {
            writer.Write(IsStrippedMips);
            writer.Write(IsStrippedMips);
            writer.Align();
        }

        writer.Write((int) TextureFormat);
        if (options.TargetVersion < UnityVersionRegister.Unity5_3) {
            writer.Write(MipCount > 1);
        } else {
            writer.Write(MipCount);
        }

        writer.Write(IsReadable);
        if (options.TargetVersion >= UnityVersionRegister.Unity2020_1) {
            writer.Write(IsPreProcessed);
        }

        if (options.TargetVersion >= UnityVersionRegister.Unity2019_3) {
            writer.Write(IgnoreTextureLimit);
        }

        if (options.TargetVersion < UnityVersionRegister.Unity5_5) {
            writer.Write(IsReadAllowed);
        }

        if (options.TargetVersion >= UnityVersionRegister.Unity2018_2) {
            writer.Write(IsStreamingMipmaps);
        }

        writer.Align();

        if (options.TargetVersion >= UnityVersionRegister.Unity2018_2) {
            writer.Write(StreamingPriority);
        }

        writer.Write(TextureCount);
        writer.Write((int) TextureDimension);
        TextureSettings.ToWriter(writer, SerializedFile, options.TargetVersion);
        writer.Write((int) LightmapFormat);
        writer.Write((int) ColorSpace);
        if (options.TargetVersion >= UnityVersionRegister.Unity2020_2) {
            writer.WriteMemory(PlatformData);
        }

        if (TextureData.Value.Length > options.ResourceDataThreshold) {
            writer.Write(0);
            new StreamingInfo(resourceStream.BaseStream.Position, TextureData.Value.Length, options.ResourceFileName).ToWriter(writer, SerializedFile, options.TargetVersion);
            resourceStream.WriteMemory(TextureData);
            resourceStream.Align(options.Alignment);
        } else {
            new StreamingInfo(writer.BaseStream.Position, TextureData.Value.Length, options.FileName).ToWriter(writer, SerializedFile, options.TargetVersion);
            writer.WriteMemory(TextureData);
        }
    }

    public override void Deserialize(BiEndianBinaryReader reader, ObjectDeserializationOptions options) {
        base.Deserialize(reader, options);
        if (ShouldDeserializePlatformData) {
            reader.BaseStream.Seek(PlatformDataStart, SeekOrigin.Begin);
            PlatformData = reader.ReadMemory(reader.ReadInt32());
        }

        if (ShouldDeserializeTextureData) {
            if (TextureDataStart > -1) {
                reader.BaseStream.Seek(TextureDataStart, SeekOrigin.Begin);
                TextureData = reader.ReadMemory(reader.ReadInt32());
            }

            if (!StreamData.IsNull) {
                TextureData = StreamData.GetData(SerializedFile.Assets, options, TextureData);
            }
        }
    }

    public override void Free() {
        if (IsMutated) {
            return;
        }

        base.Free();
        TextureData = null;
    }

    public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), Width, Height, TextureFormat, TextureDataSize, StreamData);
}
