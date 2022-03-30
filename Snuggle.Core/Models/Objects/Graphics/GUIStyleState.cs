using Snuggle.Core.Implementations;
using Snuggle.Core.IO;
using Snuggle.Core.Models.Objects.Math;

namespace Snuggle.Core.Models.Objects.Graphics;

public record GUIStyleState(PPtr<Texture2D> Background, ColorRGBA Color) {
    public static GUIStyleState FromReader(BiEndianBinaryReader reader, SerializedFile file) {
        var ptr = PPtr<Texture2D>.FromReader(reader, file);
        var color = reader.ReadStruct<ColorRGBA>();
        return new GUIStyleState(ptr, color);
    }
}
