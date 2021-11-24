using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Snuggle.Core.IO;
using Snuggle.Core.Meta;
using Snuggle.Core.Models;
using Snuggle.Core.Models.Objects.Graphics;
using Snuggle.Core.Models.Serialization;
using Snuggle.Core.Options;

namespace Snuggle.Core.Implementations;

[PublicAPI]
[UsedImplicitly]
[ObjectImplementation(UnityClassId.SpriteAtlas)]
public class SpriteAtlas : NamedObject {
    public SpriteAtlas(BiEndianBinaryReader reader, UnityObjectInfo info, SerializedFile serializedFile) : base(reader, info, serializedFile) {
        var count = reader.ReadInt32();
        PackedSprites.EnsureCapacity(count);
        for (var i = 0; i < count; ++i) {
            PackedSprites.Add(PPtr<Sprite>.FromReader(reader, serializedFile));
        }

        count = reader.ReadInt32();
        PackedSpriteNamesToIndex.EnsureCapacity(count);
        for (var i = 0; i < count; ++i) {
            PackedSpriteNamesToIndex.Add(reader.ReadString32());
        }

        count = reader.ReadInt32();
        RenderDataMap.EnsureCapacity(count);
        for (var i = 0; i < count; ++i) {
            var key = new KeyValuePair<Guid, long>(reader.ReadStruct<Guid>(), reader.ReadInt64());
            var value = SpriteAtlasData.FromReader(reader, serializedFile);
            RenderDataMap[key] = value;
        }

        Tag = reader.ReadString32();

        IsVariant = reader.ReadBoolean();
        reader.Align();
    }

    public SpriteAtlas(UnityObjectInfo info, SerializedFile serializedFile) : base(info, serializedFile) {
        Tag = string.Empty;
    }

    public List<PPtr<Sprite>> PackedSprites { get; set; } = new();
    public List<string> PackedSpriteNamesToIndex { get; set; } = new();
    public Dictionary<KeyValuePair<Guid, long>, SpriteAtlasData> RenderDataMap { get; set; } = new();
    public string Tag { get; set; }
    public bool IsVariant { get; set; }

    public override void Serialize(BiEndianBinaryWriter writer, AssetSerializationOptions options) {
        throw new NotImplementedException();
    }
}
