using System.IO;
using JetBrains.Annotations;
using Snuggle.Core.Interfaces;

namespace Snuggle.Core.IO;

[PublicAPI]
public sealed class MemoryStreamHandler : IFileHandler {
    public MemoryStreamHandler(Stream stream) => BaseStream = stream;
    public Stream BaseStream { get; set; }

    public void Dispose() => BaseStream.Dispose();

    public Stream OpenFile(object tag) {
        BaseStream.Position = 0;
        return BaseStream;
    }

    public object GetTag(object baseTag, object parent) => baseTag;
}
