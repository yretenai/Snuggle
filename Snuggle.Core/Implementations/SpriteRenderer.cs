using System;
using JetBrains.Annotations;
using Snuggle.Core.IO;
using Snuggle.Core.Meta;
using Snuggle.Core.Models;
using Snuggle.Core.Models.Objects.Graphics;
using Snuggle.Core.Models.Objects.Math;
using Snuggle.Core.Models.Serialization;
using Snuggle.Core.Options;

namespace Snuggle.Core.Implementations;

[PublicAPI]
[UsedImplicitly]
[ObjectImplementation(UnityClassId.SpriteRenderer)]
public class SpriteRenderer : Renderer {
    public SpriteRenderer(BiEndianBinaryReader reader, UnityObjectInfo info, SerializedFile serializedFile) : base(reader, info, serializedFile) {
        Sprite = PPtr<Sprite>.FromReader(reader, serializedFile);
        Color = reader.ReadStruct<ColorRGBA>();

        if (serializedFile.Version >= UnityVersionRegister.Unity5_3) {
            FlipX = reader.ReadBoolean();
            FlipY = reader.ReadBoolean();
            reader.Align();
        }

        if (serializedFile.Version >= UnityVersionRegister.Unity5_6) {
            DrawMode = (SpriteDrawMode) reader.ReadInt32();
            Scale = reader.ReadStruct<Vector2>();
            AdaptiveModeThreshold = reader.ReadSingle();
            TileMode = (SpriteTileMode) reader.ReadInt32();
        }

        if (serializedFile.Version >= UnityVersionRegister.Unity2017_1) {
            WasSpriteAssigned = reader.ReadBoolean();
            reader.Align();

            MaskInteraction = (SpriteMaskInteraction) reader.ReadInt32();
        }

        if (serializedFile.Version >= UnityVersionRegister.Unity2018_2) {
            SortPoint = (SpriteSortPoint) reader.ReadInt32();
        }
    }

    public SpriteRenderer(UnityObjectInfo info, SerializedFile serializedFile) : base(info, serializedFile) {
        Sprite = PPtr<Sprite>.Null;
    }

    public PPtr<Sprite> Sprite { get; set; }
    public ColorRGBA Color { get; set; }
    public bool FlipX { get; set; }
    public bool FlipY { get; set; }
    public SpriteDrawMode DrawMode { get; set; }
    public Vector2 Scale { get; set; }
    public float AdaptiveModeThreshold { get; set; }
    public SpriteTileMode TileMode { get; set; }
    public bool WasSpriteAssigned { get; set; }
    public SpriteMaskInteraction MaskInteraction { get; set; }
    public SpriteSortPoint SortPoint { get; set; }

    public override void Serialize(BiEndianBinaryWriter writer, AssetSerializationOptions options) {
        throw new NotImplementedException();
    }
}
