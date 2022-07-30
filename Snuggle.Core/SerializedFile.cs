using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Serilog;
using Snuggle.Core.Implementations;
using Snuggle.Core.Interfaces;
using Snuggle.Core.IO;
using Snuggle.Core.Meta;
using Snuggle.Core.Models.Serialization;
using Snuggle.Core.Options;

namespace Snuggle.Core;

public class SerializedFile : IRenewable {
#pragma warning disable CS8618
    [Obsolete("test-only")]
    internal SerializedFile(SnuggleCoreOptions options) {
#pragma warning restore CS8618
        Options = options;
    }

    public SerializedFile(Stream dataStream, object tag, IFileHandler handler, SnuggleCoreOptions options, bool leaveOpen = false) {
        try {
            Tag = tag;
            Handler = handler;
            Options = options;

            using var reader = new BiEndianBinaryReader(dataStream, true, leaveOpen);
            var header = UnitySerializedFile.FromReader(reader, Options);
            Types = UnitySerializedType.ArrayFromReader(reader, header, Options);
            ObjectInfos = UnityObjectInfo.ArrayFromReader(reader, ref header, Types, Options);
            PathIds = ObjectInfos.Select(x => x.PathId).ToList();
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
        } finally {
            if (!leaveOpen) {
                dataStream.Close();
            }
        }
    }

    public SnuggleCoreOptions Options { get; init; }
    public UnitySerializedFile Header { get; init; }
    public UnitySerializedType[] Types { get; set; }
    public List<long> PathIds { get; private set; }
    public List<UnityObjectInfo> ObjectInfos { get; private set; }
    public UnityScriptInfo[] ScriptInfos { get; init; }
    public UnityExternalInfo[] ExternalInfos { get; init; }
    public UnitySerializedType[] ReferenceTypes { get; set; } = Array.Empty<UnitySerializedType>();
    public string UserInformation { get; init; } = string.Empty;
    public UnityVersion Version { get; set; }
    public AssetCollection? Assets { get; set; }
    public string Name { get; init; } = string.Empty;
    public object Tag { get; set; }
    public IFileHandler Handler { get; set; }

    public Stream? OpenFile(long pathId) {
        var index = PathIds.IndexOf(pathId);
        return index == -1 ? null : OpenFile(ObjectInfos[index]);
    }

    public Stream OpenFile(UnityObjectInfo info) => OpenFile(info, Handler.OpenFile(Tag));

    public Stream OpenFile(UnityObjectInfo info, Stream? stream, bool leaveOpen = false) => new OffsetStream(stream ?? Handler.OpenFile(Tag), info.Offset + Header.Offset, info.Size, leaveOpen) { Position = 0 };

    public bool ToStream(FileSerializationOptions serializationOptions, [MaybeNullWhen(false)] out Stream bundleStream, [MaybeNullWhen(false)] out Stream resourceStream) {
        bundleStream = null;
        resourceStream = null;

        if (serializationOptions.TargetVersion < UnityVersionRegister.Unity5 || serializationOptions.TargetFileVersion < UnitySerializedFileVersion.InitialVersion) {
            serializationOptions = serializationOptions.MutateWithSerializedFile(this);
        }

        var (alignment, resourceDataThreshold, resourceSuffix, bundleTemplate) = serializationOptions;
        var targetVersion = serializationOptions.TargetVersion;
        var targetFileVersion = serializationOptions.TargetFileVersion;
        var isBundle = serializationOptions.IsBundle;
        var prefix = isBundle ? string.Format(bundleTemplate, Name) : string.Empty;

        var options = new AssetSerializationOptions(
            alignment,
            resourceDataThreshold,
            targetVersion,
            serializationOptions.TargetGame,
            targetFileVersion,
            prefix + Name,
            prefix + Name + resourceSuffix);
        
        bundleStream = new MemoryStream();
        resourceStream = new MemoryStream();
        using var bundleWriter = new BiEndianBinaryWriter(bundleStream, true, true);
        
        Header.ToWriter(bundleWriter, Options, options);
        UnitySerializedType.ArrayToWriter(bundleWriter, Types, Header, Options, options);
        var objectInfoOffset = bundleStream.Position;
        UnityObjectInfo.ArrayToWriter(bundleWriter, ObjectInfos, Header, Options, options);
        UnityScriptInfo.ArrayToWriter(bundleWriter, ScriptInfos, Header, Options, options);
        UnityExternalInfo.ArrayToWriter(bundleWriter, ExternalInfos, Header, Options, options);
        if (serializationOptions.TargetFileVersion >= UnitySerializedFileVersion.RefObject)  {
            UnitySerializedType.ArrayToWriter(bundleWriter, ReferenceTypes, Header, Options, options);
        }
        if (serializationOptions.TargetFileVersion >= UnitySerializedFileVersion.UserInformation) {
            bundleWriter.Write(UserInformation);
        }

        var headerSize = bundleStream.Position - 20;
        bundleWriter.Align(16); // TODO: 0x1000 alignment?
        var offset = bundleStream.Position;
        for (var index = 0; index < ObjectInfos.Count; index++) {
            var objectInfo = ObjectInfos[index];
            var newOffset = bundleStream.Position - offset;
            bundleWriter.Align(8);
            var obj = objectInfo.Instance;
            if (obj is { IsMutated: true }) {
                obj.Serialize(bundleWriter, options);
            } else {
                var stream = OpenFile(objectInfo);
                stream.CopyTo(bundleStream);
            }

            ObjectInfos[index] = objectInfo with { Offset = newOffset };
        }

        var newHeader = Header with { Size = bundleStream.Position, HeaderSize = (int) headerSize, Offset = offset };

        // Rewrite the object infos with updated offsets
        bundleStream.Position = 0;
        newHeader.ToWriter(bundleWriter, Options, options);
        bundleStream.Position = objectInfoOffset;
        UnityObjectInfo.ArrayToWriter(bundleWriter, ObjectInfos, Header, Options, options);

        // TODO: write resource stream?
        
        bundleStream.Position = 0;
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

    public void FindResources() {
        foreach (var info in ObjectInfos) {
            switch (info.Instance) {
                case ICABPathProvider cab: {
                    foreach (var (pPtr, path) in cab.GetCABPaths()) {
                        if (pPtr.Value != null) {
                            pPtr.Value.ObjectContainerPath = path;
                        }
                    }

                    break;
                }
                case PlayerSettings settings when Assets != null:
                    Assets.PlayerSettings = settings;
                    break;
            }
        }
    }

    public SerializedObject? GetObject(long pathId, SnuggleCoreOptions? options = null, Stream? dataStream = null, bool baseType = false) {
        var index = PathIds.IndexOf(pathId);
        return index == -1 ? null : GetObjectInner(index, options, dataStream, baseType);
    }

    private SerializedObject? GetObjectInner(int index, SnuggleCoreOptions? options = null, Stream? dataStream = null, bool baseType = false) {
        var objectInfo = ObjectInfos[index];

        options ??= Options;
        try
        {
            var ignored = options.IgnoreClassIds.Contains(objectInfo.ClassId.ToString()!);
            var shouldLoad = !options.LoadOnDemand;
            if (objectInfo.Instance != null)
            {
                if (!objectInfo.Instance.NeedsLoad || ignored)
                {
                    return objectInfo.Instance;
                }

                shouldLoad = true;
            }

            SerializedObject serializedObject;
            if (shouldLoad && !baseType && !ignored)
            {
                serializedObject =
                    ObjectFactory.GetInstance(OpenFile(objectInfo, dataStream, dataStream != null), objectInfo, this);
            } else
            {
                serializedObject = new SerializedObject(objectInfo, this) { NeedsLoad = true, IsMutated = false };
            }

            objectInfo.Instance = serializedObject;
            return serializedObject;
        } catch (Exception e)
        {
            Log.Error(e, "Failed to decode {PathId} (type {ClassId}) on file {Name}", objectInfo.PathId, objectInfo.ClassId,
                Name);
            return null;
        }
    }

    public IEnumerable<SerializedObject?> GetAllObjects() {
        for (var i = 0; i < ObjectInfos.Count; ++i) {
            yield return GetObjectInner(i);
        }
    }

    public void Free() {
        foreach (var serializedObject in ObjectInfos) {
            serializedObject.Instance?.Free();
        }
    }

    public void PreloadObject(UnityObjectInfo objectInfo, SnuggleCoreOptions? options = null, Stream? dataStream = null) {
        var index = PathIds.IndexOf(objectInfo.PathId);
        if (index == -1) {
            return;
        }

        var ignored = (options ?? Options).IgnoreClassIds.Contains(objectInfo.ClassId.ToString()!);
        var info = ObjectInfos[index];
        if ((options ?? Options).LoadOnDemand || ignored) {
            info.Instance = new SerializedObject(objectInfo, this) { NeedsLoad = true, IsMutated = false };
        } else {
            info.Instance = ObjectFactory.GetInstance(OpenFile(objectInfo, dataStream, dataStream != null), objectInfo, this);
        }
    }

    public IAssetBundle? GetBundle() {
        var handler = Handler;
        while (true) {
            switch (handler) {
                case BundleStreamHandler bsh:
                    return bsh.BundleFile;
                case MultiStreamHandler msh:
                    handler = msh.UnderlyingHandler;
                    continue;
            }

            return null;
        }
    }

    public void AddObject(long pathId, UnityObjectInfo objectInfo) {
        var index = PathIds.IndexOf(pathId);
        if (index == -1) {
            PathIds.Add(pathId);
            ObjectInfos.Add(objectInfo);
        } else {
            ObjectInfos[index] = objectInfo;
        }
    }
}
