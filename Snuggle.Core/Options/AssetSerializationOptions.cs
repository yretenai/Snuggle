using JetBrains.Annotations;
using Snuggle.Core.Meta;
using Snuggle.Core.Models.Serialization;

namespace Snuggle.Core.Options {
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
