using System.Runtime.InteropServices;

namespace Snuggle.Core.Models.ZIP;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct ZIP64EndOfCentralDirectory {
    public uint Magic { get; init; }
    public ulong EOCDSize { get; init; }
    public ushort VersionBy { get; init; }
    public ushort VersionExtract { get; init; }
    public uint DiskNumber { get; init; }
    public uint DirectoryDiskNumber { get; init; }
    public long NumberOfDirectoryRecords { get; init; }
    public long TotalNumberOfDirectoryRecords { get; init; }
    public long DirectorySize { get; init; }
    public long DirectoryOffset { get; init; }
}
