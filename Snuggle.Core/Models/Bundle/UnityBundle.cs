using JetBrains.Annotations;
using Snuggle.Core.IO;
using Snuggle.Core.Meta;
using Snuggle.Core.Options;

namespace Snuggle.Core.Models.Bundle;

[PublicAPI]
public record UnityBundle(string Signature, int FormatVersion, string EngineVersion, string EngineRevision) {
    public UnityFormat Format { get; } = (UnityFormat) Signature[5];

    public UnityVersion? Version { get; } = UnityVersion.ParseSafe(EngineVersion);
    public UnityVersion? Revision { get; } = UnityVersion.ParseSafe(EngineRevision);

    public void ToWriter(BiEndianBinaryWriter writer, SnuggleCoreOptions options) {
        writer.IsBigEndian = true;
        writer.WriteNullString(Signature);
        writer.Write(FormatVersion);
        writer.WriteNullString(EngineVersion);
        writer.WriteNullString(EngineRevision);
    }

    public static UnityBundle FromReader(BiEndianBinaryReader reader, SnuggleCoreOptions options) => new(reader.ReadNullString(), reader.ReadInt32(), reader.ReadNullString(), reader.ReadNullString());
}
