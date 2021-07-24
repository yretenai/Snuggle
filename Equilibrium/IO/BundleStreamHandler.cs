﻿using System;
using System.IO;
using System.Linq;
using Equilibrium.Models.Bundle;
using Equilibrium.Models.IO;
using JetBrains.Annotations;

namespace Equilibrium.IO {
    [PublicAPI]
    public class BundleStreamHandler : IFileHandler {
        public BundleStreamHandler(Bundle bundleFile) => BundleFile = bundleFile;

        public Bundle BundleFile { get; }

        public Stream OpenFile(object tag) {
            var path = tag switch {
                string str => BundleFile.Container.Blocks.First(x => x.Path.Equals(str, StringComparison.InvariantCultureIgnoreCase)),
                UnityBundleBlock block => block,
                _ => throw new NotImplementedException(),
            };

            return new MemoryStream(BundleFile.OpenFile(path)) { Position = 0 };
        }

        public object GetTag(object baseTag, object parent) => baseTag;

        public void Dispose() {
            BundleFile.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
