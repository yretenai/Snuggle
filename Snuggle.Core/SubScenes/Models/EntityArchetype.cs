using System;
using System.Collections.Generic;

namespace Snuggle.Core.SubScenes.Models;

public class EntityArchetype {
    public EntityArchetype() {
        EntityCount = 0;
        Types = Memory<int>.Empty;
    }

    public EntityArchetype(MemoryReader reader) {
        EntityCount = reader.Read<int>();
        Types = reader.ReadArray<int>();
    }

    public int EntityCount { get; set; }
    public Memory<int> Types { get; set; }
    public Memory<int> Sizes { get; set; }
    public Memory<int> Offsets { get; set; }
    public int SharedComponentCount { get; set; }
    public bool HasCalculatedOffsets => Offsets.Length == Types.Length;

    private static int GetComponentArraySize(int componentSize, int entityCount) => unchecked(componentSize * entityCount + (64 - 1)) & ~(64 - 1);

    private static int CalculateSpaceRequirement(Span<int> componentSizes, int componentCount, int entityCount) {
        var size = 0;
        for (var i = 0; i < componentCount; ++i) {
            size += GetComponentArraySize(componentSizes[i], entityCount);
        }

        return size;
    }

    private static int CalculateChunkCapacity(int bufferSize, Span<int> componentSizes, int count) {
        var totalSize = 0;
        for (var i = 0; i < count; ++i) {
            totalSize += componentSizes[i];
        }

        if (totalSize == 0) {
            return bufferSize;
        }

        var capacity = bufferSize / totalSize;
        while (CalculateSpaceRequirement(componentSizes, count, capacity) > bufferSize) {
            --capacity;
        }

        return capacity;
    }

    public void CalculateOffsets(Span<ulong> typeHashes, Dictionary<ulong, TypeMap> typeMap) {
        if (HasCalculatedOffsets) {
            return;
        }

        var chunkTypes = new int[Types.Length + 1].AsMemory();
        chunkTypes.Span[0] = typeHashes.Length - 1;
        Types.CopyTo(chunkTypes[1..]);
        Types = chunkTypes;

        Sizes = new int[Types.Length].AsMemory();
        Offsets = new int[Types.Length].AsMemory();
        var bufferCapacities = new int[Types.Length];
        var maxCapacity = int.MaxValue;
        for (var i = 0; i < Types.Length; ++i) {
            var type = typeMap[typeHashes[Types.Span[i]]].Type;
            if (type.Category == TypeCategory.SharedComponentData) {
                SharedComponentCount++;
            }

            Sizes.Span[i] = type.Size;
            bufferCapacities[i] = type.BufferCapacity;
            if (type.Size > 0) {
                maxCapacity = Math.Min(maxCapacity, type.ChunkCapacity);
            }
        }

        var chunkCapacity = Math.Min(maxCapacity, CalculateChunkCapacity(0x3FC0, Sizes.Span, Sizes.Span.Length));
        var count = Types.Length;
        var memoryOrderings = new ulong[count];
        var typeFlags = new int[count];

        for (var i = 0; i < count; ++i) {
            var typeIndex = Types.Span[i];
            var type = typeMap[typeHashes[typeIndex]].Type;
            memoryOrderings[i] = type.MemoryHash;
            typeFlags[i] = typeIndex & ~0x00FFFFFF;
        }

        bool MemoryOrderCompare(int lhs, int rhs) {
            if (typeFlags[lhs] == typeFlags[rhs]) {
                return memoryOrderings[lhs] < memoryOrderings[rhs];
            }

            return typeFlags[lhs] < typeFlags[rhs];
        }

        var typeMemoryOrder = new int[count];
        for (var i = 0; i < count; ++i) {
            var index = i;
            while (index > 1 && MemoryOrderCompare(i, typeMemoryOrder[index - 1])) {
                typeMemoryOrder[index] = typeMemoryOrder[index - 1];
                --index;
            }

            typeMemoryOrder[index] = i;
        }

        var usedBytes = 0;
        for (var i = 0; i < count; ++i) {
            var index = typeMemoryOrder[i];
            var sizeOf = Sizes.Span[index];
            Offsets.Span[i] = usedBytes;
            usedBytes += GetComponentArraySize(sizeOf, chunkCapacity);
        }
    }
}
