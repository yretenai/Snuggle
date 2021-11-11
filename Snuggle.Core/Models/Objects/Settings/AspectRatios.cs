using JetBrains.Annotations;
using Snuggle.Core.IO;

namespace Snuggle.Core.Models.Objects.Settings {
    [PublicAPI]
    public record AspectRatios(
        bool FourThirds,
        bool FiveFourths,
        bool LCD,
        bool HD,
        bool Others) {
        public static AspectRatios Default { get; } = new(true, true, true, true, true);

        public static AspectRatios FromReader(BiEndianBinaryReader reader, SerializedFile file) {
            var x43 = reader.ReadBoolean();
            var x54 = reader.ReadBoolean();
            var x1610 = reader.ReadBoolean();
            var x169 = reader.ReadBoolean();
            var others = reader.ReadBoolean();
            reader.Align();

            return new AspectRatios(x43, x54, x1610, x169, others);
        }
    }
}
