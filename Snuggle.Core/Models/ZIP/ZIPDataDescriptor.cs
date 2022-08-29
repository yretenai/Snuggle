using System.Runtime.InteropServices;

namespace Snuggle.Core.Models.ZIP;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct ZIPDataDescriptor {
    public uint CRC32 { get; init; }
    public uint CompressedSize { get; init; }
    public uint UncompressedSize { get; init; }
}
