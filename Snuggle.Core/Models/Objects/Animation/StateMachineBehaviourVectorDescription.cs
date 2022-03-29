using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;
using Snuggle.Core.Exceptions;
using Snuggle.Core.IO;
using Snuggle.Core.Meta;
using Snuggle.Core.Options;

namespace Snuggle.Core.Models.Objects.Animation;

public record StateMachineBehaviourVectorDescription(Dictionary<StateKey, StateRange> BehaviourRanges) {
    public static StateMachineBehaviourVectorDescription Default { get; } = new(new Dictionary<StateKey, StateRange>());

    private long IndicesStart { get; init; } = -1;
    public Memory<uint>? Indices { get; set; }
    private bool ShouldDeserializeIndices => IndicesStart > -1 && Indices == null;

    [JsonIgnore]
    public bool ShouldDeserialize => ShouldDeserializeIndices;

    public static StateMachineBehaviourVectorDescription FromReader(BiEndianBinaryReader reader, SerializedFile file) {
        var count = reader.ReadInt32();
        var ranges = new Dictionary<StateKey, StateRange>();
        ranges.EnsureCapacity(count);
        for (var i = 0; i < ranges.Count; ++i) {
            ranges[StateKey.FromReader(reader, file)] = StateRange.FromReader(reader, file);
        }

        var start = reader.BaseStream.Position;
        count = reader.ReadInt32();
        reader.BaseStream.Seek(count * 4, SeekOrigin.Current);
        return new StateMachineBehaviourVectorDescription(ranges) { IndicesStart = start };
    }

    public void ToWriter(BiEndianBinaryWriter writer, SerializedFile serializedFile, UnityVersion targetVersion) {
        if (ShouldDeserialize) {
            throw new IncompleteDeserialization();
        }

        writer.Write(BehaviourRanges.Count);
        foreach (var (key, value) in BehaviourRanges) {
            key.ToWriter(writer, serializedFile, targetVersion);
            value.ToWriter(writer, serializedFile, targetVersion);
        }

        writer.WriteMemory(Indices);
    }

    public void Deserialize(BiEndianBinaryReader reader, SerializedFile serializedFile, ObjectDeserializationOptions options) {
        if (ShouldDeserializeIndices) {
            reader.BaseStream.Seek(IndicesStart, SeekOrigin.Begin);
            var dataCount = reader.ReadInt32();
            Indices = reader.ReadMemory<uint>(dataCount);
        }
    }

    public void Free() {
        Indices = null;
    }
}
