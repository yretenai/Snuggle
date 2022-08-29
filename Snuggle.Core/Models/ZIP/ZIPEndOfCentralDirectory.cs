using System.Collections.Generic;
using System.Runtime.InteropServices;
using Snuggle.Core.Interfaces;

namespace Snuggle.Core.Models.ZIP;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct ZIPEndOfCentralDirectory {
    public uint Magic { get; init; }
    public ushort DiskNumber { get; init; }
    public ushort DirectoryDiskNumber { get; init; }
    public ushort NumberOfDirectoryRecords { get; init; }
    public ushort TotalNumberOfDirectoryRecords { get; init; }
    public uint DirectorySize { get; init; }
    public uint DirectoryOffset { get; init; }
    public ushort CommentLength { get; init; }

    public bool IsZIP64 => DirectoryOffset == uint.MaxValue;
}

public record struct ZIPEntry(string Path, long Length, List<KeyValuePair<ZIPExtraHeader, object>> Extra, string Comment, ZIPCentralDirectoryHeader Header) : IVirtualStorageEntry;
