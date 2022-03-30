using Snuggle.Core.Implementations;
using Snuggle.Core.IO;
using Snuggle.Core.Models.Objects.Math;

namespace Snuggle.Core.Models.Objects.Graphics;

public record GUIStyle(
    string Name,
    GUIStyleState Normal,
    GUIStyleState Hover,
    GUIStyleState Active,
    GUIStyleState Focused,
    GUIStyleState OnNormal,
    GUIStyleState OnHover,
    GUIStyleState OnActive,
    GUIStyleState OnFocused,
    RectOffset Border,
    RectOffset Margin,
    RectOffset Padding,
    RectOffset Overflow,
    PPtr<SerializedObject> Font,
    int FontSize,
    int FontStyle,
    int Alignment,
    bool WordWrap,
    bool RichText,
    int Clipping,
    int Position,
    Vector2 Offset,
    Vector2 FixedSize,
    bool StretchWidth,
    bool StretchHegith) {
    public static GUIStyle FromReader(BiEndianBinaryReader reader, SerializedFile file) {
        var name = reader.ReadString32();
        var normal = GUIStyleState.FromReader(reader, file);
        var hover = GUIStyleState.FromReader(reader, file);
        var active = GUIStyleState.FromReader(reader, file);
        var focused = GUIStyleState.FromReader(reader, file);
        var onNormal = GUIStyleState.FromReader(reader, file);
        var onHover = GUIStyleState.FromReader(reader, file);
        var onActive = GUIStyleState.FromReader(reader, file);
        var onFocused = GUIStyleState.FromReader(reader, file);
        var border = reader.ReadStruct<RectOffset>();
        var margin = reader.ReadStruct<RectOffset>();
        var padding = reader.ReadStruct<RectOffset>();
        var overflow = reader.ReadStruct<RectOffset>();
        var font = PPtr<SerializedObject>.FromReader(reader, file);
        var fontSize = reader.ReadInt32();
        var style = reader.ReadInt32();
        var alignment = reader.ReadInt32();

        var wrap = reader.ReadBoolean();
        var rtf = reader.ReadBoolean();
        reader.Align();

        var clip = reader.ReadInt32();
        var pos = reader.ReadInt32();
        var offset = reader.ReadStruct<Vector2>();
        var size = reader.ReadStruct<Vector2>();

        var stretchWidth = reader.ReadBoolean();
        var stretchHeight = reader.ReadBoolean();
        reader.Align();

        return new GUIStyle(name, normal, hover, active, focused, onNormal, onHover, onActive, onFocused, border, margin, padding, overflow, font, fontSize, style, alignment, wrap, rtf, clip, pos, offset, size, stretchWidth, stretchHeight);
    }
}
