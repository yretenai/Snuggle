using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using DragonLib;
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

        var directories = selection.FileNames.ToArray();
        var recent = SnuggleCore.Instance.Settings.RecentDirectories;
        recent.AddRange(directories);
        SnuggleCore.Instance.Settings.RecentDirectories = recent.Distinct().TakeLast(5).ToList();
        SnuggleCore.Instance.SaveOptions();
        LoadDirectoriesAndFiles(directories);
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

        var files = selection.FileNames.ToArray();
        var recent = SnuggleCore.Instance.Settings.RecentFiles;
        recent.AddRange(files);
        SnuggleCore.Instance.Settings.RecentFiles = recent.Distinct().TakeLast(5).ToList();
        SnuggleCore.Instance.SaveOptions();
        LoadDirectoriesAndFiles(files);
    }

    public static void LoadDirectoriesAndFiles(params string[] entries) {
        var instance = SnuggleCore.Instance;
        instance.WorkerAction(
            "LoadDirectoriesAndFiles",
            token => {
                // TODO: Split files.
                var files = new List<string>();
                foreach (var entry in entries) {
                    if (Directory.Exists(entry)) {
                        files.AddRange(Directory.EnumerateFiles(entry, "*", SearchOption.AllDirectories));
                    } else if (File.Exists(entry)) {
                        files.Add(entry);
                    }
                }

                var fileSet = files.ToHashSet();
                instance.Status.SetProgressMax(fileSet.Count);
                foreach (var file in fileSet) {
                    if (token.IsCancellationRequested) {
                        return;
                    }

                    instance.Status.SetStatus($"Loading {file}");
                    instance.Status.SetProgress(instance.Status.Value + 1);
                    instance.Collection.LoadFile(file, instance.Settings.Options);
                }

                instance.Collection.CacheGameObjectClassIds();
                instance.Status.Reset();
                instance.Status.SetStatus("Finding container paths...");
                instance.Collection.FindResources();
                instance.Status.SetStatus($"Loaded {instance.Collection.Files.Count} files");
                instance.WorkerAction("Collect", _ => AssetCollection.Collect(), false);
                instance.OnPropertyChanged(nameof(SnuggleCore.Objects));
                instance.OnPropertyChanged(nameof(SnuggleCore.HasAssetsVisibility));
                instance.OnPropertyChanged(nameof(SnuggleCore.Filters));
                instance.OnPropertyChanged(nameof(SnuggleCore.Title));
            },
            false);
    }

    public static void Extract(ExtractMode mode, bool selected) {
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
        instance.WorkerAction("Extract", token => { ExtractOperation((selected ? instance.SelectedObjects : instance.Objects).ToImmutableArray(), outputDirectory, mode, token); }, true);
    }

    private static void ExtractOperation(ImmutableArray<SnuggleObject> items, string outputDirectory, ExtractMode mode, CancellationToken token) {
        foreach (var SnuggleObject in items) {
            if (token.IsCancellationRequested) {
                break;
            }

            var serializedObject = SnuggleObject.GetObject();
            if (serializedObject == null) {
                continue;
            }

            var resultPath = GetResultPath(outputDirectory, serializedObject);
            var resultDir = Path.GetDirectoryName(resultPath) ?? "./";

            switch (mode) {
                case ExtractMode.Raw:
                    ExtractRaw(serializedObject, resultDir, resultPath);
                    break;
                case ExtractMode.Convert:
                    ExtractConvert(serializedObject, resultDir, resultPath, outputDirectory);
                    break;
                case ExtractMode.Serialize:
                    ExtractJson(serializedObject, resultDir, resultPath);
                    break;
                default:
                    throw new NotSupportedException();
            }
        }
    }

    private static void ExtractConvert(SerializedObject serializedObject, string resultDir, string resultPath, string outputDirectory) {
        serializedObject.Deserialize(SnuggleCore.Instance.Settings.ObjectOptions);

        var instance = SnuggleCore.Instance;

        switch (serializedObject) {
            case Texture2D texture2d: {
                SnuggleTextureFile.Save(texture2d, resultPath, SnuggleCore.Instance.Settings.WriteNativeTextures);
                return;
            }
            case Mesh mesh: {
                SnuggleMeshFile.Save(mesh, resultPath, new SnuggleMeshFile.SnuggleMeshFileOptions(instance.Settings.ObjectOptions, instance.Settings.BubbleGameObjectsDown, instance.Settings.BubbleGameObjectsUp, instance.Settings.WriteNativeTextures, true, true));
                return;
            }
            case Component component: {
                var gameObject = component.GameObject.Value;
                if (gameObject == null) {
                    return;
                }

                SnuggleMeshFile.Save(gameObject, GetResultPath(outputDirectory, gameObject), new SnuggleMeshFile.SnuggleMeshFileOptions(instance.Settings.ObjectOptions, instance.Settings.BubbleGameObjectsDown, instance.Settings.BubbleGameObjectsUp, instance.Settings.WriteNativeTextures, true, true));
                return;
            }
            case GameObject gameObject: {
                SnuggleMeshFile.Save(gameObject, GetResultPath(outputDirectory, gameObject), new SnuggleMeshFile.SnuggleMeshFileOptions(instance.Settings.ObjectOptions, instance.Settings.BubbleGameObjectsDown, instance.Settings.BubbleGameObjectsUp, instance.Settings.WriteNativeTextures, true, true));
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

    public static string GetResultPath(string outputDirectory, SerializedObject serializedObject) {
        var path = outputDirectory;
        if (SnuggleCore.Instance.Settings.GroupByType) {
            path = Path.Combine(path, ((Enum) serializedObject.ClassId).ToString("G"));
        }

        if (SnuggleCore.Instance.Settings.UseContainerPaths && !string.IsNullOrWhiteSpace(serializedObject.ObjectContainerPath)) {
            path = Path.Combine(path, "./" + serializedObject.ObjectContainerPath.SanitizeDirname());
        }

        path = Path.Combine(path, string.Format(SnuggleCore.Instance.Settings.NameTemplate, serializedObject, serializedObject.PathId, serializedObject.ClassId));

        return path;
    }
}
