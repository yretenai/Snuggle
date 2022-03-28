using System;
using System.IO;
using System.Linq;
using Snuggle.Core.Interfaces;
using Snuggle.Core.Models.Bundle;
using Snuggle.Core.Options;

namespace Snuggle.Core.IO;

public class BundleStreamHandler : IFileHandler {
    public BundleStreamHandler(Bundle bundleFile) => BundleFile = bundleFile;

    public Bundle BundleFile { get; }

    public Stream OpenFile(object tag) {
        var path = tag switch {
            string str => BundleFile.Container.Blocks.First(x => x.Path.Equals(str, StringComparison.InvariantCultureIgnoreCase)),
            UnityBundleBlock block => block,
            _ => throw new NotSupportedException($"{tag.GetType().FullName} is not supported"),
        };

        return BundleFile.OpenFile(path);
    }

    public Stream OpenSubFile(object parent, object tag, SnuggleCoreOptions options) => throw new NotSupportedException();

    public bool FileCreated(object parent, object tag, SnuggleCoreOptions options) => throw new NotSupportedException();

    public object GetTag(object baseTag, object parent) => baseTag;

    public void Dispose() {
        BundleFile.Dispose();
        GC.SuppressFinalize(this);
    }
}
