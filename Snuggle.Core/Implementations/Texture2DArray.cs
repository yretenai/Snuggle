using System;
using System.IO;
using System.Text.Json.Serialization;
using Snuggle.Core.Interfaces;
using Snuggle.Core.IO;
using Snuggle.Core.Meta;
using Snuggle.Core.Models;
using Snuggle.Core.Models.Objects;
using Snuggle.Core.Models.Objects.Graphics;
using Snuggle.Core.Models.Serialization;
using Snuggle.Core.Options;

namespace Snuggle.Core.Implementations;

[ObjectImplementation(UnityClassId.Texture2DArray)]
public class Texture2DArray : Texture, ITexture {
    public Texture2DArray(BiEndianBinaryReader reader, UnityObjectInfo info, SerializedFile serializedFile) : base(reader, info, serializedFile) {
        if (serializedFile.Version >= UnityVersionRegister.Unity2019) {
            ColorSpace = (ColorSpace) reader.ReadInt32();
            TextureFormat = ((GraphicsFormat) reader.ReadInt32()).ToTextureFormat();
        }

        Width = reader.ReadInt32();
        Height = reader.ReadInt32();
        Depth = reader.ReadInt32();

        if (serializedFile.Version < UnityVersionRegister.Unity2019) {
            TextureFormat = (TextureFormat) reader.ReadInt32();
        }

        MipCount = reader.ReadInt32();
        TextureDataSize = reader.ReadUInt32();

        TextureSettings = GLTextureSettings.FromReader(reader, SerializedFile);

        if (serializedFile.Version < UnityVersionRegister.Unity2019) {
            ColorSpace = (ColorSpace) reader.ReadInt32();
        }

        if (serializedFile.Version >= UnityVersionRegister.Unity2020_2) {
            UsageMode = (TextureUsageMode) reader.ReadInt32();
        }

        IsReadable = reader.ReadBoolean();
        reader.Align();

        TextureDataStart = reader.BaseStream.Position;
        reader.BaseStream.Seek(reader.ReadInt32(), SeekOrigin.Current);

        if (serializedFile.Version > UnityVersionRegister.Unity5_6) {
            StreamDataOffset = reader.BaseStream.Position;
            StreamData = StreamingInfo.FromReader(reader, serializedFile);
        } else {
            StreamDataOffset = -1;
            StreamData = StreamingInfo.Null;
        }
    }

    public Texture2DArray(UnityObjectInfo info, SerializedFile serializedFile) : base(info, serializedFile) {
        TextureSettings = GLTextureSettings.Default;
        StreamData = StreamingInfo.Null;
        StreamDataOffset = -1;
    }

    public uint TextureDataSize { get; set; }

    public bool IsReadable { get; set; }
    public GLTextureSettings TextureSettings { get; set; }
    public ColorSpace ColorSpace { get; set; }
    private long TextureDataStart { get; } = -1;
    private bool ShouldDeserializeTextureData => (TextureDataStart > -1 || !StreamData.IsNull) && TextureData == null;

    public int Width { get; set; }
    public int Height { get; set; }
    public int Depth { get; set; }
    public TextureFormat TextureFormat { get; set; }
    public int MipCount { get; set; } = 1;
    public TextureUsageMode UsageMode { get; set; }

    [JsonIgnore]
    public Memory<byte>? TextureData { get; set; }

    [JsonIgnore]
    public override bool ShouldDeserialize => base.ShouldDeserialize || ShouldDeserializeTextureData;

    public long StreamDataOffset { get; set; }
    public StreamingInfo StreamData { get; set; }

    public override void Serialize(BiEndianBinaryWriter writer, AssetSerializationOptions options) {
        throw new InvalidOperationException("Use Serialize(BiEndianBinaryWriter writer, BiEndianBinaryWriter resourceStream, AssetSerializationOptions options)");
    }

    public void Serialize(BiEndianBinaryWriter writer, BiEndianBinaryWriter resourceStream, AssetSerializationOptions options) {
        throw new NotImplementedException();
    }

    public override void Deserialize(BiEndianBinaryReader reader, ObjectDeserializationOptions options) {
        base.Deserialize(reader, options);

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
