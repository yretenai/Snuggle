using System;
using System.Collections.Generic;
using System.IO;
using Snuggle.Core.Options;
using Snuggle.Core.VFS;

namespace Snuggle.Core.Interfaces;

public interface IVirtualStorage : IRenewable {
    public List<IVirtualStorageEntry> Entries { get; set; }

    public Stream Open(string path, bool leaveOpen = false);
    public Stream Open(IVirtualStorageEntry entry, Stream? data = null, bool leaveOpen = false);

    public static bool IsVFSFile(object tag, Stream stream, SnuggleCoreOptions options) => ZipVFS.IsZipVFS(stream) || FATVFS.IsFAT32VFS(stream);

    public static IVirtualStorage Init(Stream stream, object tag, IFileHandler handler, SnuggleCoreOptions options, bool leaveOpen = false) {
        if (ZipVFS.IsZipVFS(stream)) {
            return new ZipVFS(stream, tag, handler, leaveOpen);
        }

        if (FATVFS.IsFAT32VFS(stream)) {
            return new FATVFS(stream, tag, handler);
        }

        throw new NotSupportedException();
    }
}
