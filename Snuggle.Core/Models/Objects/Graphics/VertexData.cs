using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;
using JetBrains.Annotations;
using Snuggle.Core.Exceptions;
using Snuggle.Core.IO;
using Snuggle.Core.Meta;
using Snuggle.Core.Options;

namespace Snuggle.Core.Models.Objects.Graphics {
    [PublicAPI]
    public enum VertexChannel {
        Vertex = 0,
        Normal = 1,
        Tangent = 2,
        Color = 3,
        UV0 = 4,
        UV1 = 5,
        UV2 = 6,
        UV3 = 7,
        UV4 = 8,
        UV5 = 9,
        UV6 = 10,
        UV7 = 11,
        SkinWeight = 12,
        SkinBoneIndex = 13,
    }

    [PublicAPI]
    public record VertexData(
        uint CurrentChannels,
        uint VertexCount,
        Dictionary<VertexChannel, ChannelInfo> Channels) {
        private long DataStart { get; init; } = -1;

        [JsonIgnore]
        public Memory<byte>? Data { get; set; }

        public static VertexData Default { get; } = new(0, 0, new Dictionary<VertexChannel, ChannelInfo>());

        [JsonIgnore]
        public bool ShouldDeserialize => Data == null;

        public static VertexData FromReader(BiEndianBinaryReader reader, SerializedFile file) {
            var currentChannels = 0U;
            if (file.Version < UnityVersionRegister.Unity2018) {
                currentChannels = reader.ReadUInt32();
            }

            var vertexCount = reader.ReadUInt32();

            var channelCount = reader.ReadInt32();
            var channels = new Dictionary<VertexChannel, ChannelInfo>();
            channels.EnsureCapacity(channelCount);

            for (var i = 0; i < channelCount; ++i) {
                var channel = (VertexChannel) i;
                if (i is >= 2 and <= 7 &&
                    file.Version < UnityVersionRegister.Unity2018) {
                    if (i == 7) {
                        channel = VertexChannel.Tangent;
                    } else {
                        channel += 1;
                    }
                }

                channels.Add(channel, ChannelInfo.FromReader(reader, file));
            }

            var dataStart = reader.BaseStream.Position;
            var dataCount = reader.ReadInt32();
            reader.BaseStream.Seek(dataCount, SeekOrigin.Current);
            reader.Align();

            return new VertexData(currentChannels, vertexCount, channels) { DataStart = dataStart };
        }

        public void ToWriter(BiEndianBinaryWriter writer, SerializedFile serializedFile, UnityVersion targetVersion) {
            if (ShouldDeserialize) {
                throw new IncompleteDeserializationException();
            }

            if (targetVersion < UnityVersionRegister.Unity2018) {
                writer.Write(CurrentChannels);
            }

            writer.Write(VertexCount);
            writer.Write(Channels.Count);

            foreach (var (_, channel) in Channels) {
                channel.ToWriter(writer, serializedFile, targetVersion);
            }

            writer.Write(Data!.Value.Length);
            writer.WriteMemory(Data);
        }

        public void Deserialize(BiEndianBinaryReader reader, SerializedFile serializedFile, ObjectDeserializationOptions options) {
            reader.BaseStream.Seek(DataStart, SeekOrigin.Begin);
            var dataCount = reader.ReadInt32();
            Data = reader.ReadMemory(dataCount);
        }
    }
}
