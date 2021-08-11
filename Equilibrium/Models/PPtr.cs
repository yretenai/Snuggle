using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
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
        private PPtr(PPtrEnclosure enclosure) : this(enclosure.FileId, enclosure.PathId) { }

        public static PPtr<T> Null => new(0, 0);

        [JsonIgnore]
        private T? UnderlyingValue { get; set; }

        [JsonIgnore]
        private UnityObjectInfo? UnderlyingInfo { get; set; }

        [JsonIgnore]
        public SerializedFile? File { get; set; }

        [JsonIgnore]
        public bool IsNull { get; } = FileId < 0 || PathId == 0;

        [JsonIgnore]
        public UnityObjectInfo? Info {
            get {
                if (IsNull ||
                    File == null ||
                    FileId > File.ExternalInfos.Length ||
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

                if (referencedFile.ObjectInfos.TryGetValue(PathId, out var info)) {
                    UnderlyingInfo = info;
                }

                return UnderlyingInfo;
            }
        }

        [JsonIgnore]
        public T? Value {
            get {
                if (IsNull ||
                    File == null ||
                    FileId > File.ExternalInfos.Length ||
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
                    File.Options.Logger.Warning("PPtr", $"Cannot find External File {File.ExternalInfos[FileId - 1].Name}");
                    return null;
                }

                var referencedObject = referencedFile.GetObject(PathId);

                if (referencedObject is not T referencedType) {
                    return null;
                }

                UnderlyingValue = referencedType;
                return UnderlyingValue;
            }
        }

        public void ToWriter(BiEndianBinaryWriter writer, SerializedFile file, UnityVersion targetVersion) {
            writer.Write(FileId);
            writer.Write(PathId);
        }

        public static PPtr<T> FromReader(BiEndianBinaryReader reader, SerializedFile file) => new(reader.ReadStruct<PPtrEnclosure>()) { File = file };

        public static IEnumerable<PPtr<T>> ArrayFromReader(BiEndianBinaryReader reader, SerializedFile file, int count) {
            return count == 0 ? Array.Empty<PPtr<T>>() : reader.ReadArray<PPtrEnclosure>(count).ToArray().Select(x => new PPtr<T>(x) { File = file });
        }

        public static implicit operator T?(PPtr<T> ptr) => ptr.Value;
        public override int GetHashCode() => HashCode.Combine(FileId, PathId);
        public override string ToString() => $"PPtr<{typeof(T).Name}> {{ FileId = {FileId}, PathId = {PathId} }}";

        [StructLayout(LayoutKind.Sequential, Size = 12, Pack = 1)]
        private record struct PPtrEnclosure(int FileId, long PathId);
    }
}
