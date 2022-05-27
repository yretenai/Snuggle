using System;
using Snuggle.Core.Implementations;
using Snuggle.Core.Interfaces;

namespace Snuggle.Handlers;

public readonly record struct SnuggleObject(
    string Name,
    long PathId,
    object ClassId,
    string Container,
    long Size,
    string SerializedName) {
    public SnuggleObject(ISerializedObject file) : this(
        file.ToString(),
        file.PathId,
        file.ClassId,
        file.ObjectContainerPath,
        file.Size,
        file.SerializedFile.Name) { }

    public string Meta =>
        GetObject() switch {
            AudioClip clip => TimeSpan.FromMilliseconds(clip.Duration * 1000).ToString("g"),
            ITexture texture2D => $"{texture2D.Width}x{texture2D.Height}x{texture2D.Depth}",
            Sprite sprite => $"{sprite.Rect.W}x{sprite.Rect.H}",
            _ => string.Empty,
        };

    public SerializedObject? GetObject(bool baseType = false) => !SnuggleCore.Instance.Collection.Files.TryGetValue(SerializedName, out var serializedFile) ? null : serializedFile.GetObject(PathId, baseType: baseType);
}
