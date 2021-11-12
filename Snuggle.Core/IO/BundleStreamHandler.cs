using System;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using Snuggle.Core.Interfaces;
using Snuggle.Core.Models.Bundle;

namespace Snuggle.Core.IO;

[PublicAPI]
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

    public object GetTag(object baseTag, object parent) => baseTag;

    public void Dispose() {
        BundleFile.Dispose();
        GC.SuppressFinalize(this);
    }
}
