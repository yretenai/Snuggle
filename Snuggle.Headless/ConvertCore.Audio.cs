using System.IO;
using Snuggle.Converters;
using Snuggle.Core.Implementations;
using Snuggle.Core.Interfaces;

namespace Snuggle.Headless;

public static partial class ConvertCore {
    public static void ConvertAudio(SnuggleFlags flags, ILogger logger, AudioClip clip) {
        var (ext, wav) = SnuggleAudioFile.GetExt(clip);
        if (flags.WriteNativeAudio) {
            wav = false;
        }

        if (!clip.Data.HasValue || clip.Data.Value.IsEmpty) {
            return;
        }

        var path = PathFormatter.Format(clip.HasContainerPath ? flags.OutputFormat : flags.ContainerlessOutputFormat ?? flags.OutputFormat, wav ? "wav" : ext[1..], clip);
        var pcm = wav ? SnuggleAudioFile.BuildWAV(clip, logger) : clip.Data.Value.Span;
        File.WriteAllBytes(path, pcm.ToArray());
        logger.Info($"Saved {path}");
    }
}
