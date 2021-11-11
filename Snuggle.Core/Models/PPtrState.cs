using JetBrains.Annotations;

namespace Snuggle.Core.Models {
    [PublicAPI]
    public enum PPtrState {
        Unloaded,
        Loaded,
        Failed,
    }
}
