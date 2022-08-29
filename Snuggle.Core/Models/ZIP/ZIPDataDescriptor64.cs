using System.Runtime.InteropServices;

namespace Snuggle.Core.Models.ZIP;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct ZIPDataDescriptor64 {
    public uint CRC32 { get; init; }
    public ulong CompressedSize { get; init; }
    public ulong UncompressedSize { get; init; }
}
