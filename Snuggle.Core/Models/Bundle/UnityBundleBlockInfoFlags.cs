using System;

namespace Snuggle.Core.Models.Bundle;

[Flags]
public enum UnityBundleBlockInfoFlags : ushort {
    CompressionMask = 0x3F,
}
