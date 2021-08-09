using System;
using Equilibrium.IO;
using Equilibrium.Meta;
using Equilibrium.Models;
using Equilibrium.Models.Objects.Math;
using Equilibrium.Models.Serialization;
using Equilibrium.Options;
using JetBrains.Annotations;

namespace Equilibrium.Implementations {
    [PublicAPI, ObjectImplementation(UnityClassId.RectTransform)]
    public class RectTransform : Transform {
        public RectTransform(BiEndianBinaryReader reader, UnityObjectInfo info, SerializedFile serializedFile) : base(reader, info, serializedFile) {
            AnchorMin = reader.ReadStruct<Vector2>();
            AnchorMax = reader.ReadStruct<Vector2>();
            Anchor = reader.ReadStruct<Vector2>();
            SizeDelta = reader.ReadStruct<Vector2>();
            Pivot = reader.ReadStruct<Vector2>();
        }

        public RectTransform(UnityObjectInfo info, SerializedFile serializedFile) : base(info, serializedFile) { }

        public Vector2 AnchorMin { get; set; }
        public Vector2 AnchorMax { get; set; }
        public Vector2 Anchor { get; set; }
        public Vector2 SizeDelta { get; set; }
        public Vector2 Pivot { get; set; }

        public override void Serialize(BiEndianBinaryWriter writer, AssetSerializationOptions options) {
            base.Serialize(writer, options);
            writer.WriteStruct(AnchorMin);
            writer.WriteStruct(AnchorMax);
            writer.WriteStruct(Anchor);
            writer.WriteStruct(SizeDelta);
            writer.WriteStruct(Pivot);
        }

        public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), AnchorMin, AnchorMax, Anchor, SizeDelta, Pivot);
    }
}
