using System;
using System.IO;
using Snuggle.Core.Interfaces;
using Snuggle.Core.Options;

namespace Snuggle.Core.IO;

public sealed class MemoryStreamHandler : IFileHandler {
    public MemoryStreamHandler(Stream stream) => BaseStream = stream;
    public Stream BaseStream { get; set; }

    public void Dispose() => BaseStream.Dispose();

    public Stream OpenFile(object tag) {
        BaseStream.Position = 0;
        return BaseStream;
    }

    public Stream OpenSubFile(object parent, object tag, SnuggleCoreOptions options) => throw new NotSupportedException();

    public bool FileCreated(object parent, object tag, SnuggleCoreOptions options) => throw new NotSupportedException();

    public object GetTag(object baseTag, object parent) => baseTag;
}
