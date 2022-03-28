using System;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using FMOD;
using JetBrains.Annotations;
using Snuggle.Core.Exceptions;
using Snuggle.Core.Implementations;
using Snuggle.Core.Interfaces;
using Snuggle.Core.Options;
using Snuggle.Native;

namespace Snuggle.Converters;

[PublicAPI]
public static class SnuggleAudioFile {
    public enum WaveFormatType : ushort {
        PCM = 1,
        Float = 3,
        ALaw = 6,
        MuLaw = 7,
    }

    public static Span<byte> BuildWAV(AudioClip clip, ILogger? logger) {
        var pcm = GetPCM(clip, logger, out var info);
        if (pcm.IsEmpty) {
            return Span<byte>.Empty;
        }

        var buffer = new byte[pcm.Length + 44].AsSpan();
        var riff = new WaveRIFF(buffer.Length);
        MemoryMarshal.Write(buffer, ref riff);
        var ofs = Unsafe.SizeOf<WaveRIFF>();

        var format = new WaveFormat(info);
        MemoryMarshal.Write(buffer[ofs..], ref format);
        ofs += Unsafe.SizeOf<WaveFormat>();

        var chunk = new WaveChunk(WaveFormat.DATA, pcm.Length);
        MemoryMarshal.Write(buffer[ofs..], ref chunk);
        ofs += Unsafe.SizeOf<WaveChunk>();
        pcm.CopyTo(buffer[ofs..]);

        return buffer;
    }

    public static (string ext, bool supportsWav) GetExt(AudioClip clip) {
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

    public static string Save(AudioClip clip, string path, SnuggleExportOptions options, ILogger? logger = null) {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir)) {
            Directory.CreateDirectory(dir);
        }

        var (ext, wav) = GetExt(clip);
        path = Path.ChangeExtension(path, ext);
        if (!clip.Data.HasValue || clip.Data.Value.IsEmpty) {
            return path;
        }

        if (!options.WriteNativeAudio && wav) {
            var wavPath = Path.ChangeExtension(path, ".wav");
            var pcm = BuildWAV(clip, logger);
            if (!pcm.IsEmpty) {
                File.WriteAllBytes(wavPath, pcm.ToArray());
                return wavPath;
            }
        }

        File.WriteAllBytes(path, clip.Data.Value.ToArray());
        return path;
    }

    public static Span<byte> GetPCM(AudioClip clip, ILogger? logger, out AudioInfo info) {
        if (clip.ShouldDeserialize) {
            throw new IncompleteDeserialization();
        }

        info = AudioInfo.Default;

        if (!clip.Data.HasValue || clip.Data.Value.IsEmpty || clip.Data.Value.Length < 4) {
            return Span<byte>.Empty;
        }

        // ReSharper disable once ConvertIfStatementToReturnStatement
        if (BinaryPrimitives.ReadUInt32BigEndian(clip.Data.Value.Span) == 0x46534235) { // 46534235 = 'FSB5' -- FMOD Sample Bank version 5
            return GetFMODPCM(clip, logger, ref info);
        }

        return Span<byte>.Empty;
    }

    // ReSharper disable once RedundantAssignment
    private static unsafe Span<byte> GetFMODPCM(AudioClip clip, ILogger? logger, ref AudioInfo info) {
        SnuggleIntegration.Register();

        info = AudioInfo.Default;

        var result = Factory.System_Create(out var system);
        if (result != RESULT.OK) {
            logger?.Error("FMOD", Error.String(result));
            return Span<byte>.Empty;
        }

        // this is giving me VC++ COMPTR trauma flashbacks. 
        try {
            result = system.init(clip.Channels, INITFLAGS.NORMAL, IntPtr.Zero);
            if (result != RESULT.OK) {
                logger?.Error("FMOD", Error.String(result));
                return Span<byte>.Empty;
            }

            var exinfo = new CREATESOUNDEXINFO { cbsize = Unsafe.SizeOf<CREATESOUNDEXINFO>(), length = (uint) clip.Data!.Value.Length };
            using var pinned = clip.Data.Value.Pin();
            result = system.createSound((IntPtr) pinned.Pointer, MODE.OPENMEMORY, ref exinfo, out var sound);
            if (result != RESULT.OK) {
                logger?.Error("FMOD", Error.String(result));
                return Span<byte>.Empty;
            }

            try {
                result = sound.getNumSubSounds(out var numSubSounds);
                if (result != RESULT.OK) {
                    return null;
                }

                if (numSubSounds == 0 || numSubSounds < clip.SubsoundIndex) {
                    return GetFMODPCM(sound, logger, ref info);
                }

                result = sound.getSubSound(clip.SubsoundIndex, out var subSound);
                if (result != RESULT.OK) {
                    logger?.Error("FMOD", Error.String(result));
                    return Span<byte>.Empty;
                }

                try {
                    return GetFMODPCM(subSound, logger, ref info);
                } finally {
                    subSound.release();
                }
            } finally {
                sound.release();
            }
        } finally {
            system.release();
        }
    }

    private static Span<byte> GetFMODPCM(Sound sound, ILogger? logger, ref AudioInfo info) {
        var result = sound.getFormat(out _, out var format, out var channels, out var bits);
        if (result != RESULT.OK) {
            logger?.Error("FMOD", Error.String(result));
            return Span<byte>.Empty;
        }

        result = sound.getDefaults(out var frequency, out _);
        if (result != RESULT.OK) {
            logger?.Error("FMOD", Error.String(result));
            return Span<byte>.Empty;
        }

        info = new AudioInfo((int) frequency, bits, channels, format == SOUND_FORMAT.PCMFLOAT);

        result = sound.getLength(out var length, TIMEUNIT.PCMBYTES);
        if (result != RESULT.OK) {
            logger?.Error("FMOD", Error.String(result));
            return Span<byte>.Empty;
        }

        result = sound.@lock(0, length, out var ptr1, out var ptr2, out var len1, out var len2);
        if (result != RESULT.OK) {
            logger?.Error("FMOD", Error.String(result));
            return null;
        }

        try {
            var data = new byte[len1];
            Marshal.Copy(ptr1, data, 0, (int) len1);
            return data;
        } finally {
            sound.unlock(ptr1, ptr2, len1, len2);
        }
    }

    [PublicAPI]
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    private record struct WaveRIFF(uint Id, int Size, uint Format) {
        public WaveRIFF(int size) : this(0x46464952, size, 0x45564157) { }
    }

    [PublicAPI]
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    private record struct WaveChunk(uint Id, int Size);

    [PublicAPI]
    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    private record struct WaveFormat(
        uint Id,
        int Size,
        WaveFormatType Type,
        ushort Channels,
        int SampleRate,
        int ByteRate,
        ushort BlockAlign,
        ushort BitRate) {
        public const uint DATA = 0x61746164;
        public WaveFormat(WaveFormatType type, ushort channels, int sampleRate, ushort bitRate) : this(type, channels, sampleRate, bitRate * sampleRate * channels / 8, (ushort) (bitRate * channels / 8), bitRate) { }

        public WaveFormat(WaveFormatType type, ushort channels, int sampleRate, int byteRate, ushort BlockAlign, ushort bitRate) : this(
            0x20746D66,
            16,
            type,
            channels,
            sampleRate,
            byteRate,
            BlockAlign,
            bitRate) { }

        public WaveFormat(AudioInfo info) : this(info.isFloat ? WaveFormatType.Float : WaveFormatType.PCM, (ushort) info.Channels, info.Frequency, (ushort) info.Bits) { }
    }

    public record AudioInfo(int Frequency, int Bits, int Channels, bool isFloat) {
        public static AudioInfo Default { get; } = new(44100, 16, 1, false);
    }
}
