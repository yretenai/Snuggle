using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
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
    bool ToStream(UnityBundleBlock[] blocks, Stream dataStream, BundleSerializationOptions serializationOptions, [MaybeNullWhen(false)] out Stream bundleStream);
}
