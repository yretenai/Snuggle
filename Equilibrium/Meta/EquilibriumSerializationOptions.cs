using Equilibrium.Models.Bundle;

namespace Equilibrium.Meta {
    public record EquilibriumSerializationOptions(
        int BlockSize,
        UnityCompressionType CompressionType) {
        public static EquilibriumSerializationOptions Default { get; } = new(0x20000, UnityCompressionType.None);
    }
}
