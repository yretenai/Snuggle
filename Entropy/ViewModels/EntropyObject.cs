using Equilibrium.Implementations;

namespace Entropy.ViewModels {
    public record EntropyObject(
        string Name,
        long PathId,
        object ClassId,
        string Container,
        long Size,
        string SerializedName) {
        public EntropyObject(SerializedObject file) : this(file.ToString(), file.PathId, file.ClassId, file.ObjectContainerPath, file.Size, file.SerializedFile.Name) { }
    }
}
