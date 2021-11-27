using Snuggle.Core.Implementations;

namespace Snuggle.Handlers;

public record SnuggleObject(string Name, long PathId, object ClassId, string Container, long Size, string SerializedName) {
    public SnuggleObject(SerializedObject file) : this(file.ToString(), file.PathId, file.ClassId, file.ObjectContainerPath, file.Size, file.SerializedFile.Name) { }

    public SerializedObject? GetObject(bool baseType = false) {
        if (!SnuggleCore.Instance.Collection.Files.TryGetValue(SerializedName, out var serializedFile)) {
            return null;
        }

        return serializedFile.GetObject(PathId, baseType: baseType);
    }
}
