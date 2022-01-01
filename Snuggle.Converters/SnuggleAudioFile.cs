using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using FMOD;
using Snuggle.Core.Exceptions;
using Snuggle.Core.Implementations;
using Snuggle.Core.Interfaces;
using Snuggle.Native;

namespace Snuggle.Converters;

public static class SnuggleAudioFile {
    public static Span<byte> GetPCM(AudioClip clip, ILogger? logger, out AudioInfo info) {
        if (clip.ShouldDeserialize) {
            throw new IncompleteDeserialization();
        }

        info = AudioInfo.Default;

        if (!clip.Data.HasValue || clip.Data.Value.IsEmpty || clip.Data.Value.Length < 4) {
            return Span<byte>.Empty;
        }

        if (BinaryPrimitives.ReadUInt32BigEndian(clip.Data.Value.Span) == 0x46534235) { // 46534235 = 'FSB5' -- FMOD Sample Bank version 5
            return GetFMODPCM(clip, logger, ref info);
        }

        return Span<byte>.Empty;
    }

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

    public record AudioInfo(int Frequency, int Bits, int Channels, bool isFloat) {
        public static AudioInfo Default { get; } = new(44100, 16, 1, false);
    }
}
