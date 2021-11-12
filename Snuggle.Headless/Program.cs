using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using DragonLib;
using DragonLib.CLI;
using Snuggle.Core;
using Snuggle.Core.Implementations;
using Snuggle.Core.Logging;
using Snuggle.Core.Meta;
using Snuggle.Core.Options;

namespace Snuggle.Headless;

public static class Program {
    public static int Main() {
        var flags = CommandLineFlags.ParseFlags<SnuggleFlags>();
        if (flags == null) {
            return 1;
        }

        var files = new List<string>();
        foreach (var entry in flags.Paths) {
            if (Directory.Exists(entry)) {
                files.AddRange(Directory.EnumerateFiles(entry, "*", flags.Recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly));
            } else if (File.Exists(entry)) {
                files.Add(entry);
            }
        }

        var fileSet = files.ToHashSet();

        if (files.Count == 0) {
            return 0;
        }

        var collection = new AssetCollection();
        var options = SnuggleOptions.Default with { Game = flags.Game, Logger = new ConsoleLogger(), CacheDataIfLZMA = true };
        if (options.Game != UnityGame.Default &&
            !string.IsNullOrEmpty(flags.GameOptions)) {
            options.GameOptions.StorageMap[options.Game] = JsonSerializer.Deserialize<JsonElement>(File.Exists(flags.GameOptions) ? File.ReadAllText(flags.GameOptions) : flags.GameOptions, SnuggleOptions.JsonOptions);
        }

        options.GameOptions.Migrate();

        foreach (var file in fileSet) {
            Console.WriteLine($"Loading {file}");
            collection.LoadFile(file, SnuggleOptions.Default);
        }

        collection.CacheGameObjectClassIds();
        Console.WriteLine("Finding container paths...");
        collection.FindResources();
        AssetCollection.Collect();
        Console.WriteLine($"Memory Tension: {GC.GetTotalMemory(false).GetHumanReadableBytes()}");

        var processed = new HashSet<long>();

        foreach (var asset in collection.Files.SelectMany(x => x.Value.GetAllObjects())) {
            var passedFilter = !flags.PathIdFilters.Any() && !flags.NameFilters.Any() || flags.PathIdFilters.Contains(asset.PathId) || flags.NameFilters.Any(x => x.IsMatch(asset.ObjectComparableName));

            if (!passedFilter) {
                continue;
            }

            Console.WriteLine($"Processing {asset}");

            switch (asset) {
                case Texture2D texture when !flags.NoTexture && flags.LooseTextures:
                    ConvertCore.ConvertTexture(flags, texture);
                    break;
                case Mesh mesh when !flags.NoMesh && flags.LooseMeshes:
                    ConvertCore.ConvertMesh(flags, mesh);
                    break;
                case GameObject gameObject when !flags.NoSkinnedMesh:
                    ConvertCore.ConvertGameObject(flags, gameObject, processed);
                    break;
                case MeshRenderer renderer when !flags.NoMesh && renderer.GameObject.Value is not null:
                    ConvertCore.ConvertGameObject(flags, renderer.GameObject.Value, processed);
                    break;
                case SkinnedMeshRenderer renderer when !flags.NoSkinnedMesh && renderer.GameObject.Value is not null:
                    ConvertCore.ConvertGameObject(flags, renderer.GameObject.Value, processed);
                    break;
                case Material material when !flags.NoMaterials && flags.LooseMaterials:
                    ConvertCore.ConvertMaterial(flags, material);
                    break;
                case Text text when !flags.NoText:
                    ConvertCore.ConvertText(flags, text);
                    break;
            }
        }

        if (options.Game != UnityGame.Default &&
            options.GameOptions.StorageMap.ContainsKey(options.Game)) {
            Console.WriteLine("Game Settings:");
            var jsonOptions = new JsonSerializerOptions(SnuggleOptions.JsonOptions) { WriteIndented = false };
            Console.WriteLine(JsonSerializer.Serialize(options.GameOptions.StorageMap[options.Game], jsonOptions));
        }

        return 0;
    }
}
