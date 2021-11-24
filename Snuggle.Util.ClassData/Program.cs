using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using AssetsTools.NET;

namespace Snuggle.Util.ClassData;

public static class Program {
    private static void Main(string[] args) {
        if (args.Length < 2) {
            Console.WriteLine("Usage: Snuggle.Util.ClassData.exe path/to/classdata.tpk path/to/typetree/output");
            return;
        }
        
        var database = new ClassDatabasePackage();
        using var stream = File.OpenRead(args[0]);
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
                var path = Path.Combine(args[1], version, name + $"_{classType.classId}.txt");
                var dir = Path.GetDirectoryName(path);
                var builder = new StringBuilder();
                PrintFields(classType, databaseFile, false, builder, new HashSet<string>());
                if (!Directory.Exists(dir)) {
                    Directory.CreateDirectory(dir!);
                }

                File.WriteAllText(path, builder.ToString());
            }
        }
    }

    private static long GetUnityVersion(ClassDatabaseFile file) {
        var split = file.header.unityVersions[0].Split('.');
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

        builder.AppendLine($"{new string(' ', field.depth * 2)}{typeName} {fieldName} {field.size} {(isAligned ? "Aligned" : "")}");
    }
}
