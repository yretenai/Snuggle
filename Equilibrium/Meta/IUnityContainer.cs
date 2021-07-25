using System;
using System.IO;
using System.Linq;
using Equilibrium.IO;
using Equilibrium.Models.Bundle;
using JetBrains.Annotations;

namespace Equilibrium.Meta {
    [PublicAPI]
    public interface IUnityContainer {
        public UnityBundleBlockInfo[] BlockInfos { get; set; }
        public UnityBundleBlock[] Blocks { get; set; }
        public long Length { get; }

        public Stream OpenFile(string path, BiEndianBinaryReader? reader = null, Stream? stream = null) => OpenFile(Blocks.FirstOrDefault(x => x.Path.Equals(path, StringComparison.InvariantCultureIgnoreCase)), reader, stream);

        public Stream OpenFile(UnityBundleBlock? block, BiEndianBinaryReader? reader = null, Stream? stream = null);
    }
}
