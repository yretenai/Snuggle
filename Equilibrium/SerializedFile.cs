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
            Header = UnitySerializedFile.FromReader(reader);
            Types = UnitySerializedType.ArrayFromReader(reader, Header).ToImmutableArray();
            ObjectInfos = UnityObjectInfo.ArrayFromReader(reader, Header).ToImmutableArray();
            ScriptInfos = UnityScriptInfo.ArrayFromReader(reader, Header).ToImmutableArray();
            ExternalInfos = UnityExternalInfo.ArrayFromReader(reader, Header).ToImmutableArray();
            ReferenceInfos = UnitySerializedType.ArrayFromReader(reader, Header, true).ToImmutableArray();
            if (Header.Version >= UnitySerializedFileVersion.UserInformation) {
                UserInformation = reader.ReadNullString();
            }
        }

        public UnitySerializedFile Header { get; set; }
        public ImmutableArray<UnitySerializedType> Types { get; set; }
        public ImmutableArray<UnityObjectInfo> ObjectInfos { get; set; }
        public ImmutableArray<UnityScriptInfo> ScriptInfos { get; set; }
        public ImmutableArray<UnityExternalInfo> ExternalInfos { get; set; }
        public ImmutableArray<UnitySerializedType> ReferenceInfos { get; set; }
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
