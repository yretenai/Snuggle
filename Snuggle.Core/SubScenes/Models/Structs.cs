using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Snuggle.Core.SubScenes.Models;

public enum Codec {
    None,
    LZ4,
}

public enum ResourceMetadataType {
    Unknown,
    Scene,
}

public enum TypeIndexFlags {
    HasNoEntityReferences = 1 << 24,
    SystemStateType = 1 << 25,
    BufferComponentType = 1 << 26,
    SharedComponentType = 1 << 27,
    ManagedComponentType = 1 << 28,
    ChunkComponentType = 1 << 29,
    ZeroSizeInChunkType = 1 << 30,
}

public enum TypeCategory {
    ComponentData,
    BufferData,
    SharedComponentData,
    EntityData,
    UnityEngineObject,
}

[StructLayout(LayoutKind.Sequential, Pack = 1, Size = 0x20)]
public record struct AssetHeader(long Validation, int Length, int Allocator, int Hash);

[StructLayout(LayoutKind.Sequential, Pack = 1, Size = 0x18)]
public record struct ResourceMetadata(Hash128 ResourceId, int Flags, ResourceMetadataType Type);

[StructLayout(LayoutKind.Sequential, Pack = 1, Size = 0x3C)]
public readonly record struct EntitySection(Hash128 Guid, int Index, int Size, int RefCount, Vector3 Min, Vector3 Max, Codec Codec, int DecompressedSize);

[StructLayout(LayoutKind.Sequential, Pack = 1, Size = 0x10)]
public readonly record struct SharedComponentRecord(ulong TypeHash, int Size);

[StructLayout(LayoutKind.Sequential, Pack = 1, Size = 0x10)]
public readonly record struct AssetBatch(int TotalBatchSize, int BlobAssetHeaderCount, int RefCount);

[StructLayout(LayoutKind.Sequential, Pack = 1, Size = 0x8)]
public readonly record struct EntityChunkPatch(int Offset, int Length);

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct TypeInfo(ulong TypeHash, ulong MemoryHash, int Alignment, int Size, int ElementSize, int TypeSize, int BufferCapacity, int ChunkCapacity, TypeCategory Category);

public readonly record struct TypeAssembly(string Assembly, string Name, TypeAssembly[] Generics);

public readonly record struct TypeMap(TypeInfo Type, Memory<int> EntityOffsets, Memory<int> BlobOffsets, TypeAssembly TypeAssemblies);

public record EntityComponent(TypeInfo TypeInfo, TypeAssembly TypeName, object? Data);

public record struct EntityInstance(EntityComponent[] Components);

[StructLayout(LayoutKind.Sequential, Pack = 1, Size = 0x8)]
public record struct Entity(int Index, int Version);

[StructLayout(LayoutKind.Sequential, Pack = 1, Size = 0x40)]
public record struct ChunkHeader(long ArchetypeIndex, Entity MetaEntity, int Count, int Capacity, int ListIndex, int EmptyListIndex, uint Flags, ulong SequenceNumber);
