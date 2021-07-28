using Equilibrium.Implementations;
using Equilibrium.IO;
using Equilibrium.Meta;
using JetBrains.Annotations;

namespace Equilibrium.Models.Objects {
    [PublicAPI]
    public record ComponentPtr(
        ClassId ClassId,
        PPtr<Component> Ptr) {
        public static ComponentPtr FromReader(BiEndianBinaryReader reader, SerializedFile file) {
            var classId = ClassId.Unknown;
            if (file.Version < new UnityVersion(5, 5)) {
                classId = (ClassId) reader.ReadInt32();
            }

            var ptr = PPtr<Component>.FromReader(reader, file);
            return new ComponentPtr(classId, ptr);
        }

        public void ToWriter(BiEndianBinaryWriter writer, SerializedFile serializedFile, UnityVersion? targetVersion) {
            if (targetVersion < new UnityVersion(5, 5)) {
                writer.Write((int) ClassId);
            }

            Ptr.ToWriter(writer, serializedFile, targetVersion);
        }

        public PPtr<T> ToPtr<T>() where T : Component => new(Ptr.FileId, Ptr.PathId) { File = Ptr.File };
    }
}
