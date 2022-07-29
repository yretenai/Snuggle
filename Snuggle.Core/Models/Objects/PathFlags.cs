using System;

namespace Snuggle.Core.Models.Objects;

[Flags]
public enum PathFlags {
    None = 0,
    GenerateNames = 1,
    GenerateFiles = 2,
    CaseInsensitive = 4,
}
