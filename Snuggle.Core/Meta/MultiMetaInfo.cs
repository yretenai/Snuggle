using JetBrains.Annotations;

namespace Snuggle.Core.Meta {
    [PublicAPI]
    public record MultiMetaInfo(object Tag, long Offset, long Size);
}
