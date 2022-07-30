using System.Collections.Generic;
using System.Linq;
using Snuggle.Core.IO;
using Snuggle.Core.Meta;
using Snuggle.Core.Models.Objects.Math;

namespace Snuggle.Core.Models.Objects.Graphics;

public record Gradient(List<ColorRGBA> Colors, List<ushort> ColorStops, List<ushort> AlphaStops, int Mode, byte ColorCount, byte AlphaCount) {
    public static Gradient FromReader(BiEndianBinaryReader reader, SerializedFile file) {
        var colors = new List<ColorRGBA>();
        colors.AddRange(file.Version < UnityVersionRegister.Unity5_6 ? reader.ReadArray<Color32>(8).Select(x => x.ToRGBA()) : reader.ReadArray<ColorRGBA>(8));
        var cs = new List<ushort>();
        cs.AddRange(reader.ReadArray<ushort>(8));
        var @as = new List<ushort>();
        @as.AddRange(reader.ReadArray<ushort>(8));
        var mode = -1;
        if (file.Version >= UnityVersionRegister.Unity5_5) {
            mode = reader.ReadInt32();
        }

        var cz = reader.ReadByte();
        var az = reader.ReadByte();
        return new Gradient(colors, cs, @as, mode, cz, az);
    }
}
