using Equilibrium.Implementations;

namespace Entropy.Handlers {
    public record EntropyObject(
        string Name,
        long PathId,
        object ClassId,
        string Container,
        long Size,
        string SerializedName) {
        public EntropyObject(SerializedObject file) : this(file.ToString(), file.PathId, file.ClassId, file.ObjectContainerPath, file.Size, file.SerializedFile.Name) { }

        public SerializedObject? GetObject() {
            if (!EntropyCore.Instance.Collection.Files.TryGetValue(SerializedName, out var serializedFile)) {
                return null;
            }

            serializedFile.Objects.TryGetValue(PathId, out var serializedObject);
            return serializedObject;
        }
    }
}
