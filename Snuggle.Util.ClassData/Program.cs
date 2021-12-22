using System;
using System.IO;
using System.Text;
using System.Text.Json;

namespace Snuggle.Util.ClassData;

public static class Program {
    private static void Main(string[] args) {
        if (args.Length < 2) {
            Console.WriteLine("Usage: Snuggle.Util.ClassData.exe path/to/classdata.tpk path/to/typetree/output");
            return;
        }

        switch (Path.GetExtension(args[0]).ToLowerInvariant()) {
            case ".json":
                ParseDump(args[0], args[1]);
                break;
        }
    }

    private static void ParseDump(string path, string outputPath) {
        var database = JsonSerializer.Deserialize<UnityInfo>(File.ReadAllText(path))!;
        foreach (var classObject in database.Classes) {
            if (classObject.ReleaseRootNode == null) {
                continue;
            }
            
            var name = classObject.Name;
            var result = Path.Combine(outputPath, database.Version, name + $"_{classObject.TypeID}.txt");
            var dir = Path.GetDirectoryName(result);
            var builder = new StringBuilder();
            PrintFields(classObject.ReleaseRootNode, builder);
            if (!Directory.Exists(dir)) {
                Directory.CreateDirectory(dir!);
            }

            if (builder.Length == 0) {
                continue;
            } 

            File.WriteAllText(result, builder.ToString());
        }
    }

    private static long GetUnityVersion(string version) {
        var split = version.Split('.');
        var first = uint.Parse(split[0]);
        var second = uint.Parse(split[1]);

        return first * 10000 + second;
    }

    private static void PrintFields(UnityNode? node, StringBuilder builder) {
        if (node == null) {
            return;
        }
        
        var typeName = node.TypeName;
        var fieldName = node.Name;
        if (string.IsNullOrEmpty(typeName) || string.IsNullOrEmpty(fieldName)) {
            return;
        }
        
        var isAligned = (node.MetaFlag & 0x4000) != 0 || typeName == "Array";

        builder.AppendLine($"{new string(' ', node.Level * 2)}{typeName} {fieldName} {node.ByteSize} {(isAligned ? "Aligned" : string.Empty)}");

        foreach (var subNode in node.SubNodes) {
            PrintFields(subNode, builder);
        }
    }
}
