using System;
using System.IO;
using Snuggle.Core.Interfaces;
using Snuggle.Core.Meta;
using Snuggle.Core.Options;

namespace Snuggle.Core.IO;

public class OffsetStreamHandler : IFileHandler {
    public OffsetStreamHandler(IFileHandler handler) => UnderlyingHandler = handler;

    public static Lazy<OffsetStreamHandler> FileInstance { get; } = new(() => new OffsetStreamHandler(new FileStreamHandler()));

    public static Lazy<OffsetStreamHandler> SplitInstance { get; } = new(() => new OffsetStreamHandler(new SplitFileStreamHandler()));

    public IFileHandler UnderlyingHandler { get; }

    public void Dispose() {
        GC.SuppressFinalize(this);
    }

    public Stream OpenFile(object tag) {
        var (subTag, offset, length) = tag switch {
            OffsetInfo meta => meta,
            string str when str.EndsWith(".split0") => new OffsetInfo(str, 0, SplitFileStream.GetLength(str)),
            string str => new OffsetInfo(str, 0, new FileInfo(str).Length),
            _ => throw new NotSupportedException($"{tag.GetType().FullName} is not supported"),
        };

        var stream = UnderlyingHandler.OpenFile(subTag);

        return new OffsetStream(stream, offset, length);
    }

    public Stream OpenSubFile(object parent, object tag, SnuggleCoreOptions options) => UnderlyingHandler.OpenSubFile(parent is OffsetInfo mmi ? mmi.Tag : tag, tag, options);

    public bool FileCreated(object parent, object tag, SnuggleCoreOptions options) => UnderlyingHandler.FileCreated(parent is OffsetInfo mmi ? mmi.Tag : tag, tag, options);
    public bool SupportsCreation => UnderlyingHandler.SupportsCreation;

    public object GetTag(object baseTag, object parent) {
        if (baseTag is not OffsetInfo meta) {
            return baseTag;
        }

        var length = 0L;
        if (parent is IAssetBundle bundle) {
            length = bundle.Length;
        }

        if (length == 0) {
            return baseTag;
        }

        return meta with { Length = length };
    }
}
