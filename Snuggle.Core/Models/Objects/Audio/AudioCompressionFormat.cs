using JetBrains.Annotations;

namespace Snuggle.Core.Models.Objects.Audio;

[PublicAPI]
public enum AudioCompressionFormat {
    PCM,
    Vorbis,
    ADPCM,
    MP3,
    VAG,
    HEVAG,
    XMA,
    AAC,
    GCADPCM,
    ATRAC9,
}
