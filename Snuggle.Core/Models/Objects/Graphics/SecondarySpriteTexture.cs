using JetBrains.Annotations;
using Snuggle.Core.Implementations;
using Snuggle.Core.IO;
using Snuggle.Core.Meta;

namespace Snuggle.Core.Models.Objects.Graphics;

[PublicAPI]
public record SecondarySpriteTexture(PPtr<Texture2D> Texture, string Name) {
    public static SecondarySpriteTexture Default = new(PPtr<Texture2D>.Null, string.Empty);

    public static SecondarySpriteTexture FromReader(BiEndianBinaryReader reader, SerializedFile file) {
        var texture = PPtr<Texture2D>.FromReader(reader, file);
        var name = reader.ReadString32();
        return new SecondarySpriteTexture(texture, name);
    }

    public void ToWriter(BiEndianBinaryWriter writer, SerializedFile serializedFile, UnityVersion targetVersion) {
        Texture.ToWriter(writer, serializedFile, targetVersion);
        writer.WriteString32(Name);
    }
}
