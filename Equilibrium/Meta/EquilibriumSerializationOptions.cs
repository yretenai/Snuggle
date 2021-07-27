using Equilibrium.Models.Bundle;

namespace Equilibrium.Meta {
    public record EquilibriumSerializationOptions(
        int BlockSize,
        UnityCompressionType CompressionType,
        UnityCompressionType BlockCompressionType) {
        public static EquilibriumSerializationOptions Default { get; } = new(int.MaxValue, UnityCompressionType.None, UnityCompressionType.LZMA);
    }
}
