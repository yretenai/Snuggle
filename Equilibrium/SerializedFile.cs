using System;
using System.Collections.Immutable;
using System.IO;
using Equilibrium.IO;
using Equilibrium.Models.IO;
using Equilibrium.Models.Serialization;
using JetBrains.Annotations;

namespace Equilibrium {
    [PublicAPI]
    public class SerializedFile : IRenewable {
        public SerializedFile(Stream dataStream, string tag, IFileHandler handler, bool leaveOpen = false) {
            Tag = tag;
            Handler = handler;

            using var reader = new BiEndianBinaryReader(dataStream, true, leaveOpen);
            var header = UnitySerializedFile.FromReader(reader);
            Types = UnitySerializedType.ArrayFromReader(reader, header).ToImmutableArray();
            ObjectInfos = UnityObjectInfo.ArrayFromReader(reader, ref header, Types).ToImmutableArray();
            ScriptInfos = UnityScriptInfo.ArrayFromReader(reader, header).ToImmutableArray();
            ExternalInfos = UnityExternalInfo.ArrayFromReader(reader, header).ToImmutableArray();
            ReferenceTypes = UnitySerializedType.ArrayFromReader(reader, header, true).ToImmutableArray();
            if (header.Version >= UnitySerializedFileVersion.UserInformation) {
                UserInformation = reader.ReadNullString();
            }

            Header = header;
        }

        public UnitySerializedFile Header { get; set; }
        public ImmutableArray<UnitySerializedType> Types { get; set; }
        public ImmutableArray<UnityObjectInfo> ObjectInfos { get; set; }
        public ImmutableArray<UnityScriptInfo> ScriptInfos { get; set; }
        public ImmutableArray<UnityExternalInfo> ExternalInfos { get; set; }
        public ImmutableArray<UnitySerializedType> ReferenceTypes { get; set; }
        public string UserInformation { get; set; } = string.Empty;

        public object Tag { get; set; }
        public IFileHandler Handler { get; set; }

        public byte[] OpenFile(long pathId) {
            using var reader = Handler.OpenFile(Tag);
            // TODO
            return Array.Empty<byte>();
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
            } finally {
                reader.IsBigEndian = isBigEndian;
                reader.BaseStream.Seek(pos, SeekOrigin.Begin);
            }
        }
    }
}
