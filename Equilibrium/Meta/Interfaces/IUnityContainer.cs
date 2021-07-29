using System;
using System.IO;
using System.Linq;
using Equilibrium.IO;
using Equilibrium.Meta.Options;
using Equilibrium.Models.Bundle;
using JetBrains.Annotations;

namespace Equilibrium.Meta.Interfaces {
    [PublicAPI]
    public interface IUnityContainer {
        public UnityBundleBlockInfo[] BlockInfos { get; set; }
        public UnityBundleBlock[] Blocks { get; set; }
        public long Length { get; }

        public Stream OpenFile(string path, EquilibriumOptions options, BiEndianBinaryReader? reader = null, Stream? stream = null) => OpenFile(Blocks.FirstOrDefault(x => x.Path.Equals(path, StringComparison.InvariantCultureIgnoreCase)), options, reader, stream);

        public Stream OpenFile(UnityBundleBlock? block, EquilibriumOptions options, BiEndianBinaryReader? reader = null, Stream? stream = null);

        public void ToWriter(BiEndianBinaryWriter writer, UnityBundle header, EquilibriumOptions options, UnityBundleBlock[] blocks, Stream blockStream, BundleSerializationOptions bundleSerializationOptions);
    }
}
