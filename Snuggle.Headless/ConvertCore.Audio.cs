using System.IO;
using Snuggle.Converters;
using Snuggle.Core.Implementations;

namespace Snuggle.Headless;

public static partial class ConvertCore {
    public static void ConvertAudio(SnuggleFlags flags, AudioClip clip) {
        var (ext, supportsConversion) = SnuggleAudioFile.GetExt(clip);
        if (flags.WriteNativeAudio) {
            supportsConversion = false;
        }

        if (!clip.Data.HasValue || clip.Data.Value.IsEmpty) {
            return;
        }

        var path = PathFormatter.Format(clip.HasContainerPath ? flags.OutputFormat : flags.ContainerlessOutputFormat ?? flags.OutputFormat, supportsConversion ? "wav" : ext[1..], clip);
        var fullPath = Path.GetFullPath(path);
        if (!flags.Overwrite && File.Exists(fullPath)) {
            return;
        }

        var pcm = supportsConversion ? SnuggleAudioFile.GetPCM(clip, out ext) : clip.Data.Value;
        if (supportsConversion) {
            Path.ChangeExtension(fullPath, ext);
        }

        File.WriteAllBytes(fullPath, pcm.ToArray());
    }
}
