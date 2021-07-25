using System;
using Equilibrium.Implementations;
using Equilibrium.IO;
using JetBrains.Annotations;

namespace Equilibrium.Models {
    [PublicAPI]
    public record PPtr<T>(
        int FileId,
        long PathId) where T : SerializedObject {
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
                    SerializedFile? referencedFile;
                    if (FileId == 0) {
                        referencedFile = File;
                    } else if (!File.Assets.Files.TryGetValue(File.ExternalInfos[FileId - 1].Name, out referencedFile)) {
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

        public static PPtr<T> FromReader(BiEndianBinaryReader reader, SerializedFile file) => new(reader.ReadInt32(), file.Header.BigIdEnabled ? reader.ReadInt64() : reader.ReadInt32()) { File = file };
        public static implicit operator T?(PPtr<T> ptr) => ptr.Value;
        public override int GetHashCode() => HashCode.Combine(FileId, PathId);
        public override string ToString() => $"PPtr<{typeof(T).Name}> {{ FileId = {FileId}, PathId = {PathId} }}";
    }
}
