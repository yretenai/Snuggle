using System;
using System.Linq;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using Snuggle.Core.IO;
using Snuggle.Core.Meta;

namespace Snuggle.Core.Models.Objects.Graphics {
    [PublicAPI]
    public record ChannelInfo(
        int Stream,
        int Offset,
        VertexFormat Format,
        VertexDimension Dimension,
        int ExtraData) {
        public static ChannelInfo Default { get; } = new(0, 0, VertexFormat.Single, VertexDimension.None, 0);

        public static ChannelInfo FromReader(BiEndianBinaryReader reader, SerializedFile file) {
            var stream = reader.ReadByte();
            var offset = reader.ReadByte();
            var format = (VertexFormat) reader.ReadByte();
            if (file.Version >= UnityVersionRegister.Unity2019 &&
                format >= VertexFormat.Color) { // Color removed in 2019.1
                format += 1;
            }

            if (file.Version < UnityVersionRegister.Unity2017) { // Format rew orked in2017.1
                format = format switch {
                    >= VertexFormat.SNorm8 => VertexFormat.UInt32,
                    VertexFormat.UNorm8 => VertexFormat.UInt8,
                    _ => format,
                };
            }

            var dimension = reader.ReadByte();
            return new ChannelInfo(stream, offset, format, (VertexDimension) (dimension & 0xF), dimension & ~0xF);
        }

        public void ToWriter(BiEndianBinaryWriter writer, SerializedFile serializedFile, UnityVersion targetVersion) {
            writer.Write((byte) Stream);
            writer.Write((byte) Offset);
            writer.Write((byte) Format);
            writer.Write((byte) (((int) Dimension | ExtraData) & 0xFF));
        }

        public int GetSize() {
            var valueSize = Format switch {
                VertexFormat.UNorm8 => 1,
                VertexFormat.SNorm8 => 1,
                VertexFormat.UInt8 => 1,
                VertexFormat.SInt8 => 1,
                VertexFormat.Half => 2,
                VertexFormat.UNorm16 => 2,
                VertexFormat.SNorm16 => 2,
                VertexFormat.UInt16 => 2,
                VertexFormat.SInt16 => 2,
                VertexFormat.Single => 4,
                VertexFormat.Color => 1,
                VertexFormat.UInt32 => 4,
                VertexFormat.SInt32 => 4,
                _ => 0,
            };
            return valueSize * (int) Dimension;
        }

        public object[] Unpack(ref Span<byte> dataSpan) {
            var data = dataSpan[..GetSize()];
            return Format switch {
                VertexFormat.Single => MemoryMarshal.Cast<byte, float>(data).ToArray().Cast<object>().ToArray(),
                VertexFormat.Half => MemoryMarshal.Cast<byte, Half>(data).ToArray().Select(x => (float) x).Cast<object>().ToArray(),
                VertexFormat.Color => MemoryMarshal.Cast<byte, uint>(data).ToArray().SelectMany(Utils.UnwrapRGBA).Cast<object>().ToArray(),
                VertexFormat.UNorm8 => data.ToArray().Select(x => x / (float) byte.MaxValue).Cast<object>().ToArray(),
                VertexFormat.SNorm8 => data.ToArray().Select(x => (sbyte) x / (float) sbyte.MaxValue).Cast<object>().ToArray(),
                VertexFormat.UNorm16 => MemoryMarshal.Cast<byte, ushort>(data).ToArray().Select(x => x / (float) ushort.MaxValue).Cast<object>().ToArray(),
                VertexFormat.SNorm16 => MemoryMarshal.Cast<byte, short>(data).ToArray().Select(x => x / (float) short.MaxValue).Cast<object>().ToArray(),
                VertexFormat.UInt8 => data.ToArray().Cast<object>().ToArray(),
                VertexFormat.SInt8 => data.ToArray().Select(x => (sbyte) x).Cast<object>().ToArray(),
                VertexFormat.UInt16 => MemoryMarshal.Cast<byte, ushort>(data).ToArray().Cast<object>().ToArray(),
                VertexFormat.SInt16 => MemoryMarshal.Cast<byte, short>(data).ToArray().Cast<object>().ToArray(),
                VertexFormat.UInt32 => MemoryMarshal.Cast<byte, uint>(data).ToArray().Cast<object>().ToArray(),
                VertexFormat.SInt32 => MemoryMarshal.Cast<byte, int>(data).ToArray().Cast<object>().ToArray(),
                _ => throw new NotSupportedException(),
            };
        }
    }
}
