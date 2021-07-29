using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Equilibrium.Implementations;
using Equilibrium.IO;
using Equilibrium.Meta;
using Equilibrium.Meta.Interfaces;
using Equilibrium.Meta.Options;
using Equilibrium.Models.Serialization;
using JetBrains.Annotations;

namespace Equilibrium {
    [PublicAPI]
    public class SerializedFile : IRenewable {
        public SerializedFile(Stream dataStream, object tag, IFileHandler handler, EquilibriumOptions? options = null, bool leaveOpen = false) {
            Tag = tag;
            Handler = handler;
            Options = options ?? EquilibriumOptions.Default;

            using var reader = new BiEndianBinaryReader(dataStream, true, leaveOpen);
            var header = UnitySerializedFile.FromReader(reader, Options);
            Types = UnitySerializedType.ArrayFromReader(reader, header, Options);
            ObjectInfos = UnityObjectInfo.ArrayFromReader(reader, ref header, Types, Options);
            ScriptInfos = UnityScriptInfo.ArrayFromReader(reader, header, Options);
            ExternalInfos = UnityExternalInfo.ArrayFromReader(reader, header, Options);
            if (header.Version < UnitySerializedFileVersion.RefObject) {
                ReferenceTypes = UnitySerializedType.ArrayFromReader(reader, header, Options, true);
            }

            if (header.Version >= UnitySerializedFileVersion.UserInformation) {
                UserInformation = reader.ReadNullString();
            }

            Header = header;

            Version = UnityVersion.Parse(header.UnityVersion);

            Objects = new Dictionary<long, SerializedObject>(ObjectInfos.Length);
        }

        public EquilibriumOptions Options { get; init; }
        public UnitySerializedFile Header { get; init; }
        public UnitySerializedType[] Types { get; init; }
        public UnityObjectInfo[] ObjectInfos { get; init; }
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

        public Stream OpenFile(long pathId) => OpenFile(ObjectInfos.First(x => x.PathId == pathId));

        public Stream OpenFile(UnityObjectInfo info) => OpenFile(info, Handler.OpenFile(Tag));

        public Stream OpenFile(UnityObjectInfo info, Stream stream, bool leaveOpen = false) => new OffsetStream(stream, info.Offset + Header.Offset, info.Size, leaveOpen);

        public Stream ToStream(FileSerializationOptions options) => throw new NotImplementedException();

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
    }
}
