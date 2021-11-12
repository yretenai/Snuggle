using JetBrains.Annotations;

namespace Snuggle.Core.Models.Objects.Graphics; 

[PublicAPI]
public enum FilterMode {
    Point = 0,
    Bilinear = 1,
    Trilinear = 2,
}