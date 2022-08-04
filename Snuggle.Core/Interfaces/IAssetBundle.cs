using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Snuggle.Core.Meta;
using Snuggle.Core.Models.Bundle;
using Snuggle.Core.Options;

namespace Snuggle.Core.Interfaces;

public interface IAssetBundle : IDisposable, IRenewable {
    long Length { get; }
    UnityVersion Version { get; }
    SnuggleCoreOptions Options { get; init; }
    Stream OpenFile(string path);
    Stream OpenFile(UnityBundleBlock block);
    IEnumerable<UnityBundleBlock> GetBlocks();
    bool ToStream(UnityBundleBlock[] blocks, Stream dataStream, BundleSerializationOptions serializationOptions, Stream bundleStream);

    public static bool RebuildBundle(IAssetBundle bundle, IEnumerable<UnityBundleBlock> blocks, BundleSerializationOptions serializationOptions, Stream outputStream) {
        var blocksArray = blocks is UnityBundleBlock[] ? (UnityBundleBlock[]) blocks : blocks.ToArray();
        using var dataStream = bundle.OpenFile(new UnityBundleBlock(0, blocksArray.Select(x => x.Size).Sum(), 0, string.Empty));
        return bundle.ToStream(blocksArray, dataStream, serializationOptions, outputStream);
    }
}
