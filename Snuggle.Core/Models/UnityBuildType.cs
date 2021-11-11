using JetBrains.Annotations;

namespace Snuggle.Core.Models {
    [PublicAPI]
    public enum UnityBuildType {
        None = 0,
        Release = 'r',
        Alpha = 'a',
        Beta = 'b',
        Final = 'f',
        Patch = 'p',
    }
}
