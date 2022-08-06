using System;
using System.Collections.Generic;
using Snuggle.Core.Game.Unite;
using Snuggle.Core.IO;
using Snuggle.Core.Meta;
using Snuggle.Core.Models;
using Snuggle.Core.Models.Objects.Graphics;
using Snuggle.Core.Models.Objects.Math;
using Snuggle.Core.Models.Serialization;
using Snuggle.Core.Options;

namespace Snuggle.Core.Implementations;

[ObjectImplementation(UnityClassId.Sprite)]
public class Sprite : NamedObject {
    public Sprite(BiEndianBinaryReader reader, UnityObjectInfo info, SerializedFile serializedFile) : base(reader, info, serializedFile) {
        if (serializedFile.Options.Game is UnityGame.PokemonUnite) {
            var container = GetExtraContainer<UniteSpriteExtension>(UnityClassId.Sprite);
            container.UnknownValue = reader.ReadInt32();
        }

        Rect = reader.ReadStruct<Rect>();
        Offset = reader.ReadStruct<Vector2>();
        Border = reader.ReadStruct<Vector4>();
        PixelsToUnits = reader.ReadSingle();
        Pivot = serializedFile.Version >= UnityVersionRegister.Unity5_5 ? reader.ReadStruct<Vector2>() : new Vector2(0.5f, 0.5f);
        Extrude = reader.ReadUInt32();

        IsPolygon = serializedFile.Version >= UnityVersionRegister.Unity5_3 && reader.ReadBoolean();
        reader.Align();

        int count;

        if (serializedFile.Version >= UnityVersionRegister.Unity2017_1) {
            RenderDataKey = new KeyValuePair<Guid, long>(reader.ReadStruct<Guid>(), reader.ReadInt64());

            count = reader.ReadInt32();
            AtlasTags.EnsureCapacity(count);
            for (var i = 0; i < count; ++i) {
                AtlasTags.Add(reader.ReadString32());
            }

            SpriteAtlas = PPtr<SpriteAtlas>.FromReader(reader, serializedFile);
        } else {
            SpriteAtlas = PPtr<SpriteAtlas>.Null;
        }

        RenderData = SpriteRenderData.FromReader(reader, serializedFile);

        count = reader.ReadInt32();
        PhysicsShape.EnsureCapacity(count);
        for (var i = 0; i < count; ++i) {
            var physicsCount = reader.ReadInt32();
            var list = new List<Vector2>();
            list.EnsureCapacity(physicsCount);
            list.AddRange(reader.ReadArray<Vector2>(physicsCount));
            PhysicsShape.Add(list);
        }

        if (serializedFile.Version >= UnityVersionRegister.Unity2018_1) {
            count = reader.ReadInt32();
            Bones.EnsureCapacity(count);
            for (var i = 0; i < count; ++i) {
                Bones.Add(SpriteBone.FromReader(reader, serializedFile));
            }
        }
    }

    public Sprite(UnityObjectInfo info, SerializedFile serializedFile) : base(info, serializedFile) {
        SpriteAtlas = PPtr<SpriteAtlas>.Null;
        RenderData = SpriteRenderData.Default;
    }

    public Rect Rect { get; set; }
    public Vector2 Offset { get; set; }
    public Vector4 Border { get; set; }
    public float PixelsToUnits { get; set; }
    public Vector2 Pivot { get; set; }
    public uint Extrude { get; set; }
    public bool IsPolygon { get; set; }
    public KeyValuePair<Guid, long> RenderDataKey { get; set; }
    public List<string> AtlasTags { get; set; } = new();
    public PPtr<SpriteAtlas> SpriteAtlas { get; set; }
    public SpriteRenderData RenderData { get; set; }
    public List<List<Vector2>> PhysicsShape { get; set; } = new();
    public List<SpriteBone> Bones { get; set; } = new();

    public override bool ShouldDeserialize => base.ShouldDeserialize || RenderData.ShouldDeserialize;

    public override void Serialize(BiEndianBinaryWriter writer, AssetSerializationOptions options) {
        throw new NotImplementedException();
    }

    public override void Free() {
        if (IsMutated) {
            return;
        }

        base.Free();
        RenderData.Free();
    }

    public override void Deserialize(BiEndianBinaryReader reader, ObjectDeserializationOptions options) {
        base.Deserialize(reader, options);

        if (RenderData.ShouldDeserialize) {
            RenderData.Deserialize(reader, SerializedFile, options);
        }
    }
}
