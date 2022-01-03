using System;
using System.Text;
using JetBrains.Annotations;

namespace Snuggle.Core;

[PublicAPI]
public static class CRC {
    private const uint Polynomial = 0xEDB88320;
    public static readonly uint[] Table;

    static CRC() {
        Table = new uint[256];
        for (uint i = 0; i < 256; i++) {
            var r = i;
            for (var j = 0; j < 8; j++) {
                if ((r & 1) != 0) {
                    r = (r >> 1) ^ Polynomial;
                } else {
                    r >>= 1;
                }
            }

            Table[i] = r;
        }
    }

    public static uint GetDigest(Span<byte> data, uint value = 0) {
        value ^= 0xFFFFFFFF;

        foreach (var t in data) {
            value = Table[(byte) value ^ t] ^ (value >> 8);
        }

        return value ^ 0xFFFFFFFF;
    }

    public static uint GetDigest(string str, uint value = 0, Encoding? encoding = null) {
        encoding ??= Encoding.UTF8;

        return GetDigest(encoding.GetBytes(str).AsSpan(), value);
    }
}
