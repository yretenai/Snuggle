using System;
using Snuggle.Core.IO;
using Snuggle.Core.Meta;
using Snuggle.Core.Models;
using Snuggle.Core.Models.Serialization;
using Snuggle.Core.Options;

namespace Snuggle.Core.Implementations;

[ObjectImplementation(UnityClassId.MeshRenderer)]
public class MeshRenderer : Renderer {
    public MeshRenderer(BiEndianBinaryReader reader, UnityObjectInfo info, SerializedFile serializedFile) : base(reader, info, serializedFile) {
        AdditionalVertexStream = PPtr<Mesh>.FromReader(reader, SerializedFile);

        if (SerializedFile.Version >= UnityVersionRegister.Unity2020_1) {
            EnlightenVertexStream = PPtr<Mesh>.FromReader(reader, SerializedFile);
        } else {
            EnlightenVertexStream = PPtr<Mesh>.Null;
        }
    }

    public MeshRenderer(UnityObjectInfo info, SerializedFile serializedFile) : base(info, serializedFile) {
        AdditionalVertexStream = PPtr<Mesh>.Null;
        EnlightenVertexStream = PPtr<Mesh>.Null;
    }

    public PPtr<Mesh> AdditionalVertexStream { get; set; }
    public PPtr<Mesh> EnlightenVertexStream { get; set; }

    public override void Serialize(BiEndianBinaryWriter writer, AssetSerializationOptions options) {
        throw new NotImplementedException();
    }
}
