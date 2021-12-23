using System;
using JetBrains.Annotations;
using Snuggle.Core.IO;
using Snuggle.Core.Meta;
using Snuggle.Core.Options;

namespace Snuggle.Core.Models.Bundle;

[PublicAPI]
public record UnityBundle(string Signature, UnityFormat Format, int FormatVersion, string EngineVersion, string EngineRevision) {
    public UnityVersion? Version { get; } = UnityVersion.ParseSafe(EngineVersion);
    public UnityVersion? Revision { get; } = UnityVersion.ParseSafe(EngineRevision);

    public void ToWriter(BiEndianBinaryWriter writer, SnuggleCoreOptions options) {
        writer.IsBigEndian = true;
        writer.WriteNullString(Signature);
        writer.Write(FormatVersion);
        writer.WriteNullString(EngineVersion);
        writer.WriteNullString(EngineRevision);
    }

    public static UnityBundle FromReader(BiEndianBinaryReader reader, SnuggleCoreOptions options) {
        var originalSignature = reader.ReadNullString();
        var format = UnityFormat.FS;
        if (!originalSignature.StartsWith("Unity")) {
            if (originalSignature.Contains("Unity")) {
                format = (UnityFormat) originalSignature[originalSignature.IndexOf("Unity", StringComparison.Ordinal) + 5];
            } else if (Core.Bundle.NonStandardLookup.TryGetValue(originalSignature, out var nonStandard)) {
                format = nonStandard.Format;
            }
        } else {
            format = (UnityFormat) originalSignature[5];
        }
        
        return new UnityBundle(originalSignature, format, reader.ReadInt32(), reader.ReadNullString(), reader.ReadNullString());
    }
}
