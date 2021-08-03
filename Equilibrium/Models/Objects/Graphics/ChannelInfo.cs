using System;
using Equilibrium.IO;
using Equilibrium.Meta;
using JetBrains.Annotations;

namespace Equilibrium.Models.Objects.Graphics {
    [PublicAPI]
    public record ChannelInfo(
        byte Stream,
        byte Offset,
        byte Format,
        byte Dimension) {
        public static ChannelInfo Default { get; } = new(0, 0, 0, 0);

        public static ChannelInfo FromReader(BiEndianBinaryReader reader, SerializedFile file) => throw new NotImplementedException();

        public void ToWriter(BiEndianBinaryWriter writer, SerializedFile serializedFile, UnityVersion targetVersion) {
            throw new NotImplementedException();
        }
    }
}
