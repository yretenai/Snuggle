using Equilibrium.Implementations;
using Equilibrium.IO;
using Equilibrium.Meta;
using JetBrains.Annotations;

namespace Equilibrium.Models.Objects {
    [PublicAPI]
    public record ComponentPair(
        object ClassId,
        PPtr<Component> Ptr) {
        public static ComponentPair FromReader(BiEndianBinaryReader reader, SerializedFile file) {
            object classId = default(UnityClassId);
            if (file.Version < new UnityVersion(5, 5)) {
                classId = ObjectFactory.GetClassIdForGame(file.Options.Game, reader.ReadInt32());
            }

            var ptr = PPtr<Component>.FromReader(reader, file);
            return new ComponentPair(classId, ptr);
        }

        public void ToWriter(BiEndianBinaryWriter writer, SerializedFile serializedFile, UnityVersion targetVersion) {
            if (targetVersion < new UnityVersion(5, 5)) {
                writer.Write((int) ClassId);
            }

            Ptr.ToWriter(writer, serializedFile, targetVersion);
        }

        public PPtr<T> ToPtr<T>() where T : Component => new(Ptr.FileId, Ptr.PathId) { File = Ptr.File };
    }

    [PublicAPI]
    public record StreamingInfo(
        long Offset,
        long Size,
        string Path) {
        public static StreamingInfo Default { get; } = new(0, 0, string.Empty);

        public static StreamingInfo FromReader(BiEndianBinaryReader reader, SerializedFile file) {
            var offset = file.Version >= new UnityVersion(2020, 1) ? reader.ReadInt64() : reader.ReadUInt32();
            var size = reader.ReadUInt32();
            var path = reader.ReadString32();
            return new StreamingInfo(offset, size, path);
        }

        public void ToWriter(BiEndianBinaryWriter writer, SerializedFile serializedFile, UnityVersion targetVersion) {
            if (targetVersion >= new UnityVersion(2020, 1)) {
                writer.Write(Offset);
            } else {
                writer.Write((uint) Offset);
            }

            writer.Write((uint) Size);
            writer.WriteString32(Path);
        }
    }
}
