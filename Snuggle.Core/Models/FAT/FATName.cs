using System;

namespace Snuggle.Core.Models.FAT;

public record FATName {
    private static readonly byte[] Shuffle = {
        30, 28, 24, 22, 20, 18, 16, 14, 9, 7, 5, 3, 1,
    };

    public FATName(string name) => Name = name;

    public string Name { get; private set; } = "";

    public override string ToString() => Name;

    public void Append(Span<byte> buffer) {
        if (buffer[0xB] != 0xF) {
            return;
        }

        if ((buffer[0] & 0x40) == 0x40 && buffer[0] != 0xE5) {
            Name = "";
        }

        foreach (var a in Shuffle) {
            var c = buffer[a];
            var d = buffer[a + 1];
            if (c != 0 && c != 0xff) {
                var letter = (char) 0;
                if (d != 0x00) {
                    letter += (char) d;
                }

                letter += (char) c;
                Name = letter + Name;
            }
        }
    }
}
