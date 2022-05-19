using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using Snuggle.Core.Implementations;
using Snuggle.Core.IO;
using Snuggle.Core.Meta;
using Snuggle.Core.Models.Serialization;

namespace Snuggle.Core.Models;

public record PPtr<T>(int FileId, long PathId) where T : SerializedObject {
    // ReSharper disable once UseDeconstructionOnParameter
    private PPtr(PPtrEnclosure enclosure) : this(enclosure.FileId, enclosure.PathId) { }

    public static PPtr<T> Null => new(0, 0);
    public static int Size => 12;
    public object? Tag => Info?.ClassId;
    
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
            if (IsNull || File == null || FileId > File.ExternalInfos.Length || File.Assets == null) {
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
            if (IsNull || File == null || FileId > File.ExternalInfos.Length || File.Assets == null) {
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

    private (long, string)? UnderlyingCompositeId { get; set; }

    public PPtr<U> As<U>() where U : SerializedObject => new(FileId, PathId) { File = File };

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

    public (long, string) GetCompositeId() {
        if (IsNull || File == null || FileId > File.ExternalInfos.Length || File.Assets == null) {
            return (0, "");
        }

        if (UnderlyingCompositeId != null) {
            return UnderlyingCompositeId.Value;
        }

        SerializedFile? referencedFile;
        if (FileId == 0) {
            referencedFile = File;
        } else if (!File.Assets.Files.TryGetValue(File.ExternalInfos[FileId - 1].Name, out referencedFile)) {
            File.Options.Logger.Warning("PPtr", $"Cannot find External File {File.ExternalInfos[FileId - 1].Name}");
            return (0, "");
        }

        UnderlyingCompositeId = (PathId, referencedFile.Name);
        return UnderlyingCompositeId.Value;
    }

    public bool IsSame(T? child) {
        if (IsNull || child == null || child.PathId != PathId || File == null || FileId > File.ExternalInfos.Length || File.Assets == null) {
            return false;
        }

        SerializedFile? referencedFile;
        if (FileId == 0) {
            referencedFile = File;
        } else if (!File.Assets.Files.TryGetValue(File.ExternalInfos[FileId - 1].Name, out referencedFile)) {
            File.Options.Logger.Warning("PPtr", $"Cannot find External File {File.ExternalInfos[FileId - 1].Name}");
            return false;
        }

        return referencedFile.Name == child.SerializedFile.Name;
    }

    [StructLayout(LayoutKind.Sequential, Size = 12, Pack = 1)]
    private record struct PPtrEnclosure(int FileId, long PathId);
}
