using Equilibrium.Implementations;
using Equilibrium.IO;
using JetBrains.Annotations;

namespace Equilibrium.Models {
    [PublicAPI]
    public record PPtr<T>(
        int FileId,
        ulong PathId) where T : SerializedObject {
        private T? UnderlyingValue { get; set; }

        public SerializedFile? Host { get; set; }

        public static PPtr<T> FromReader(BiEndianBinaryReader reader, SerializedFile host) {
            return new(reader.ReadInt32(), host.Header.BigIdEnabled ? reader.ReadUInt64() : reader.ReadUInt32()) { Host = host };
        }

        public bool IsNull { get; } = FileId < 0 || PathId == 0;

        public T? Value {
            get {
                if (IsNull ||
                    Host == null ||
                    FileId >= Host.ExternalInfos.Length ||
                    Host.Assets == null ||
                    State == PPtrState.Failed) {
                    return null;
                }

                if (State == PPtrState.Unloaded) {
                    if (!Host.Assets.Files.TryGetValue(Host.ExternalInfos[FileId].Name, out var referencedFile)) {
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

        public static implicit operator T?(PPtr<T> ptr) {
            return ptr.Value;
        }
    }
}
