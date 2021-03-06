using System.Collections.Generic;
using System.Linq;
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
        var min = subsetIndices.Min();
        var max = subsetIndices.Max();
        return new StaticBatchInfo((ushort) min, (ushort) (max - min + 1));
    }
}
