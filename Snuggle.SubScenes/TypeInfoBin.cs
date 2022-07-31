using Snuggle.SubScenes.Models;

namespace Snuggle.SubScenes;

public static class TypeInfoBin {
    public static Dictionary<ulong, TypeMap> Read(Memory<byte> data) {
        var reader = new MemoryReader(data);
        var version = reader.Read<int>();
        if (version > 1) {
            throw new NotSupportedException();
        }
        var count = reader.Read<int>();
        var entries = new Dictionary<ulong, TypeMap>(count);
        for (var i = 0; i < count; ++i) {
            var typeInfo = reader.Read<TypeInfo>();
            var entityOffset = reader.ReadArray<int>();
            var blobOffset = reader.ReadArray<int>();
            var typeAsm = ReadTypeAssembly(reader);
            entries[typeInfo.TypeHash] = new TypeMap(typeInfo, entityOffset, blobOffset, typeAsm);
        }

        return entries;
    }

    private static TypeAssembly ReadTypeAssembly(MemoryReader reader) {
        var asm = reader.ReadString();
        var type = reader.ReadString();
        var count = reader.Read<int>();
        var generics = new TypeAssembly[count];
        for (var i = 0; i < count; ++i) {
            generics[i] = ReadTypeAssembly(reader);
        }

        return new TypeAssembly(asm, type, generics);
    }
}
