using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Equilibrium.IO;
using JetBrains.Annotations;

namespace Equilibrium.Models.Bundle {
    [PublicAPI]
    public interface IUnityContainer {
        public ICollection<UnityBundleBlockInfo>? BlockInfos { get; set; }
        public ICollection<UnityBundleBlock>? Blocks { get; set; }

        public Span<byte> OpenFile(string path, BiEndianBinaryReader? reader = null, Stream? stream = null) => OpenFile(Blocks?.FirstOrDefault(x => x.Path.Equals(path, StringComparison.InvariantCultureIgnoreCase)), reader, stream);

        public Span<byte> OpenFile(UnityBundleBlock? block, BiEndianBinaryReader? reader = null, Stream? stream = null);
    }
}
