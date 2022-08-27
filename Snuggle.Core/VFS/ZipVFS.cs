using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Snuggle.Core.Interfaces;

namespace Snuggle.Core.VFS;

public sealed record ZipVFS : IVirtualStorage, IDisposable {
    public ZipVFS(Stream data, object tag, IFileHandler handler, bool leaveOpen = false) {
        Tag = tag;
        Handler = handler;
        Archive = new ZipArchive(data, ZipArchiveMode.Read, leaveOpen);
        Entries = Archive.Entries.Where(x => !x.IsEncrypted).Select(x => (IVirtualStorageEntry) new VirtualStorageEntry(x.FullName, x.Length)).ToList();
    }

    public ZipArchive Archive { get; init; }

    public void Dispose() {
        Archive.Dispose();
    }

    public List<IVirtualStorageEntry> Entries { get; set; }

    public Stream Open(string path, bool leaveOpen = false) => Open(new VirtualStorageEntry(path), null, leaveOpen);

    public Stream Open(IVirtualStorageEntry entry, Stream? data = null, bool leaveOpen = false) {
        var zipEntry = Archive.Entries.First(x => x.FullName == entry.Path);
        var tmp = new MemoryStream((int) zipEntry.Length);
        zipEntry.Open().CopyTo(tmp);
        tmp.Position = 0;
        return tmp;
    }

    public object Tag { get; set; }
    public IFileHandler Handler { get; set; }

    public static bool IsZipVFS(Stream data) {
        var pos = data.Position;
        try {
            Span<byte> span = stackalloc byte[4];
            data.ReadExactly(span);
            return BinaryPrimitives.ReadUInt32LittleEndian(span) == 0x04034b50;
        } catch {
            return false;
        } finally {
            data.Position = pos;
        }
    }
}
