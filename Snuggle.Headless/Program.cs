using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using DragonLib;
using DragonLib.CommandLine;
using Serilog;
using Snuggle.Converters;
using Snuggle.Core;
using Snuggle.Core.Implementations;
using Snuggle.Core.Interfaces;
using Snuggle.Core.Models;
using Snuggle.Core.Models.Serialization;
using Snuggle.Core.Options;

namespace Snuggle.Headless;

public static class Program {
    public static int Main() {
        Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();

        var flags = CommandLineFlagsParser.ParseFlags<SnuggleFlags>();
        if (flags == null) {
            return 1;
        }

        SystemManagement.DescribeLog();
        Log.Debug(flags.ToString());
        Log.Debug("Args: {Args}", string.Join(' ', Environment.GetCommandLineArgs()[1..]));

        var files = new List<string>();
        foreach (var entry in flags.Paths) {
            if (Directory.Exists(entry)) {
                var dir = Directory.GetFiles(entry, "*", flags.Recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
                Log.Information("Found {Count} files in {Entry}", dir.Length, entry);
                files.AddRange(dir);
            } else if (File.Exists(entry)) {
                Log.Information("Found {Entry}", entry);
                files.Add(entry);
            } else {
                Log.Information("Could not find {Entry}", entry);
            }
        }

        var fileSet = files.ToHashSet();
        Log.Information("Found {Count} files.", files.Count);

        if (files.Count == 0) {
            Log.Information("No files found, exiting...");
            return 0;
        }

        var collection = new AssetCollection();

        if (flags.ExclusiveClassIds.Any()) {
            foreach (var classId in Enum.GetNames<UnityClassId>()) {
                if (flags.ExclusiveClassIds.Contains(classId)) {
                    continue;
                }

                flags.IgnoreClassIds.Add(classId);
            }
        }

        var options = SnuggleCoreOptions.Default with { Game = flags.Game, CacheDataIfLZMA = true, IgnoreClassIds = flags.IgnoreClassIds };

        foreach (var file in fileSet) {
            collection.LoadFile(file, options);
        }

        Log.Information("Caching GameObject ClassIds");
        collection.CacheGameObjectClassIds();
        Log.Information("Finding container paths...");
        collection.FindResources();
        Log.Information("Building GameObject Graph...");
        collection.BuildGraph();
        Log.Information("Collecting Memory...");
        AssetCollection.Collect();
        Log.Information("Memory Tension: {Size}", GC.GetTotalMemory(false).GetHumanReadableBytes());

        if (flags.DumpSerializedInfo) {
            foreach (var (_, file) in collection.Files) {
                var ext = "json";
                var path = PathFormatter.Format(flags.OutputFormat, ext, new SerializedObject(new UnityObjectInfo(0, 0, 0, 0, UnityClassId.Object, 0, false, 0, false), file));
                var fullPath = Path.Combine(flags.OutputPath, path);
                if (File.Exists(fullPath)) {
                    continue;
                }

                fullPath.EnsureDirectoryExists();
                File.WriteAllBytes(fullPath, JsonSerializer.SerializeToUtf8Bytes(new { file.ExternalInfos, file.Tag, file.Name, file.UserInformation }, SnuggleCoreOptions.JsonOptions));
            }
        }

        foreach (var asset in collection.Files.SelectMany(x => x.Value.GetAllObjects())) {
            if (asset == null) {
                continue;
            }

            if (asset.GetType().FullName == typeof(SerializedObject).FullName) {
                continue;
            }

            if (flags.PathIdFilters.Any() && !flags.PathIdFilters.Contains(asset.PathId)) {
                continue;
            }

            if (flags.NameFilters.Any() && !flags.NameFilters.Any(x => x.IsMatch(asset.ObjectComparableName))) {
                continue;
            }

            if (flags.OnlyCAB && string.IsNullOrWhiteSpace(asset.ObjectContainerPath)) {
                continue;
            }

            if (flags.DataOnly || flags.GameObjectOnly) {
                if (flags.GameObjectOnly && asset is not GameObject) {
                    continue;
                }

                Log.Information("Dumping Data for asset {Asset}", asset);
                ConvertCore.ConvertObject(flags, asset);
                continue;
            }

            try {
                switch (asset) {
                    case ITexture texture when flags.LooseTextures:
                        Log.Information("Processing Texture {Asset}", asset);
                        ConvertCore.ConvertTexture(flags, texture, true);
                        break;
                    case Mesh mesh when flags.LooseMeshes:
                        Log.Information("Processing Mesh {Asset}", asset);
                        ConvertCore.ConvertMesh(flags, mesh);
                        break;
                    case GameObject gameObject when flags.GameObject:
                        Log.Information("Processing GameObject {Asset}", asset);
                        ConvertCore.ConvertGameObject(flags, gameObject);
                        break;
                    case MeshRenderer renderer when flags.Mesh && renderer.GameObject.Value is not null:
                        Log.Information("Processing GameObject {Asset}", renderer.GameObject.Value);
                        ConvertCore.ConvertGameObject(flags, renderer.GameObject.Value);
                        break;
                    case SkinnedMeshRenderer renderer when flags.SkinnedMesh && renderer.GameObject.Value is not null:
                        Log.Information("Processing GameObject {Asset}", renderer.GameObject.Value);
                        ConvertCore.ConvertGameObject(flags, renderer.GameObject.Value);
                        break;
                    case Material material when flags.LooseMaterials:
                        Log.Information("Processing Material {Asset}", asset);
                        ConvertCore.ConvertMaterial(flags, material);
                        break;
                    case Text text when flags.Text:
                        Log.Information("Processing Text {Asset}", asset);
                        ConvertCore.ConvertText(flags, text);
                        break;
                    case Sprite sprite when flags.Sprite:
                        Log.Information("Processing Sprite {Asset}", asset);
                        ConvertCore.ConvertSprite(flags, sprite);
                        break;
                    case AudioClip clip when flags.Audio:
                        Log.Information("Processing Audio {Asset}", asset);
                        ConvertCore.ConvertAudio(flags, clip);
                        break;
                    case MonoBehaviour monoBehaviour when flags.Script:
                        if (flags.ScriptFilters.Any() && (monoBehaviour.Script.Value == null || !flags.ScriptFilters.Any(x => x.IsMatch(monoBehaviour.Script.Value.ObjectComparableName)))) {
                            continue;
                        }

                        if (flags.AssemblyFilters.Any() && (monoBehaviour.Script.Value == null || !flags.AssemblyFilters.Any(x => x.IsMatch(monoBehaviour.Script.Value.AssemblyName)))) {
                            continue;
                        }

                        Log.Information("Processing MonoBehaviour {Asset}", asset);
                        ConvertCore.ConvertMonoBehaviour(flags, monoBehaviour);
                        break;
                    case ICABPathProvider cabPathProvider when flags.CAB:
                        Log.Information("Processing CAB Path Provider {Asset}", asset);
                        ConvertCore.ConvertCABPathProvider(flags, cabPathProvider);
                        break;
                }
            } catch (Exception e) {
                Log.Error(e, "Failure decoding {PathId} from {Tag}", asset.PathId, IFileHandler.UnpackTagToString(asset.SerializedFile.Tag));
            }

            if (flags.LowMemory) {
                ConvertCore.ClearMemory(collection);
            }
        }

        return 0;
    }
}
