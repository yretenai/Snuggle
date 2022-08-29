using System.Runtime.InteropServices;

namespace Snuggle.Core.Models.ZIP;

public enum ZIPExtraHeaderId : ushort {
    ZIP64ExtraHeader = 0x0001,
}

public readonly record struct ZIPExtraHeader {
    public ZIPExtraHeaderId Id { get; init; }
    public ushort Length { get; init; }
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct ZIP64ExtendedInformation {
    public long UncompressedSize { get; init; }
    public long CompressedSize { get; init; }
    public long Offset { get; init; }
    public uint DiskNumber { get; init; }
}
