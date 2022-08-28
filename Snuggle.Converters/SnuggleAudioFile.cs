using System;
using System.Buffers.Binary;
using System.IO;
using Fmod5Sharp;
using Snuggle.Core.Exceptions;
using Snuggle.Core.Implementations;
using Snuggle.Core.Options;
namespace Snuggle.Converters;

public static class SnuggleAudioFile {

    public static (string ext, bool supportsConversion) GetExt(AudioClip clip) {
        if (clip.ShouldDeserialize) {
            throw new IncompleteDeserialization();
        }

        if (!clip.Data.HasValue || clip.Data.Value.IsEmpty || clip.Data.Value.Length < 4) {
            return (".audio", false);
        }

        if (BinaryPrimitives.ReadUInt32BigEndian(clip.Data.Value.Span) == 0x46534235) { // 46534235 = 'FSB5' -- FMOD Sample Bank version 5
            return (".fsb", true);
        }

        // ReSharper disable once ConvertIfStatementToReturnStatement
        if (BinaryPrimitives.ReadUInt16BigEndian(clip.Data.Value.Span) is >= 0xFFF0 and <= 0xFFFF) {
            return (".m4a", false);
        }

        return (".audio", false);
    }

    public static string Save(AudioClip clip, string path, SnuggleExportOptions options) {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir)) {
            Directory.CreateDirectory(dir);
        }

        var (ext, canConvert) = GetExt(clip);
        path = Path.ChangeExtension(path, ext);
        if (!clip.Data.HasValue || clip.Data.Value.IsEmpty) {
            return path;
        }

        if (!options.WriteNativeAudio && canConvert) {
            var pcm = GetPCM(clip, out ext);
            var wavPath = Path.ChangeExtension(path, "." + ext);
            if (pcm.Length > 0) {
                File.WriteAllBytes(wavPath, pcm);
                return wavPath;
            }
        }

        File.WriteAllBytes(path, clip.Data.Value.ToArray());
        return path;
    }

    public static byte[] GetPCM(AudioClip clip, out string? ext) {
        if (clip.ShouldDeserialize) {
            throw new IncompleteDeserialization();
        }

        ext = null;

        if (!clip.Data.HasValue || clip.Data.Value.IsEmpty || clip.Data.Value.Length < 4) {
            return Array.Empty<byte>();
        }

        // ReSharper disable once ConvertIfStatementToReturnStatement
        if (BinaryPrimitives.ReadUInt32BigEndian(clip.Data.Value.Span) == 0x46534235) { // 46534235 = 'FSB5' -- FMOD Sample Bank version 5
            return GetFMODPCM(clip, out ext);
        }

        return Array.Empty<byte>();
    }

    // ReSharper disable once RedundantAssignment
    private static byte[] GetFMODPCM(AudioClip clip, out string? ext) {
        var fsb = FsbLoader.LoadFsbFromByteArray(clip.Data!.Value.ToArray());
        var sample = fsb.Samples[0];
        return sample.RebuildAsStandardFileFormat(out var data, out ext) ? data : Array.Empty<byte>();
    }

}
