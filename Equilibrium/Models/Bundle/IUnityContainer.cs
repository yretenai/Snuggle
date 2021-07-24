using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Equilibrium.IO;
using JetBrains.Annotations;

namespace Equilibrium.Models.Bundle {
    [PublicAPI]
    public interface IUnityContainer {
        public ImmutableArray<UnityBundleBlockInfo> BlockInfos { get; set; }
        public ImmutableArray<UnityBundleBlock> Blocks { get; set; }
        public long Length { get; }

        public Span<byte> OpenFile(string path, BiEndianBinaryReader? reader = null, Stream? stream = null) => OpenFile(Blocks.FirstOrDefault(x => x.Path.Equals(path, StringComparison.InvariantCultureIgnoreCase)), reader, stream);

        public Span<byte> OpenFile(UnityBundleBlock? block, BiEndianBinaryReader? reader = null, Stream? stream = null);
    }
}
