using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using Snuggle.Core.Implementations;
using Snuggle.Core.Interfaces;
using Snuggle.Core.IO;
using Snuggle.Core.Meta;
using Snuggle.Core.Models.Serialization;
using Snuggle.Core.Options;

namespace Snuggle.Core; 

[PublicAPI]
public class SerializedFile : IRenewable {
    public SerializedFile(Stream dataStream, object tag, IFileHandler handler, SnuggleOptions options, bool leaveOpen = false) {
        try {
            Tag = tag;
            Handler = handler;
            Options = options;

            using var reader = new BiEndianBinaryReader(dataStream, true, leaveOpen);
            var header = UnitySerializedFile.FromReader(reader, Options);
            Types = UnitySerializedType.ArrayFromReader(reader, header, Options);
            ObjectInfos = UnityObjectInfo.ArrayFromReader(reader, ref header, Types, Options).ToDictionary(x => x.PathId);
            ScriptInfos = UnityScriptInfo.ArrayFromReader(reader, header, Options);
            ExternalInfos = UnityExternalInfo.ArrayFromReader(reader, header, Options);
            if (header.FileVersion >= UnitySerializedFileVersion.RefObject) {
                ReferenceTypes = UnitySerializedType.ArrayFromReader(reader, header, Options, true);
            }

            if (header.FileVersion >= UnitySerializedFileVersion.UserInformation) {
                UserInformation = reader.ReadNullString();
            }

            Header = header;

            Version = UnityVersion.Parse(header.EngineVersion);

            Objects = new Dictionary<long, SerializedObject>();
            Objects.EnsureCapacity(ObjectInfos.Count);
        } finally {
            if (!leaveOpen) {
                dataStream.Close();
            }
        }
    }

    public SnuggleOptions Options { get; init; }
    public UnitySerializedFile Header { get; init; }
    public UnitySerializedType[] Types { get; init; }
    public Dictionary<long, UnityObjectInfo> ObjectInfos { get; init; }
    public UnityScriptInfo[] ScriptInfos { get; init; }
    public UnityExternalInfo[] ExternalInfos { get; init; }
    public UnitySerializedType[] ReferenceTypes { get; init; } = Array.Empty<UnitySerializedType>();
    public string UserInformation { get; init; } = string.Empty;
    public UnityVersion Version { get; set; }
    public AssetCollection? Assets { get; set; }
    public string Name { get; set; } = string.Empty;

    private Dictionary<long, SerializedObject> Objects { get; init; }

    public object Tag { get; set; }
    public IFileHandler Handler { get; set; }

    public Stream OpenFile(long pathId) => OpenFile(ObjectInfos[pathId]);

    public Stream OpenFile(UnityObjectInfo info) => OpenFile(info, Handler.OpenFile(Tag));

    public Stream OpenFile(UnityObjectInfo info, Stream? stream, bool leaveOpen = false) => new OffsetStream(stream ?? Handler.OpenFile(Tag), info.Offset + Header.Offset, info.Size, leaveOpen) { Position = 0 };

    public bool ToStream(FileSerializationOptions serializationOptions, [MaybeNullWhen(false)] out Stream? bundleStream, [MaybeNullWhen(false)] out Stream? resourceStream) {
        bundleStream = null;
        resourceStream = null;

        if (serializationOptions.TargetVersion < UnityVersionRegister.Unity5 ||
            serializationOptions.TargetFileVersion < UnitySerializedFileVersion.InitialVersion) {
            serializationOptions = serializationOptions.MutateWithSerializedFile(this);
        }

        var (alignment, resourceDataThreshold, resourceSuffix, bundleTemplate) = serializationOptions;
        var targetVersion = serializationOptions.TargetVersion;
        var targetFileVersion = serializationOptions.TargetFileVersion;
        var isBundle = serializationOptions.IsBundle;
        var prefix = isBundle ? string.Format(bundleTemplate, Name) : "";

        var options = new AssetSerializationOptions(
            alignment,
            resourceDataThreshold,
            targetVersion,
            serializationOptions.TargetGame,
            targetFileVersion,
            prefix + Name,
            prefix + Name + resourceSuffix);

        // TODO.
        return false;
    }

    public static bool IsSerializedFile(Stream stream) {
        using var reader = new BiEndianBinaryReader(stream, true, true);
        return IsSerializedFile(reader);
    }

    public static bool IsSerializedFile(BiEndianBinaryReader reader) {
        var isBigEndian = reader.IsBigEndian;
        var pos = reader.BaseStream.Position;
        try {
            reader.IsBigEndian = true;
            var headerSize = reader.ReadInt32();
            if (headerSize < 0) {
                return false;
            }

            var totalSize = reader.ReadInt32();
            if (headerSize > totalSize) {
                return false;
            }

            if (totalSize > reader.BaseStream.Length) {
                return false;
            }

            var version = (UnitySerializedFileVersion) reader.ReadUInt32();
            if (version > UnitySerializedFileVersion.Latest) {
                return false;
            }

            return totalSize >= reader.ReadInt32();
        } catch {
            return false;
        } finally {
            reader.IsBigEndian = isBigEndian;
            reader.BaseStream.Seek(pos, SeekOrigin.Begin);
        }
    }

    public void FindResources(SerializedObject? resourceManager) {
        foreach (var serializedObject in Objects.Values) {
            if (serializedObject is ICABPathProvider cab) {
                foreach (var (pPtr, path) in cab.GetCABPaths()) {
                    if (pPtr.Value != null) {
                        pPtr.Value.ObjectContainerPath = path;
                    }
                }
            }

            if (serializedObject is PlayerSettings settings && Assets != null) {
                Assets.PlayerSettings = settings;
            }
        }
    }

    public SerializedObject? GetObject(long pathId, SnuggleOptions? options = null, Stream? dataStream = null) => !ObjectInfos.ContainsKey(pathId) ? null : GetObject(ObjectInfos[pathId], options, dataStream);

    public SerializedObject? GetObject(UnityObjectInfo objectInfo, SnuggleOptions? options = null, Stream? dataStream = null) {
        if (!ObjectInfos.ContainsKey(objectInfo.PathId)) {
            return null;
        }

        options ??= Options;
        try {
            var shouldLoad = !options.LoadOnDemand;
            if (Objects.TryGetValue(objectInfo.PathId, out var serializedObject)) {
                if (!serializedObject.NeedsLoad) {
                    return serializedObject;
                }

                shouldLoad = true;
            }

            if (shouldLoad) {
                serializedObject = ObjectFactory.GetInstance(OpenFile(objectInfo, dataStream, dataStream != null), objectInfo, this);
            } else {
                serializedObject = new SerializedObject(objectInfo, this) { NeedsLoad = true };
            }

            Objects[objectInfo.PathId] = serializedObject;
            return serializedObject;
        } catch (Exception e) {
            options.Logger.Error("Serialized", $"Failed to decode {objectInfo.PathId} (type {objectInfo.ClassId}) on file {Name}", e);
            return null;
        }
    }

    public IEnumerable<SerializedObject> GetAllObjects() => Objects.Values;

    public void Free() {
        foreach (var (_, serializedObject) in Objects) {
            serializedObject.Free();
        }
    }

    public void PreloadObject(UnityObjectInfo objectInfo, SnuggleOptions? options = null, Stream? dataStream = null) {
        if ((options ?? Options).LoadOnDemand) {
            Objects[objectInfo.PathId] = new SerializedObject(objectInfo, this) { NeedsLoad = true };
        } else {
            Objects[objectInfo.PathId] = ObjectFactory.GetInstance(OpenFile(objectInfo, dataStream, dataStream != null), objectInfo, this);
        }
    }
}