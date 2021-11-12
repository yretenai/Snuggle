using JetBrains.Annotations;

namespace Snuggle.Core.Models.Bundle;

[PublicAPI]
public enum UnityFormat : byte {
    Archive = (byte) 'A',
    FS = (byte) 'F',
    Web = (byte) 'W',
    Raw = (byte) 'R',
}
