using JetBrains.Annotations;

namespace Equilibrium.Meta {
    [PublicAPI]
    public record MultiMetaInfo(object Tag, long Offset, long Size);
}
