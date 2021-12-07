using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using AssetsTools.NET;

namespace Snuggle.Util.ClassData;

public static class Program {
    private static void Main(string[] args) {
        if (args.Length < 2) {
            Console.WriteLine("Usage: Snuggle.Util.ClassData.exe path/to/classdata.tpk path/to/typetree/output");
            return;
        }

        switch (Path.GetExtension(args[0]).ToLowerInvariant()) {
            case ".tpk":
                ParseClassDatabase(args[0], args[1]);
                break;
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

    private static void ParseClassDatabase(string path, string outputPath) {
        var database = new ClassDatabasePackage();
        using var stream = File.OpenRead(path);
        using var reader = new AssetsFileReader(stream);
        database.Read(reader);

        var lastVersion = string.Empty;
        foreach (var databaseFile in database.files.OrderBy(GetUnityVersion)) {
            var version = databaseFile.header.unityVersions[0].Replace('*', '0');
            if (!string.IsNullOrEmpty(lastVersion)) {
                Console.WriteLine($"diff -wrU 10 {lastVersion} {version} > diffs/{version}.patch");
            }

            lastVersion = version;

            foreach (var classType in databaseFile.classes) {
                var nameRef = classType.name;
                var name = nameRef.GetString(databaseFile);
                var result = Path.Combine(outputPath, version, name + $"_{classType.classId}.txt");
                var dir = Path.GetDirectoryName(result);
                var builder = new StringBuilder();
                PrintFields(classType, databaseFile, false, builder, new HashSet<string>());
                if (!Directory.Exists(dir)) {
                    Directory.CreateDirectory(dir!);
                }

                if (builder.Length == 0) {
                    continue;
                } 

                File.WriteAllText(result, builder.ToString());
            }
        }
    }

    private static long GetUnityVersion(ClassDatabaseFile file) {
        return GetUnityVersion(file.header.unityVersions[0]);
    }

    private static long GetUnityVersion(string version) {
        var split = version.Split('.');
        var first = uint.Parse(split[0]);
        var second = uint.Parse(split[1]);

        return first * 10000 + second;
    }

    private static void PrintFields(ClassDatabaseType? classType, ClassDatabaseFile databaseFile, bool skipDepthZero, StringBuilder builder, HashSet<string> done) {
        if (classType == null) {
            return;
        }

        if (!skipDepthZero) {
            foreach (var field in classType.fields.Where(x => x.depth == 0)) {
                PrintField(databaseFile, field, builder, done);
            }
        }

        foreach (var field in classType.fields.Where(x => x.depth > 0)) {
            PrintField(databaseFile, field, builder, done);
        }
    }

    private static void PrintField(ClassDatabaseFile databaseFile, ClassDatabaseTypeField field, StringBuilder builder, ISet<string> done) {
        var typeNameRef = field.typeName;
        var typeName = typeNameRef.GetString(databaseFile);
        var fieldNameRef = field.fieldName;
        var fieldName = fieldNameRef.GetString(databaseFile);
        if (string.IsNullOrEmpty(typeName) || string.IsNullOrEmpty(fieldName)) {
            return;
        }

        if (field.depth == 1) {
            if (!done.Add($"{typeName} {fieldName}")) {
                return;
            }
        }

        var isAligned = (field.flags2 & 0x4000) != 0 || typeName == "Array";

        builder.AppendLine($"{new string(' ', field.depth * 2)}{typeName} {fieldName} {field.size} {(isAligned ? "Aligned" : string.Empty)}");
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
