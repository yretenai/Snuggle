using System;
using JetBrains.Annotations;
using Snuggle.Core.IO;
using Snuggle.Core.Meta;
using Snuggle.Core.Models;
using Snuggle.Core.Models.Serialization;
using Snuggle.Core.Options;

namespace Snuggle.Core.Implementations {
    [PublicAPI, UsedImplicitly, ObjectImplementation(UnityClassId.MeshFilter)]
    public class MeshFilter : Component {
        public MeshFilter(BiEndianBinaryReader reader, UnityObjectInfo info, SerializedFile serializedFile) : base(reader, info, serializedFile) => Mesh = PPtr<Mesh>.FromReader(reader, serializedFile);
        public MeshFilter(UnityObjectInfo info, SerializedFile serializedFile) : base(info, serializedFile) => Mesh = PPtr<Mesh>.Null;

        public PPtr<Mesh> Mesh { get; set; }

        public override void Serialize(BiEndianBinaryWriter writer, AssetSerializationOptions options) {
            base.Serialize(writer, options);
            Mesh.ToWriter(writer, SerializedFile, options.TargetVersion);
        }

        public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), GameObject);
    }
}
