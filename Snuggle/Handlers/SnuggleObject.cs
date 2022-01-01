using System;
using Snuggle.Core.Implementations;

namespace Snuggle.Handlers;

public record SnuggleObject(
    string Name,
    long PathId,
    object ClassId,
    string Container,
    long Size,
    string SerializedName,
    string Meta) {
    public SnuggleObject(SerializedObject file) : this(
        file.ToString(),
        file.PathId,
        file.ClassId,
        file.ObjectContainerPath,
        file.Size,
        file.SerializedFile.Name,
        GetMeta(file)) { }

    private static string GetMeta(SerializedObject file) =>
        file switch {
            AudioClip clip => TimeSpan.FromMilliseconds(clip.Duration * 1000).ToString("g"),
            Texture2D texture2D => $"{texture2D.Width}x{texture2D.Height}",
            Sprite sprite => $"{sprite.Rect.W}x{sprite.Rect.H}",
            _ => string.Empty,
        };

    public SerializedObject? GetObject(bool baseType = false) {
        if (!SnuggleCore.Instance.Collection.Files.TryGetValue(SerializedName, out var serializedFile)) {
            return null;
        }

        return serializedFile.GetObject(PathId, baseType: baseType);
    }
}
