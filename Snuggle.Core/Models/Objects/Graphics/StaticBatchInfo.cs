using System.Collections.Generic;
using Snuggle.Core.IO;

namespace Snuggle.Core.Models.Objects.Graphics;

public record StaticBatchInfo(ushort FirstSubmesh, ushort SubmeshCount) {
    public static StaticBatchInfo Default { get; } = new(0, 1);

    public static StaticBatchInfo FromReader(BiEndianBinaryReader reader, SerializedFile file) {
        var first = reader.ReadUInt16();
        var count = reader.ReadUInt16();

        return new StaticBatchInfo(first, count);
    }

    public static StaticBatchInfo FromSubsetIndices(List<int> subsetIndices) {
        var min = ushort.MaxValue;
        var max = ushort.MinValue;
        foreach (var indice in subsetIndices) {
            if (indice < min) {
                min = (ushort) indice;
            }

            if (indice > max) {
                max = (ushort) indice;
            }
        }

        return new StaticBatchInfo(min, (ushort) (max - min + 1));
    }
}
