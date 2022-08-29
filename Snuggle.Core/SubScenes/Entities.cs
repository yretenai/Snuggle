using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using Mono.Cecil;
using Mono.Cecil.Rocks;
using Snuggle.Core.SubScenes.Models;
using Unity.SerializationLogic;

namespace Snuggle.Core.SubScenes;

public class Entities {
    public Entities(Memory<byte> data) {
        var reader = new MemoryReader(data);
        var version = reader.Read<int>();

        // file format completely changed after version 57.
        // I only have files for version 57.
        if (version > 57) {
            throw new NotSupportedException();
        }

        TypeHashes = reader.ReadArray<ulong>();
        Archetypes = reader.ReadClassArray<EntityArchetype>();
        AssetSize = reader.Read<int>();
        AssetBatchInfo = reader.Read<AssetBatch>();
        AssetBatchHeaders = new AssetHeader[AssetBatchInfo.BlobAssetHeaderCount].AsMemory();
        AssetBatch = new Memory<byte>[AssetBatchInfo.BlobAssetHeaderCount];
        for (var i = 0; i < AssetBatchInfo.BlobAssetHeaderCount; ++i) {
            AssetBatchHeaders.Span[i] = reader.Read<AssetHeader>();
            AssetBatch[i] = reader.ReadArray<byte>(AssetBatchHeaders.Span[i].Length);
        }

        SharedComponents = reader.ReadArray<int>();
        SharedComponentRecords = reader.ReadArray<SharedComponentRecord>();

        var managedLength = reader.Read<int>();
        ManagedCount = reader.Read<int>();
        ManagedBuffer = reader.ReadArray<byte>(managedLength);

        RawChunks = reader.ReadClassArray<EntityChunk>();
        Chunks = new Memory<byte>[RawChunks.Length];
        for (var i = 0; i < RawChunks.Length; ++i) {
            var chunk = RawChunks[i];
            if (chunk.PatchHeaders.Length > 0) {
                var maxSize = Math.Max(chunk.Data.Length, chunk.PatchHeaders.Max(x => x.Offset + x.Length));
                var chunkData = new byte[maxSize].AsMemory();
                chunk.Data.CopyTo(chunkData);
                for (var j = 0; j < chunk.PatchHeaders.Length; ++j) {
                    var chunkHeader = chunk.PatchHeaders[j];
                    var chunkPatch = chunk.Patches[j];
                    chunkPatch.CopyTo(chunkData[chunkHeader.Offset..]);
                }

                Chunks[i] = chunkData;
            } else {
                Chunks[i] = chunk.Data;
            }
        }
    }

    public Memory<ulong> TypeHashes { get; set; }
    public EntityArchetype[] Archetypes { get; set; }
    public int AssetSize { get; set; }
    public AssetBatch AssetBatchInfo { get; set; }
    public Memory<AssetHeader> AssetBatchHeaders { get; set; }
    public Memory<byte>[] AssetBatch { get; set; }
    public Memory<int> SharedComponents { get; set; }
    public Memory<SharedComponentRecord> SharedComponentRecords { get; set; }
    public int ManagedCount { get; set; }
    public Memory<byte> ManagedBuffer { get; set; }
    public EntityChunk[] RawChunks { get; set; }
    public Memory<byte>[] Chunks { get; set; }

    public EntityInstance[][] DecodeEntities(Dictionary<ulong, TypeMap> typeMap, AssetCollection collection) {
        var typeHashes = new ulong[TypeHashes.Length + 1].AsSpan();
        TypeHashes.Span.CopyTo(typeHashes);
        typeHashes[^1] = typeMap.First(x => x.Value.Type.Category == TypeCategory.EntityData).Key; // every archetype has an entity, but it's not listed :dab:
        var instances = new EntityInstance[Archetypes.Length][];
        var sharedComponentOffsets = new int[Archetypes.Length];
        var sharedComponentOffset = 0;
        for (var index = 0; index < Archetypes.Length; index++) {
            sharedComponentOffsets[index] = sharedComponentOffset;
            var archetype = Archetypes[index];
            if (!archetype.HasCalculatedOffsets) {
                archetype.CalculateOffsets(typeHashes, typeMap);
            }

            sharedComponentOffset += archetype.SharedComponentCount;

            instances[index] = new EntityInstance[archetype.EntityCount];
        }

        var sharedData = new Memory<byte>[SharedComponentRecords.Length];
        var sharedOffset = 0;
        for (var i = 0; i < SharedComponentRecords.Length; ++i) {
            var length = SharedComponentRecords.Span[i].Size;
            if (length == -1) {
                length = typeMap[SharedComponentRecords.Span[i].TypeHash].Type.TypeSize;
            }

            // shared data is serialized using BinarySerialiation, not to be confused with MonoBehaviour Serialization, or memcpy serialization, or DNX Binary Serialization.
            // so far I think every single managed object is a simple object, but that's not a guarantee.
            Debug.Assert(ManagedBuffer.Span[sharedOffset] == 0, "ManagedBuffer.Span[sharedOffset] == TokenNone");

            sharedOffset += 1;
            sharedData[i] = ManagedBuffer.Slice(sharedOffset, length).ToArray().AsMemory();
            sharedOffset += length;
        }

        foreach (var chunk in Chunks) {
            var chunkHeader = MemoryMarshal.Read<ChunkHeader>(chunk.Span);
            var archetype = Archetypes[chunkHeader.ArchetypeIndex];

            for (var i = 0; i < chunkHeader.Count; ++i) {
                var entityInstance = instances[chunkHeader.ArchetypeIndex][i + chunkHeader.ListIndex];
                entityInstance.Components = new EntityComponent[archetype.Types.Length];
                var sharedComponentInnerOffset = 0;
                for (var j = 0; j < archetype.Types.Span.Length; j++) {
                    var typeIndex = archetype.Types.Span[j];
                    var type = typeMap[typeHashes[typeIndex & 0x00FFFFFF]];
                    var size = archetype.Sizes.Span[j];
                    var offset = archetype.Offsets.Span[j] + size * i;
                    var slice = chunk.Slice(0x40 + offset, size).ToArray().AsMemory();
                    // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
                    switch (type.Type.Category) {
                        case TypeCategory.ComponentData:
                        case TypeCategory.EntityData:
                            entityInstance.Components[j] = new EntityComponent(type.Type, type.TypeAssemblies, ObjectReader.ReadUnmanagedObject(collection.Assemblies, type.TypeAssemblies, slice));
                            break;
                        case TypeCategory.BufferData: {
                            var count = MemoryMarshal.Read<int>(slice[8..].Span);
                            var buffer = new byte[count][];
                            for (var k = 0; k < count; ++k) {
                                buffer[k] = slice.Slice(16 + k * type.Type.ElementSize, type.Type.ElementSize).ToArray();
                            }

                            var bufferData = new EntityComponent(type.Type, type.TypeAssemblies, buffer);

                            entityInstance.Components[j] = bufferData;
                            break;
                        }
                        case TypeCategory.SharedComponentData: {
                            var globalComponentIndex = sharedComponentOffsets[chunkHeader.ArchetypeIndex] + sharedComponentInnerOffset++;
                            var sharedComponentIndex = SharedComponents.Span[globalComponentIndex];
                            var sharedComponentRecord = SharedComponentRecords.Span[sharedComponentIndex - 1];
                            Debug.Assert(sharedComponentRecord.TypeHash == type.Type.TypeHash);
                            entityInstance.Components[j] = new EntityComponent(type.Type, type.TypeAssemblies, sharedComponentRecord);
                            break;
                        }
                        case TypeCategory.UnityEngineObject:
                            Debug.Assert(size == 4, "size == 4");
                            entityInstance.Components[j] = new EntityComponent(type.Type, type.TypeAssemblies, MemoryMarshal.Read<int>(slice.Span));
                            break;
                    }
                }

                instances[chunkHeader.ArchetypeIndex][i + chunkHeader.ListIndex] = entityInstance;
            }
        }

        return instances;
    }
}

public class ObjectReader {
    public static object? ReadUnmanagedObject(AssemblyResolver resolver, TypeAssembly typeRef, Memory<byte> data) {
        if (data.Length == 0) {
            return null;
        }

        var reader = new MemoryReader(data);
        return ReadUnmanagedObject(ConstructType(resolver, typeRef), reader);
    }

    public static object? ReadUnmanagedObject(TypeReference typeReference, MemoryReader reader) {
        switch (typeReference.FullName) {
            case "System.Byte":
            case "System.UInt8":
                return reader.Read<byte>();
            case "System.SByte":
            case "System.Int8":
                return reader.Read<sbyte>();
            case "System.UInt16":
                return reader.Read<ushort>();
            case "System.Int16":
                return reader.Read<short>();
            case "System.UInt32":
                return reader.Read<uint>();
            case "System.Int32":
                return reader.Read<int>();
            case "System.UInt64":
                return reader.Read<ulong>();
            case "System.Int64":
                return reader.Read<long>();
            case "System.Half":
                return reader.Read<Half>();
            case "System.Single":
                return reader.Read<float>();
            case "System.Double":
                return reader.Read<double>();
            case "System.Decimal":
                return reader.Read<decimal>();
            case "System.Guid":
                return reader.Read<Guid>();
            case "Unity.Entities.Hash128":
                return reader.Read<Hash128>();
            case "Unity.Entities.Entity":
                return reader.Read<Entity>();
            case "Unity.Entities.BlobAssetReferenceData": // :hollow:
                return reader.Partition(8).Data.ToArray();
        }

        if (typeReference.Namespace.StartsWith("System.")) {
            throw new NotImplementedException();
        }

        if (UnitySerializationLogic.IsUnityEngineObject(typeReference)) {
            return reader.Read<int>(); // todo: read ReferencedSceneAssets.
        }

        var typeDefinition = typeReference.Resolve();

        var fields = new Dictionary<string, object?>();
        var baseOffset = -1;
        foreach (var field in typeDefinition.Fields) {
            if (field.IsStatic) {
                continue;
            }

            var cpp2ilAttribute = field.CustomAttributes.FirstOrDefault(x => x.AttributeType.FullName == "Cpp2IlInjected.FieldOffsetAttribute");
            if (cpp2ilAttribute == null) { // we need accurate field offsets.
                return null;
            }

            var offset = int.Parse(((string) cpp2ilAttribute.Fields.First(x => x.Name == "Offset").Argument.Value)[2..], NumberStyles.HexNumber);
            if (baseOffset == -1) {
                baseOffset = offset;
                offset = 0;
            } else {
                offset -= baseOffset;
            }

            var type = ResolveType(field.FieldType, typeReference);
            fields[field.Name] = ReadUnmanagedObject(type, reader.Partition(offset, -1));
        }

        return fields;
    }

    private static TypeReference ResolveType(TypeReference typeReference, TypeReference declaringType) {
        if (!typeReference.IsGenericParameter || declaringType is not GenericInstanceType genericInstanceType) {
            return typeReference;
        }

        for (var i = 0; i < genericInstanceType.GenericParameters.Count; ++i) {
            if (genericInstanceType.Name == typeReference.Name) {
                return genericInstanceType.GenericArguments[i];
            }
        }

        return typeReference;
    }

    private static TypeReference ConstructType(AssemblyResolver resolver, TypeAssembly typeRef) {
        var asm = resolver.Resolve(typeRef.Assembly);
        var type = asm.MainModule.GetType(typeRef.Name);
        if (typeRef.Generics.Length > 0) {
            var generics = typeRef.Generics.Select(_ => ConstructType(resolver, typeRef)).ToArray();
            return type.MakeGenericInstanceType(generics);
        }

        return type;
    }
}
