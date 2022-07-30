using System.Text.Json.Serialization;
using Snuggle.Core.Implementations;
using Snuggle.Core.IO;
using Snuggle.Core.Meta;

namespace Snuggle.Core.Models.Objects;

public record ComponentPair(object ClassId, PPtr<Component> Ptr) {
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Tag { get; set; }

    public static ComponentPair FromReader(BiEndianBinaryReader reader, SerializedFile file) {
        object classId = UnityClassId.Unknown;
        if (file.Version < UnityVersionRegister.Unity5_5) {
            classId = ObjectFactory.GetClassIdForGame(file.Options.Game, reader.ReadInt32());
        }

        var ptr = PPtr<Component>.FromReader(reader, file);
        return new ComponentPair(classId, ptr);
    }

    public void ToWriter(BiEndianBinaryWriter writer, SerializedFile serializedFile, UnityVersion targetVersion) {
        if (targetVersion < UnityVersionRegister.Unity5_5) {
            writer.Write((int) ClassId);
        }

        Ptr.ToWriter(writer, serializedFile, targetVersion);
    }

    public PPtr<T> ToPtr<T>() where T : Component => new(Ptr.FileId, Ptr.PathId) { File = Ptr.File };
}
