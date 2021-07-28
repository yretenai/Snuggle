using System;
using System.Linq;
using Equilibrium.Implementations;
using Equilibrium.IO;
using Equilibrium.Meta;
using Equilibrium.Models.Serialization;
using JetBrains.Annotations;

namespace Equilibrium.Models {
    [PublicAPI]
    public record PPtr<T>(
        int FileId,
        long PathId) where T : SerializedObject {
        public static PPtr<T> Null => new(0, 0);
        private T? UnderlyingValue { get; set; }
        private UnityObjectInfo? UnderlyingInfo { get; set; }

        public SerializedFile? File { get; set; }

        public bool IsNull { get; } = FileId < 0 || PathId == 0;

        public UnityObjectInfo? Info {
            get {
                if (IsNull ||
                    File == null ||
                    FileId >= File.ExternalInfos.Length ||
                    File.Assets == null) {
                    return null;
                }

                if (UnderlyingInfo != null) {
                    return UnderlyingInfo;
                }

                SerializedFile? referencedFile;
                if (FileId == 0) {
                    referencedFile = File;
                } else if (!File.Assets.Files.TryGetValue(File.ExternalInfos[FileId - 1].Name, out referencedFile)) {
                    return null;
                }

                UnderlyingInfo = referencedFile.ObjectInfos.FirstOrDefault(x => x.PathId == PathId);
                return UnderlyingInfo;
            }
        }

        public T? Value {
            get {
                if (IsNull ||
                    File == null ||
                    FileId >= File.ExternalInfos.Length ||
                    File.Assets == null) {
                    return null;
                }

                if (UnderlyingValue != null) {
                    return UnderlyingValue;
                }

                SerializedFile? referencedFile;
                if (FileId == 0) {
                    referencedFile = File;
                } else if (!File.Assets.Files.TryGetValue(File.ExternalInfos[FileId - 1].Name, out referencedFile)) {
                    return null;
                }

                if (!referencedFile.Objects.TryGetValue(PathId, out var referencedObject)) {
                    return null;
                }

                if (referencedObject is not T referencedType) {
                    return null;
                }

                UnderlyingValue = referencedType;
                return UnderlyingValue;
            }
        }

        public void ToWriter(BiEndianBinaryWriter writer, SerializedFile file, UnityVersion? targetVersion) {
            writer.Write(FileId);
            if (File == null ||
                file.Header.BigIdEnabled) {
                writer.Write(PathId);
            } else {
                writer.Write((uint) PathId);
            }
        }

        public static PPtr<T> FromReader(BiEndianBinaryReader reader, SerializedFile file) => new(reader.ReadInt32(), file.Header.BigIdEnabled ? reader.ReadInt64() : reader.ReadInt32()) { File = file };
        public static implicit operator T?(PPtr<T> ptr) => ptr.Value;
        public override int GetHashCode() => HashCode.Combine(FileId, PathId);
        public override string ToString() => $"PPtr<{typeof(T).Name}> {{ FileId = {FileId}, PathId = {PathId} }}";
    }
}
