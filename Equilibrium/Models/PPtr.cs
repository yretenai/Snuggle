using Equilibrium.Implementations;
using Equilibrium.IO;
using JetBrains.Annotations;

namespace Equilibrium.Models {
    [PublicAPI]
    public record PPtr<T>(
        int FileId,
        ulong PathId) where T : SerializedObject {
        private T? UnderlyingValue { get; set; }

        public SerializedFile? File { get; set; }

        public bool IsNull { get; } = FileId < 0 || PathId == 0;

        public T? Value {
            get {
                if (IsNull ||
                    File == null ||
                    FileId >= File.ExternalInfos.Length ||
                    File.Assets == null ||
                    State == PPtrState.Failed) {
                    return null;
                }

                if (State == PPtrState.Unloaded) {
                    if (!File.Assets.Files.TryGetValue(File.ExternalInfos[FileId].Name, out var referencedFile)) {
                        State = PPtrState.Failed;
                        return null;
                    }

                    if (!referencedFile.Objects.TryGetValue(PathId, out var referencedObject)) {
                        State = PPtrState.Failed;
                        return null;
                    }

                    if (referencedObject is not T referencedType) {
                        State = PPtrState.Failed;
                        return null;
                    }

                    UnderlyingValue = referencedType;
                    State = PPtrState.Loaded;
                }

                return UnderlyingValue;
            }
        }

        public PPtrState State { get; set; } = PPtrState.Unloaded;

        public static PPtr<T> FromReader(BiEndianBinaryReader reader, SerializedFile file) => new(reader.ReadInt32(), file.Header.BigIdEnabled ? reader.ReadUInt64() : reader.ReadUInt32()) { File = file };

        public static implicit operator T?(PPtr<T> ptr) => ptr.Value;
    }
}
