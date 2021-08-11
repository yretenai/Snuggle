using System.Collections.Generic;
using System.Linq;
using Equilibrium.IO;
using JetBrains.Annotations;

namespace Equilibrium.Models.Objects.Graphics {
    [PublicAPI]
    public record StaticBatchInfo(
        ushort FirstSubmesh,
        ushort SubmeshCount) {
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
}
