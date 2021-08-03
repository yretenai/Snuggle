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
}
