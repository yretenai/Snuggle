using Equilibrium.IO;
using Equilibrium.Meta;
using Equilibrium.Meta.Options;
using Equilibrium.Models;
using Equilibrium.Models.Objects.Graphics;
using Equilibrium.Models.Serialization;
using JetBrains.Annotations;

namespace Equilibrium.Implementations {
    [PublicAPI, ObjectImplementation(UnityClassId.Texture)]
    public class Texture : NamedObject {
        public Texture(BiEndianBinaryReader reader, UnityObjectInfo info, SerializedFile serializedFile) : base(reader, info, serializedFile) {
            if (serializedFile.Version >= new UnityVersion(2017, 3)) {
                FallbackFormat = (TextureFormat) reader.ReadInt32();
                DownscaleFallback = reader.ReadBoolean();

                if (serializedFile.Version >= new UnityVersion(2020, 2)) {
                    AlphaOptional = reader.ReadBoolean();
                }

                reader.Align();
            }
        }

        public Texture(UnityObjectInfo info, SerializedFile serializedFile) : base(info, serializedFile) { }

        public TextureFormat FallbackFormat { get; set; }
        public bool DownscaleFallback { get; set; }
        public bool AlphaOptional { get; set; }

        public override void Serialize(BiEndianBinaryWriter writer, UnityVersion? targetVersion, FileSerializationOptions options) {
            base.Serialize(writer, targetVersion, options);
            if (targetVersion >= new UnityVersion(2017, 3)) {
                writer.Write((int) FallbackFormat);
                writer.Write(DownscaleFallback);

                if (targetVersion >= new UnityVersion(2020, 2)) {
                    writer.Write(AlphaOptional);
                }

                writer.Align();
            }
        }
    }
}
