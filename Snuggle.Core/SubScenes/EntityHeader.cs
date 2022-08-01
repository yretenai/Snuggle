using System;
using Snuggle.Core.SubScenes.Models;

namespace Snuggle.Core.SubScenes;

public class EntityHeader {
    public EntityHeader(Memory<byte> data) {
        var reader = new MemoryReader(data);
        var version = reader.Read<int>();
        if (version is not 1 and not 3) {
            // valid versions: yep
            // 3: 0.14.0p18 -- changes: Added Dependencies, Section now has a ptr for runtime asset data.
            //                          however, this is always zero in the file-- because it's a memory pointer. 
            // 1: 0.13.0p14
            throw new NotSupportedException();
        }

        Header = reader.Read<AssetHeader>();
        Sections = reader.ReadBlobArray<EntitySection>();
        Name = reader.ReadBlobString();
        Dependencies = version >= 3 ? reader.ReadBlobArray2D<Hash128>() : Array.Empty<Memory<Hash128>>();

        CustomMetadata = reader.ReadBlobClassArray2D<EntityCustomMetadata>();
    }

    public AssetHeader Header { get; set; }
    public Memory<EntitySection> Sections { get; set; }
    public string Name { get; set; }
    public Memory<Hash128>[] Dependencies { get; set; }
    public EntityCustomMetadata[][] CustomMetadata { get; set; }
}
