using System;
using Equilibrium.IO;
using Equilibrium.Meta;
using Equilibrium.Options;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Equilibrium.Models.Objects.Math {
    [PublicAPI]
    public record PackedBitInfo(
        uint Count,
        float Range,
        float Start,
        byte BitSize) {
        private long DataStart { get; set; } = -1;

        [JsonIgnore]
        public Memory<byte>? Data { get; set; }

        public static PackedBitInfo Default { get; } = new(0, 0, 0, 0);

        public bool ShouldDeserialize => Data == null;

        public static PackedBitInfo FromReader(BiEndianBinaryReader reader, SerializedFile file) => throw new NotImplementedException();

        public void ToWriter(BiEndianBinaryWriter writer, SerializedFile serializedFile, UnityVersion targetVersion) {
            throw new NotImplementedException();
        }

        public void Deserialize(BiEndianBinaryReader reader, ObjectDeserializationOptions options) {
            throw new NotImplementedException();
        }
    }
}
