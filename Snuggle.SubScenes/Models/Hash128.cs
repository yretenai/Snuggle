using System.Runtime.InteropServices;

namespace Snuggle.SubScenes.Models;

// Normally I'd just use System.Guid, but there's a bug in the way Hash128 is printed.
// Basically it's writing the hextets backwards. 0xAB gets written as "ba"
[StructLayout(LayoutKind.Sequential, Pack = 1, Size = 0x10)]
public readonly record struct Hash128(uint X, uint Y, uint Z, uint W) {
    private static readonly char[] hex = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f' };

    public override unsafe string ToString() {
        var chars = stackalloc char[32];

        for (var i = 0; i < 4; i++) {
            for (var j = 7; j >= 0; j--) {
                var cur = i switch {
                    0 => X,
                    1 => Y,
                    2 => Z,
                    _ => W,
                };
                cur >>= j * 4;
                cur &= 0xF;
                chars[i * 8 + j] = hex[cur];
            }
        }

        return new string(chars, 0, 32);
    }
}
