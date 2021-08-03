using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Equilibrium.Implementations;
using Equilibrium.Interfaces;
using Equilibrium.IO;
using Equilibrium.Meta;
using Equilibrium.Models.Serialization;
using Equilibrium.Options;
using JetBrains.Annotations;

namespace Equilibrium {
    [PublicAPI]
    public class SerializedFile : IRenewable {
        public SerializedFile(Stream dataStream, object tag, IFileHandler handler, EquilibriumOptions options, bool leaveOpen = false) {
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
                if (header.FileVersion < UnitySerializedFileVersion.RefObject) {
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

        public EquilibriumOptions Options { get; init; }
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

        public Dictionary<long, SerializedObject> Objects { get; init; }

        public object Tag { get; set; }
        public IFileHandler Handler { get; set; }

        public Stream OpenFile(long pathId) => OpenFile(ObjectInfos[pathId]);

        public Stream OpenFile(UnityObjectInfo info) => OpenFile(info, Handler.OpenFile(Tag));

        public Stream OpenFile(UnityObjectInfo info, Stream stream, bool leaveOpen = false) => new OffsetStream(stream, info.Offset + Header.Offset, info.Size, leaveOpen);

        public bool ToStream(FileSerializationOptions serializationOptions, [MaybeNullWhen(false)] out Stream? bundleStream, [MaybeNullWhen(false)] out Stream? resourceStream) {
            bundleStream = null;
            resourceStream = null;

            var (alignment, resourceDataThreshold, targetVersion, targetGame, targetFileVersion, isBundle, resourceSuffix) = serializationOptions;

            if (targetVersion < UnityVersionRegister.Unity5) {
                targetVersion = Header.Version ?? UnityVersionRegister.Unity5;
            }

            if (targetFileVersion < UnitySerializedFileVersion.InitialVersion) {
                targetFileVersion = Header.FileVersion;
            }

            var prefix = isBundle ? $"archive:/{Name}/" : "";

            var options = new AssetSerializationOptions(
                alignment,
                resourceDataThreshold,
                targetVersion,
                targetGame,
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

        public void FindAssetContainerNames(SerializedObject? resourceManager) {
            if (Objects.Values.FirstOrDefault(x => x is AssetBundle) is AssetBundle assetBundle &&
                assetBundle.Container.Count > 0) {
                foreach (var (path, (_, _, pPtr)) in assetBundle.Container) {
                    if (pPtr.Value != null) {
                        pPtr.Value.ObjectContainerPath = path;
                    }
                }
            }

            // TODO: resource manager
        }
    }
}
