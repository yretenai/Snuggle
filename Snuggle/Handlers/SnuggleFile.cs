using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using Microsoft.WindowsAPICodePack.Dialogs;
using Snuggle.Converters;
using Snuggle.Core;
using Snuggle.Core.Implementations;
using Snuggle.Core.Interfaces;
using Snuggle.Core.Options;

namespace Snuggle.Handlers;

public static class SnuggleFile {
    public static void LoadDirectories() {
        using var selection = new CommonOpenFileDialog {
            IsFolderPicker = true,
            Multiselect = true,
            AllowNonFileSystemItems = false,
            Title = "Select folder to load",
            InitialDirectory = Path.GetDirectoryName(SnuggleCore.Instance.Settings.RecentDirectories.LastOrDefault()),
            ShowPlacesList = true,
        };

        if (selection.ShowDialog() != CommonFileDialogResult.Ok) {
            return;
        }

        LoadDirectoriesAndFiles(selection.FileNames.ToArray());
    }

    public static void LoadFiles() {
        using var selection = new CommonOpenFileDialog {
            IsFolderPicker = false,
            Multiselect = true,
            AllowNonFileSystemItems = false,
            InitialDirectory = Path.GetDirectoryName(SnuggleCore.Instance.Settings.RecentFiles.LastOrDefault()),
            Title = "Select files to load",
            ShowPlacesList = true,
        };

        if (selection.ShowDialog() != CommonFileDialogResult.Ok) {
            return;
        }

        LoadDirectoriesAndFiles(selection.FileNames.ToArray());
    }

    public static void LoadDirectoriesAndFiles(params string[] entries) {
        var instance = SnuggleCore.Instance;
        instance.WorkerAction(
            "LoadDirectoriesAndFiles",
            token => {
                // TODO: Split files.
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
                    instance.LogTarget.Info($"Loading {Path.GetFileName(file)}");
                    instance.Status.SetProgress(instance.Status.Value + 1);
                    instance.Collection.LoadFile(file, instance.Settings.Options);
                }

                instance.Collection.CacheGameObjectClassIds();
                instance.Status.Reset();
                instance.Status.SetStatus("Finding container paths...");
                instance.LogTarget.Info("Finding container paths...");
                instance.Collection.FindResources();
                instance.Status.SetStatus("Building GameObject Graph...");
                instance.LogTarget.Info("Building GameObject Graph...");
                instance.Collection.BuildGraph();
                instance.Status.SetStatus($"Loaded {instance.Collection.Files.Count} files");
                instance.LogTarget.Info($"Loaded {instance.Collection.Files.Count} files");
                instance.WorkerAction("Collect", _ => AssetCollection.Collect(), false);
                instance.OnPropertyChanged(nameof(SnuggleCore.Objects));
                instance.OnPropertyChanged(nameof(SnuggleCore.HasAssetsVisibility));
                instance.OnPropertyChanged(nameof(SnuggleCore.Filters));
                instance.OnPropertyChanged(nameof(SnuggleCore.Title));
            },
            false);
    }

    public static void Extract(ExtractMode mode, ExtractFilter filter) {
        using var selection = new CommonOpenFileDialog {
            IsFolderPicker = true,
            Multiselect = false,
            AllowNonFileSystemItems = false,
            InitialDirectory = SnuggleCore.Instance.Settings.LastSaveDirectory,
            Title = "Select folder to save to",
            ShowPlacesList = true,
        };

        if (selection.ShowDialog() != CommonFileDialogResult.Ok) {
            return;
        }

        var outputDirectory = selection.FileNames.First();
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
            return snuggleObject.PathId.ToString().Contains(search, StringComparison.InvariantCultureIgnoreCase) || snuggleObject.Name.Contains(search, StringComparison.InvariantCultureIgnoreCase) || snuggleObject.Container.Contains(search, StringComparison.InvariantCultureIgnoreCase) || snuggleObject.SerializedName.Contains(search, StringComparison.InvariantCultureIgnoreCase) || snuggleObject.ClassId.ToString()?.Contains(search, StringComparison.InvariantCultureIgnoreCase) == true;
        }

        return true;
    }

    private static void ExtractOperation(IReadOnlyList<SnuggleObject> items, string outputDirectory, ExtractMode mode, CancellationToken token) {
        foreach (var SnuggleObject in items) {
            if (token.IsCancellationRequested) {
                break;
            }

            var serializedObject = SnuggleObject.GetObject();
            if (mode == ExtractMode.Raw) {
                serializedObject ??= SnuggleObject.GetObject(true);
            }

            if (serializedObject == null) {
                continue;
            }

            var resultPath = Path.Combine(outputDirectory, PathFormatter.Format(SnuggleCore.Instance.Settings.ExportOptions.PathTemplate, "bytes", serializedObject));
            var resultDir = Path.GetDirectoryName(resultPath) ?? "./";

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
        }
    }

    private static void ExtractConvert(SerializedObject serializedObject, string resultDir, string resultPath) {
        serializedObject.Deserialize(SnuggleCore.Instance.Settings.ObjectOptions);

        var instance = SnuggleCore.Instance;

        switch (serializedObject) {
            case Texture2D texture2d: {
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
        }
    }

    private static void ExtractJson(SerializedObject serializedObject, string resultDir, string resultPath) {
        serializedObject.Deserialize(SnuggleCore.Instance.Settings.ObjectOptions);

        var data = JsonSerializer.Serialize<object>(serializedObject, SnuggleCoreOptions.JsonOptions);
        if (!Directory.Exists(resultDir)) {
            Directory.CreateDirectory(resultDir);
        }

        resultPath = Path.ChangeExtension(resultPath, ".json");
        File.WriteAllText(resultPath, data, Encoding.UTF8);
    }

    private static void ExtractRaw(SerializedObject serializedObject, string resultDir, string resultPath) {
        using var dataStream = serializedObject.SerializedFile.OpenFile(serializedObject.PathId);
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
