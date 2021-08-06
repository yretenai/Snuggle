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

        public static ChannelInfo FromReader(BiEndianBinaryReader reader, SerializedFile file) {
            var stream = reader.ReadByte();
            var offset = reader.ReadByte();
            var format = reader.ReadByte();
            var dimension = reader.ReadByte();
            return new ChannelInfo(stream, offset, format, dimension);
        }

        public void ToWriter(BiEndianBinaryWriter writer, SerializedFile serializedFile, UnityVersion targetVersion) {
            writer.Write(Stream);
            writer.Write(Offset);
            writer.Write(Format);
            writer.Write(Dimension);
        }
    }
}
