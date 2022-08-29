using System.Runtime.InteropServices;

namespace Snuggle.Core.Models.ZIP;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct ZIP64EndOfCentralDirectoryLocator {
    public uint Magic { get; init; }
    public uint DiskNumber { get; init; }
    public long EOCDOffset { get; init; }
    public uint TotalNumberOfDisks { get; init; }
}
