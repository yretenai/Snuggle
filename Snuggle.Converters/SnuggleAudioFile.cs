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
    public static Span<byte> GetPCM(AudioClip clip, ILogger? logger) {
        if (clip.ShouldDeserialize) {
            throw new IncompleteDeserialization();
        }

        if (!clip.Data.HasValue || clip.Data.Value.IsEmpty || clip.Data.Value.Length < 4) {
            return Span<byte>.Empty;
        }

        if (BinaryPrimitives.ReadUInt32BigEndian(clip.Data.Value.Span) == 0x46534235) { // 46534235 = 'FSB5' -- FMOD Sample Bank version 5
            return GetFMODPCM(clip, logger);
        }

        return Span<byte>.Empty;
    }

    private static unsafe Span<byte> GetFMODPCM(AudioClip clip, ILogger? logger) {
        SnuggleIntegration.Register();

        var result = Factory.System_Create(out var system);
        if (result != RESULT.OK) {
            logger?.Error("FMOD", Error.String(result));
            return Span<byte>.Empty;
        }

        // this is giving me VC++ COMPTR trauma flashbacks. 
        try {
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
                    return GetFMODPCM(sound, logger);
                }

                result = sound.getSubSound(clip.SubsoundIndex, out var subSound);
                if (result != RESULT.OK) {
                    logger?.Error("FMOD", Error.String(result));
                    return Span<byte>.Empty;
                }

                try {
                    return GetFMODPCM(subSound, logger);
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

    private static Span<byte> GetFMODPCM(Sound sound, ILogger? logger) {
        var result = sound.getLength(out var length, TIMEUNIT.PCMBYTES);
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
}
