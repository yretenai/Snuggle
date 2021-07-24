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

        public Span<byte> OpenFile(long pathId) {
            using var reader = Handler.OpenFile(Tag);
            // TODO
            return Span<byte>.Empty;
        }
    }
}
