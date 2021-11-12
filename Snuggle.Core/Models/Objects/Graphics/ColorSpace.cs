using JetBrains.Annotations;

namespace Snuggle.Core.Models.Objects.Graphics; 

[PublicAPI]
public enum ColorSpace {
    Unknown = -1,
    Gamma = 0,
    Linear = 1,
}