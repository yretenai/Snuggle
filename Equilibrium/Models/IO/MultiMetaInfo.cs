using JetBrains.Annotations;

namespace Equilibrium.Models.IO {
    [PublicAPI]
    public record MultiMetaInfo(object Tag, long Offset, long Size);
}
