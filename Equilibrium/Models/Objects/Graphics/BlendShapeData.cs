using System;
using System.Collections.Generic;
using Equilibrium.IO;
using Equilibrium.Meta;
using Equilibrium.Options;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Equilibrium.Models.Objects.Graphics {
    [PublicAPI]
    public record BlendShapeData(
        List<MeshBlendShape> Shapes,
        List<MeshBlendShapeChannel> Channels) {
        private long VerticesOffset { get; } = -1;
        private long WeightsOffset { get; } = -1;

        [JsonIgnore]
        public List<BlendShapeVertex>? Vertices { get; set; }

        [JsonIgnore]
        public List<float>? Weights { get; set; }

        public static BlendShapeData Default { get; } = new(new List<MeshBlendShape>(), new List<MeshBlendShapeChannel>());

        public bool ShouldDeserialize => Vertices == null || Weights == null;

        public static BlendShapeData FromReader(BiEndianBinaryReader reader, SerializedFile file) => throw new NotImplementedException();

        public void ToWriter(BiEndianBinaryWriter writer, SerializedFile serializedFile, UnityVersion targetVersion) {
            throw new NotImplementedException();
        }

        public void Deserialize(BiEndianBinaryReader reader, ObjectDeserializationOptions options) {
            throw new NotImplementedException();
        }
    }
}
