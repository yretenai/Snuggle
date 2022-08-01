using System;

namespace Snuggle.Core.SubScenes.Models;

public class EntityChunk {
    public EntityChunk() {
        Data = Memory<byte>.Empty;
        PatchHeaders = Array.Empty<EntityChunkPatch>();
        Patches = Array.Empty<Memory<byte>>();
    }

    public EntityChunk(MemoryReader reader) {
        Data = reader.ReadArray<byte>(0x4000);
        PatchHeaders = reader.ReadArray<EntityChunkPatch>().ToArray();
        Patches = new Memory<byte>[PatchHeaders.Length];
        for (var i = 0; i < PatchHeaders.Length; ++i) {
            Patches[i] = reader.ReadArray<byte>(PatchHeaders[i].Length);
        }
    }

    public Memory<byte> Data { get; set; }
    public EntityChunkPatch[] PatchHeaders { get; set; }
    public Memory<byte>[] Patches { get; set; }
}
