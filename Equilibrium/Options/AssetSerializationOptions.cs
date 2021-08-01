using Equilibrium.Meta;
using Equilibrium.Models.Serialization;
using JetBrains.Annotations;

namespace Equilibrium.Options {
    [PublicAPI]
    public record AssetSerializationOptions(
        int Alignment,
        long ResourceDataThreshold,
        UnityVersion TargetVersion,
        UnityGame TargetGame,
        UnitySerializedFileVersion TargetFileVersion,
        string FileName,
        string ResourceFileName);
}
