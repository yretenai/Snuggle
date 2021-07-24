using System;
using System.IO;
using System.Linq;
using Equilibrium.IO;
using JetBrains.Annotations;

namespace Equilibrium.Models.Bundle {
    [PublicAPI]
    public interface IUnityContainer {
        public UnityBundleBlockInfo[] BlockInfos { get; set; }
        public UnityBundleBlock[] Blocks { get; set; }
        public long Length { get; }

        public byte[] OpenFile(string path, BiEndianBinaryReader? reader = null, Stream? stream = null) => OpenFile(Blocks.FirstOrDefault(x => x.Path.Equals(path, StringComparison.InvariantCultureIgnoreCase)), reader, stream);

        public byte[] OpenFile(UnityBundleBlock? block, BiEndianBinaryReader? reader = null, Stream? stream = null);
    }
}
