using System;
using System.Collections.Generic;
using Equilibrium.IO;
using Equilibrium.Meta;
using Equilibrium.Options;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Equilibrium.Models.Objects.Graphics {
    [PublicAPI]
    public record VertexData(
        uint VertexCount,
        List<ChannelInfo> Channels) {
        private long DataStart { get; set; } = -1;

        [JsonIgnore]
        public Memory<byte>? Data { get; set; }

        public static VertexData Default { get; } = new(0, new List<ChannelInfo>());

        public bool ShouldDeserialize => Data == null;

        public static VertexData FromReader(BiEndianBinaryReader reader, SerializedFile file) => throw new NotImplementedException();

        public void ToWriter(BiEndianBinaryWriter writer, SerializedFile serializedFile, UnityVersion targetVersion) {
            throw new NotImplementedException();
        }

        public void Deserialize(BiEndianBinaryReader reader, ObjectDeserializationOptions options) {
            throw new NotImplementedException();
        }
    }
}
