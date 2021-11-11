using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using DragonLib;
using DragonLib.CLI;
using Snuggle.Core;
using Snuggle.Core.Logging;
using Snuggle.Core.Meta;
using Snuggle.Core.Options;

namespace Snuggle.Headless {
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

            foreach (var asset in collection.Files.SelectMany(x => x.Value.GetAllObjects())) {
                // TODO(yretenai) Textures
                // TODO(yretenai) Mesh
                // TODO(yretenai) Skinned Mesh
                // TODO(yretenai) Text
                // TODO(yretenai) Material
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
}
