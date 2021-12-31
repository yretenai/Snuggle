using JetBrains.Annotations;

namespace Snuggle.Core.Models.Objects.Audio;

[PublicAPI]
public enum AudioLoadType {
    DecompressOnLoad,
    CompressedInMemory,
    Streaming,
}
