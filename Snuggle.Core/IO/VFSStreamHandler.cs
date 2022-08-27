using System;
using System.IO;
using Snuggle.Core.Interfaces;
using Snuggle.Core.Meta;
using Snuggle.Core.Options;

namespace Snuggle.Core.IO;

public class VFSStreamHandler : IFileHandler {
    public VFSStreamHandler(IVirtualStorage vfs) => VFS = vfs;
    public IVirtualStorage VFS { get; }

    public Stream OpenFile(object tag) {
        while (true) {
            switch (tag) {
                case string str:
                    return VFS.Open(str);
                case IVirtualStorageEntry entry:
                    return VFS.Open(entry);
                case OffsetInfo offsetInfo:
                    tag = offsetInfo.Tag;
                    break;
                default:
                    throw new NotSupportedException($"{tag.GetType().FullName} is not supported");
            }
        }
    }

    public Stream OpenSubFile(object parent, object tag, SnuggleCoreOptions options) => throw new NotSupportedException();

    public bool FileCreated(object parent, object tag, SnuggleCoreOptions options) => throw new NotSupportedException();
    public bool SupportsCreation => false;

    public object GetTag(object baseTag, object parent) => baseTag;

    public void Dispose() {
        (VFS as IDisposable)?.Dispose();
        GC.SuppressFinalize(this);
    }
}
