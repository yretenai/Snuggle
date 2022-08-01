using System;
using Snuggle.Core.SubScenes.Models;

namespace Snuggle.Core.SubScenes;

public class Catalog {
    public Catalog(Memory<byte> data) {
        var reader = new MemoryReader(data);
        var version = reader.Read<int>();
        if (version is not 1) {
            throw new NotSupportedException();
        }

        Header = reader.Read<AssetHeader>();
        Resources = reader.ReadBlobArray<ResourceMetadata>();
        Paths = reader.ReadBlobStringArray();
    }

    public AssetHeader Header { get; }
    public Memory<ResourceMetadata> Resources { get; }
    public string[] Paths { get; }
}
