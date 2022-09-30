using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using Ookii.Dialogs.Wpf;
using Serilog;
using Snuggle.Converters;
using Snuggle.Core;
using Snuggle.Core.Implementations;
using Snuggle.Core.Interfaces;
using Snuggle.Core.Models;
using Snuggle.Core.Options;

namespace Snuggle.Handlers;

public static class SnuggleFile {
    public static void LoadDirectories() {
        var selection = new VistaFolderBrowserDialog {
            Multiselect = true,
            UseDescriptionForTitle = true,
            Description = "Select folder to load",
            SelectedPath = Path.GetDirectoryName(SnuggleCore.Instance.Settings.RecentDirectories.LastOrDefault()),
            ShowNewFolderButton = false,
        };

        if (selection.ShowDialog() != true) {
            return;
        }

        LoadDirectoriesAndFiles(selection.SelectedPaths);
    }

    public static void LoadFiles() { 
        var selection = new VistaOpenFileDialog {
            Multiselect = true,
            InitialDirectory = Path.GetDirectoryName(SnuggleCore.Instance.Settings.RecentFiles.LastOrDefault()),
            Title = "Select files to load",
        };

        if (selection.ShowDialog() != true) {
            return;
        }

        LoadDirectoriesAndFiles(selection.FileNames.ToArray());
    }

    public static void LoadDirectoriesAndFiles(params string[] entries) {
        var instance = SnuggleCore.Instance;
        instance.WorkerAction(
            "LoadDirectoriesAndFiles",
            token => {
                var files = new List<string>();
                var recentFiles = SnuggleCore.Instance.Settings.RecentFiles;
                var recentDirectories = SnuggleCore.Instance.Settings.RecentDirectories;
                foreach (var entry in entries) {
                    if (Directory.Exists(entry)) {
                        files.AddRange(Directory.EnumerateFiles(entry, "*", SearchOption.AllDirectories));
                        recentDirectories.Add(Path.GetFullPath(entry));
                    } else if (File.Exists(entry)) {
                        files.Add(entry);
                        recentFiles.Add(Path.GetFullPath(entry));
                    }
                }

                SnuggleCore.Instance.Settings.RecentFiles = recentFiles.Distinct().TakeLast(5).ToList();
                SnuggleCore.Instance.Settings.RecentDirectories = recentDirectories.Distinct().TakeLast(5).ToList();
                SnuggleCore.Instance.SaveOptions();

                var fileSet = files.ToHashSet();
                instance.Status.SetProgressMax(fileSet.Count);
                foreach (var file in fileSet) {
                    if (token.IsCancellationRequested) {
                        return;
                    }

                    instance.Status.SetStatus($"Loading {file}");
                    Log.Information("Loading {Name}", Path.GetFileName(file));
                    instance.Status.SetProgress(instance.Status.Value + 1);
                    var ext = Path.GetExtension(file);
                    if (!ext.StartsWith(".split") || ext == ".split0") {
                        instance.Collection.LoadFile(file, instance.Settings.Options);
                    }
                }
            },
            false);

        instance.WorkerAction("CacheGameObjectClassIds",
            _ => {
                instance.Status.Reset();
                instance.Status.SetStatus("Caching GameObject ClassIds...");
                Log.Information("Caching GameObject ClassIds...");
                instance.Collection.CacheGameObjectClassIds();
            },
            true);

        instance.WorkerAction("Finalize",
            _ => {
                instance.Status.Reset();
                instance.Status.SetStatus("Caching GameObject ClassIds...");
                Log.Information("Caching GameObject ClassIds...");
                instance.Collection.CacheGameObjectClassIds();
                instance.Status.SetStatus("Finding container paths...");
                Log.Information("Finding container paths...");
                instance.Collection.FindResources();
                instance.Status.SetStatus("Building IL2CPP Data via Cpp2IL...");
                Log.Information("Building IL2CPP Data via CPP2IL...");
                instance.Collection.ProcessIL2CPP();
                instance.Status.SetStatus($"Loaded {instance.Collection.Files.Count} files");
                Log.Information("Loaded {Count} files", instance.Collection.Files.Count);
                instance.WorkerAction("Collect", _ => AssetCollection.Collect(), false);
                instance.OnPropertyChanged(nameof(SnuggleCore.Objects));
                instance.OnPropertyChanged(nameof(SnuggleCore.HasAssetsVisibility));
                instance.OnPropertyChanged(nameof(SnuggleCore.Filters));
                instance.OnPropertyChanged(nameof(SnuggleCore.Title));
            },
            true);
    }

    public static void Extract(ExtractMode mode, ExtractFilter filter) {
        var selection = new VistaFolderBrowserDialog {
            ShowNewFolderButton = true,
            Multiselect = false,
            SelectedPath = SnuggleCore.Instance.Settings.LastSaveDirectory,
            UseDescriptionForTitle = true,
            Description = "Select folder to save to",
        };

        if (selection.ShowDialog() != true) {
            return;
        }

        var outputDirectory = selection.SelectedPath;
        if (string.IsNullOrEmpty(outputDirectory)) {
            return;
        }

        var instance = SnuggleCore.Instance;
        instance.Settings.LastSaveDirectory = outputDirectory;
        instance.SaveOptions();
        var objects = filter switch {
            ExtractFilter.Selected => instance.SelectedObjects,
            ExtractFilter.All => instance.Objects,
            ExtractFilter.Filtered when string.IsNullOrWhiteSpace(instance.Search) && instance.Filters.Count == 0 && !instance.Settings.ExportOptions.OnlyWithCABPath => instance.Objects,
            ExtractFilter.Filtered => instance.Objects.Where(Filter).ToList(),
            _ => throw new ArgumentOutOfRangeException(nameof(filter), filter, null),
        };
        instance.WorkerAction("Extract", token => { ExtractOperation(objects, outputDirectory, mode, token); }, true);
    }

    public static bool Filter(SnuggleObject snuggleObject) {
        var search = SnuggleCore.Instance.Search;
        var filter = SnuggleCore.Instance.Filters;

        if (SnuggleCore.Instance.Settings.ExportOptions.OnlyWithCABPath && string.IsNullOrWhiteSpace(snuggleObject.Container)) {
            return false;
        }

        if (filter.Count > 0 && !filter.Contains(snuggleObject.ClassId)) {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(search)) {
            if (long.TryParse(search, NumberStyles.Number, null, out var numberSearch)) {
                if (snuggleObject.PathId == numberSearch) {
                    return true;
                }
            }

            return snuggleObject.Name.Contains(search, StringComparison.InvariantCultureIgnoreCase) ||
                   snuggleObject.Container.Contains(search, StringComparison.InvariantCultureIgnoreCase) ||
                   snuggleObject.SerializedName.Contains(search, StringComparison.InvariantCultureIgnoreCase);
        }

        return true;
    }

    private static void ExtractOperation(IReadOnlyCollection<SnuggleObject> items, string outputDirectory, ExtractMode mode, CancellationToken token) {
        SnuggleCore.Instance.Status.SetProgressMax(items.Count);
        SnuggleCore.Instance.Status.SetProgress(0);
        foreach (var SnuggleObject in items) {
            if (token.IsCancellationRequested) {
                break;
            }

            SnuggleCore.Instance.Status.SetProgress(SnuggleCore.Instance.Status.Value + 1);

            if (SnuggleCore.Instance.Status.Value % 1000 == 0) {
                SnuggleTextureFile.ClearMemory();
                SnuggleSpriteFile.ClearMemory();
            }

            var serializedObject = SnuggleObject.GetObject();
            if (mode == ExtractMode.Raw) {
                serializedObject ??= SnuggleObject.GetObject(true);
            }

            if (serializedObject == null) {
                continue;
            }

            var ext = mode switch {
                ExtractMode.Raw => "bytes",
                ExtractMode.Convert => DetermineExtension(serializedObject.ClassId),
                ExtractMode.Serialize => "data.json",
                _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null),
            };

            var path = PathFormatter.Format(SnuggleCore.Instance.Settings.ExportOptions.DecidePathTemplate(serializedObject), ext, serializedObject);
            Log.Information("Saving {PathId} {Name} - {Path}", serializedObject.PathId, serializedObject.SerializedFile.Name, Path.ChangeExtension(path, null));
            SnuggleCore.Instance.Status.SetStatus($"Saving {Path.ChangeExtension(path, null)}");
            var resultPath = Path.Combine(outputDirectory, path);
            var resultDir = Path.GetDirectoryName(resultPath) ?? "./";

            try {
                switch (mode) {
                    case ExtractMode.Raw:
                        ExtractRaw(serializedObject, resultDir, resultPath);
                        break;
                    case ExtractMode.Convert:
                        ExtractConvert(serializedObject, resultDir, resultPath);
                        break;
                    case ExtractMode.Serialize:
                        ExtractJson(serializedObject, resultDir, resultPath);
                        break;
                    default:
                        throw new NotSupportedException();
                }
            } catch (Exception e) {
                Log.Error(e, "Failure while extracting file");
            }
        }

        SnuggleCore.Instance.FreeMemory();
        SnuggleCore.Instance.Status.Reset();
    }

    private static string DetermineExtension(object boxedClassId) {
        if (boxedClassId is UnityClassId classId) {
            return classId switch {
                UnityClassId.GameObject => "gltf",
                UnityClassId.Mesh => "gltf",
                UnityClassId.MeshFilter => "gltf",
                UnityClassId.SkinnedMeshRenderer => "gltf",
                UnityClassId.Texture => "png",
                UnityClassId.Sprite => "png",
                UnityClassId.Material => "json",
                UnityClassId.TextAsset => "txt",
                _ => "bytes",
            };
        }

        return "bytes";
    }

    private static void ExtractConvert(ISerialized serializedObject, string resultDir, string resultPath) {
        serializedObject.Deserialize(SnuggleCore.Instance.Settings.ObjectOptions);

        var instance = SnuggleCore.Instance;

        switch (serializedObject) {
            case ITexture texture2d: {
                SnuggleTextureFile.Save(texture2d, resultPath, instance.Settings.ExportOptions, true);
                return;
            }
            case Material material: {
                SnuggleMaterialFile.Save(material, resultPath, false);
                return;
            }
            case Sprite sprite: {
                SnuggleSpriteFile.Save(sprite, resultPath, instance.Settings.ObjectOptions, instance.Settings.ExportOptions);
                return;
            }
            case Mesh mesh: {
                SnuggleMeshFile.Save(mesh, resultPath, instance.Settings.ObjectOptions, instance.Settings.ExportOptions, instance.Settings.MeshExportOptions);
                return;
            }
            case Component component: {
                if (component is not SkinnedMeshRenderer and not MeshFilter) {
                    return;
                }

                var gameObject = component.GameObject.Value;
                if (gameObject == null) {
                    return;
                }

                SnuggleMeshFile.Save(gameObject, resultPath, instance.Settings.ObjectOptions, instance.Settings.ExportOptions, instance.Settings.MeshExportOptions);
                return;
            }
            case Text text: {
                serializedObject.Deserialize(SnuggleCore.Instance.Settings.ObjectOptions);

                if (!Directory.Exists(resultDir)) {
                    Directory.CreateDirectory(resultDir);
                }

                var ext = ".txt";
                if (Path.HasExtension(text.ObjectContainerPath)) {
                    ext = Path.GetExtension(text.ObjectContainerPath);
                }

                resultPath = Path.ChangeExtension(resultPath, ext);
                File.WriteAllBytes(resultPath, text.String!.Value.ToArray());
                return;
            }
            case GameObject gameObject: {
                SnuggleMeshFile.Save(gameObject, resultPath, instance.Settings.ObjectOptions, instance.Settings.ExportOptions, instance.Settings.MeshExportOptions);
                return;
            }
            case AudioClip clip: {
                SnuggleAudioFile.Save(clip, resultPath, instance.Settings.ExportOptions);
                return;
            }
        }
    }

    private static void ExtractJson(ISerialized serializedObject, string resultDir, string resultPath) {
        serializedObject.Deserialize(SnuggleCore.Instance.Settings.ObjectOptions);

        var data = JsonSerializer.Serialize<object>(serializedObject, SnuggleCoreOptions.JsonOptions);
        if (!Directory.Exists(resultDir)) {
            Directory.CreateDirectory(resultDir);
        }

        resultPath = Path.ChangeExtension(resultPath, ".json");
        File.WriteAllText(resultPath, data, Encoding.UTF8);
    }

    private static void ExtractRaw(SerializedObject serializedObject, string resultDir, string resultPath) {
        using var dataStream = serializedObject.SerializedFile.OpenFile(serializedObject.Info);
        if (!Directory.Exists(resultDir)) {
            Directory.CreateDirectory(resultDir);
        }

        using var outputStream = File.OpenWrite(resultPath);
        outputStream.SetLength(0);
        dataStream.CopyTo(outputStream);

        if (serializedObject is ISerializedResource serializedResource && !serializedResource.StreamData.IsNull) {
            var resource = serializedResource.StreamData.GetData(SnuggleCore.Instance.Collection, SnuggleCore.Instance.Settings.ObjectOptions);
            resultPath = Path.ChangeExtension(resultPath, ".resS");
            using var outputResourceStream = File.OpenWrite(resultPath);
            outputResourceStream.SetLength(0);
            outputResourceStream.Write(resource.Span);
        }
    }
}
