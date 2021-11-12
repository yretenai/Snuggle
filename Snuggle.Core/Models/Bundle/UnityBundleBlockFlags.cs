using System;
using JetBrains.Annotations;

namespace Snuggle.Core.Models.Bundle;

[PublicAPI]
[Flags]
public enum UnityBundleBlockFlags {
    SerializedFile = 4,
}
