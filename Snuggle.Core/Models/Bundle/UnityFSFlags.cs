using System;

namespace Snuggle.Core.Models.Bundle;

[Flags]
public enum UnityFSFlags : uint {
    CompressionRange = 0x3F,
    CombinedData = 0x40,
    BlocksInfoAtEnd = 0x80,
    OldBrowserFallback = 0x100,
    BlockInfoNeedPaddingAtStart = 0x200,
}
