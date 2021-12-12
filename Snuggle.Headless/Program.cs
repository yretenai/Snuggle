using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using DragonLib;
using DragonLib.CLI;
using DragonLib.IO;
using Snuggle.Core;
using Snuggle.Core.Implementations;
using Snuggle.Core.Interfaces;
using Snuggle.Core.Logging;
using Snuggle.Core.Meta;
using Snuggle.Core.Options;
using Snuggle.Headless.GameFlags;

namespace Snuggle.Headless;

public static class Program {
    public static int Main() {
        var additionalFlags = new Dictionary<UnityGame, Type>();
        foreach (var type in Assembly.GetExecutingAssembly().GetTypes().Where(x => x.GetCustomAttribute<GameFlagsAttribute>() != null && x.IsAssignableTo(typeof(IGameFlags)))) {
            var gameFlagsAttr = type.GetCustomAttribute<GameFlagsAttribute>();
            if (gameFlagsAttr == null) {
                continue;
            }

            additionalFlags[gameFlagsAttr.Game] = type;
        }

        var flags = CommandLineFlags.ParseFlags<SnuggleFlags>(
            (typeMap, helpInvoked) => {
                CommandLineFlags.PrintHelp(typeMap, helpInvoked);
                foreach (var (game, t) in additionalFlags) {
                    Logger.Info("FLAG", $"Help for UnityGame.{game:G}");
                    CommandLineFlags.PrintHelp(t, CommandLineFlags.PrintHelp, helpInvoked);
                }
            });
        if (flags == null) {
            return 1;
        }

        ILogger logger = ConsoleLogger.Instance;

        logger.Debug(flags.ToString());
        logger.Debug($"Args: {string.Join(' ', Environment.GetCommandLineArgs()[1..])}");
        IGameFlags? gameFlags = null;
        if (flags.Game != UnityGame.Default && additionalFlags.TryGetValue(flags.Game, out var additionalGameFlags)) {
            gameFlags = CommandLineFlags.ParseFlags(additionalGameFlags) as IGameFlags;
            if (gameFlags != null) {
                logger.Debug(gameFlags.ToString());
            }
        }

        var files = new List<string>();
        foreach (var entry in flags.Paths) {
            if (Directory.Exists(entry)) {
                var dir = Directory.GetFiles(entry, "*", flags.Recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
                logger.Info($"Found {dir.Length} files in {entry}");
                files.AddRange(dir);
            } else if (File.Exists(entry)) {
                logger.Info($"Found {entry}");
                files.Add(entry);
            } else {
                logger.Info($"Could not find {entry}");
            }
        }

        var fileSet = files.ToHashSet();
        logger.Info($"Found {files.Count} files.");

        if (files.Count == 0) {
            logger.Info("No files found, exiting...");
            return 0;
        }

        var collection = new AssetCollection();
        var options = SnuggleCoreOptions.Default with { Game = flags.Game, Logger = logger, CacheDataIfLZMA = true };
        if (options.Game != UnityGame.Default && gameFlags != default) {
            options.GameOptions.StorageMap[options.Game] = JsonSerializer.SerializeToElement(gameFlags.ToOptions(), SnuggleCoreOptions.JsonOptions);
        }

        options.GameOptions.Migrate();

        foreach (var file in fileSet) {
            collection.LoadFile(file, options);
        }

        collection.CacheGameObjectClassIds();
        logger.Info("Finding container paths...");
        collection.FindResources();
        logger.Info("Building GameObject Graph...");
        collection.BuildGraph();
        logger.Info("Collecting Memory...");
        AssetCollection.Collect();
        logger.Info($"Memory Tension: {GC.GetTotalMemory(false).GetHumanReadableBytes()}");

        foreach (var asset in collection.Files.SelectMany(x => x.Value.GetAllObjects())) {
            var passedFilter = !flags.PathIdFilters.Any() && !flags.NameFilters.Any() || flags.PathIdFilters.Contains(asset.PathId) || flags.NameFilters.Any(x => x.IsMatch(asset.ObjectComparableName));

            if (!passedFilter) {
                continue;
            }

            try {
                switch (asset) {
                    case Texture2D texture when flags.LooseTextures:
                        logger.Info($"Processing Texture {asset}");
                        ConvertCore.ConvertTexture(flags, logger, texture, true);
                        break;
                    case Mesh mesh when flags.LooseMeshes:
                        logger.Info($"Processing Mesh {asset}");
                        ConvertCore.ConvertMesh(flags, logger, mesh);
                        break;
                    case GameObject gameObject when !flags.NoGameObject:
                        logger.Info($"Processing GameObject {asset}");
                        ConvertCore.ConvertGameObject(flags, logger, gameObject);
                        break;
                    case MeshRenderer renderer when !flags.NoMesh && renderer.GameObject.Value is not null:
                        logger.Info($"Processing GameObject {renderer.GameObject.Value}");
                        ConvertCore.ConvertGameObject(flags, logger, renderer.GameObject.Value);
                        break;
                    case SkinnedMeshRenderer renderer when !flags.NoSkinnedMesh && renderer.GameObject.Value is not null:
                        logger.Info($"Processing GameObject {renderer.GameObject.Value}");
                        ConvertCore.ConvertGameObject(flags, logger, renderer.GameObject.Value);
                        break;
                    case Material material when flags.LooseMaterials:
                        logger.Info($"Processing Material {asset}");
                        ConvertCore.ConvertMaterial(flags, logger, material);
                        break;
                    case Text text when !flags.NoText:
                        logger.Info($"Processing Text {asset}");
                        ConvertCore.ConvertText(flags, logger, text);
                        break;
                    case Sprite sprite when !flags.NoSprite:
                        logger.Info($"Processing Sprite {asset}");
                        ConvertCore.ConvertSprite(flags, logger, sprite);
                        break;
                }
            } catch (Exception e) {
                logger.Error(e.Message, e);
            }

            if (flags.LowMemory) {
                ConvertCore.ClearMemory(collection);
            }
        }

        if (options.Game != UnityGame.Default && options.GameOptions.StorageMap.ContainsKey(options.Game)) {
            logger.Info("Updated Game Settings");
            var jsonOptions = new JsonSerializerOptions(SnuggleCoreOptions.JsonOptions) { WriteIndented = false };
            logger.Info(JsonSerializer.Serialize(options.GameOptions.StorageMap[options.Game], jsonOptions));
        }

        return 0;
    }
}
