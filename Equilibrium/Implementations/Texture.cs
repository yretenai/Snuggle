using System;
using Equilibrium.IO;
using Equilibrium.Meta;
using Equilibrium.Models;
using Equilibrium.Models.Objects.Graphics;
using Equilibrium.Models.Serialization;
using Equilibrium.Options;
using JetBrains.Annotations;

namespace Equilibrium.Implementations {
    [PublicAPI, ObjectImplementation(UnityClassId.Texture)]
    public class Texture : NamedObject {
        public Texture(BiEndianBinaryReader reader, UnityObjectInfo info, SerializedFile serializedFile) : base(reader, info, serializedFile) {
            if (serializedFile.Version >= UnityVersionRegister.Unity2017_3) {
                FallbackFormat = (TextureFormat) reader.ReadInt32();
                DownscaleFallback = reader.ReadBoolean();
            }

            if (serializedFile.Version >= UnityVersionRegister.Unity2020_2) {
                AlphaOptional = reader.ReadBoolean();
            }

            reader.Align();
        }

        public Texture(UnityObjectInfo info, SerializedFile serializedFile) : base(info, serializedFile) { }

        public TextureFormat FallbackFormat { get; set; }
        public bool DownscaleFallback { get; set; }
        public bool AlphaOptional { get; set; }
        
        public override void Serialize(BiEndianBinaryWriter writer, AssetSerializationOptions options) {
            base.Serialize(writer, options);
            if (options.TargetVersion >= UnityVersionRegister.Unity2017_3) {
                writer.Write((int) FallbackFormat);
                writer.Write(DownscaleFallback);

                if (options.TargetVersion >= UnityVersionRegister.Unity2020_2) {
                    writer.Write(AlphaOptional);
                }

                writer.Align();
            }
        }

        public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), FallbackFormat);
    }
}
