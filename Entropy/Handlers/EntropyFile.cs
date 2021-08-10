using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using DragonLib;
using Equilibrium.Implementations;
using Equilibrium.Interfaces;
using Equilibrium.Options;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace Entropy.Handlers {
    public static class EntropyFile {
        public static void LoadDirectories() {
            using var selection = new CommonOpenFileDialog {
                IsFolderPicker = true,
                Multiselect = true,
                AllowNonFileSystemItems = false,
                Title = "Select folder to load",
                InitialDirectory = Path.GetDirectoryName(EntropyCore.Instance.Settings.RecentDirectories.LastOrDefault()),
                ShowPlacesList = true,
            };

            if (selection.ShowDialog() != CommonFileDialogResult.Ok) {
                return;
            }

            var directories = selection.FileNames.ToArray();
            var recent = EntropyCore.Instance.Settings.RecentDirectories;
            recent.AddRange(directories);
            EntropyCore.Instance.Settings.RecentDirectories = recent.Distinct().TakeLast(5).ToList();
            EntropyCore.Instance.SaveOptions();
            LoadDirectoriesAndFiles(directories);
        }

        public static void LoadFiles() {
            using var selection = new CommonOpenFileDialog {
                IsFolderPicker = false,
                Multiselect = true,
                AllowNonFileSystemItems = false,
                InitialDirectory = Path.GetDirectoryName(EntropyCore.Instance.Settings.RecentFiles.LastOrDefault()),
                Title = "Select files to load",
                ShowPlacesList = true,
            };

            if (selection.ShowDialog() != CommonFileDialogResult.Ok) {
                return;
            }

            var files = selection.FileNames.ToArray();
            var recent = EntropyCore.Instance.Settings.RecentFiles;
            recent.AddRange(files);
            EntropyCore.Instance.Settings.RecentFiles = recent.Distinct().TakeLast(5).ToList();
            EntropyCore.Instance.SaveOptions();
            LoadDirectoriesAndFiles(files);
        }

        public static void LoadDirectoriesAndFiles(params string[] entries) {
            var instance = EntropyCore.Instance;
            instance.WorkerAction("LoadDirectoriesAndFiles",
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

                    var fileSet = files.ToImmutableHashSet();
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
                    instance.Collection.FindAssetContainerNames();
                    instance.Status.SetStatus($"Loaded {instance.Collection.Files.Count} files");
                    instance.OnPropertyChanged(nameof(EntropyCore.Objects));
                    instance.OnPropertyChanged(nameof(EntropyCore.Filters));
                });
        }

        public static void Extract(ExtractMode mode, bool selected) {
            using var selection = new CommonOpenFileDialog {
                IsFolderPicker = true,
                Multiselect = false,
                AllowNonFileSystemItems = false,
                InitialDirectory = EntropyCore.Instance.Settings.LastSaveDirectory,
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

            var instance = EntropyCore.Instance;
            instance.Settings.LastSaveDirectory = outputDirectory;
            instance.SaveOptions();
            instance.WorkerAction("Extract", token => { ExtractOperation((selected ? instance.SelectedObjects : instance.Objects).ToImmutableArray(), outputDirectory, mode, token); });
        }

        private static void ExtractOperation(ImmutableArray<EntropyObject> items, string outputDirectory, ExtractMode mode, CancellationToken token) {
            var instance = EntropyCore.Instance;
            foreach (var entropyObject in items) {
                if (token.IsCancellationRequested) {
                    break;
                }

                var serializedObject = entropyObject.GetObject();
                if (serializedObject == null) {
                    continue;
                }

                var resultPath = GetResultPath(outputDirectory, serializedObject);
                resultPath = Path.Combine(resultPath, string.Format(instance.Settings.NameTemplate, serializedObject, serializedObject.PathId, serializedObject.ClassId));
                var resultDir = Path.GetDirectoryName(resultPath) ?? "./";

                switch (mode) {
                    case ExtractMode.Raw: {
                        ExtractRaw(serializedObject, resultDir, resultPath);
                        break;
                    }
                    case ExtractMode.Convert:
                        throw new NotImplementedException();
                    case ExtractMode.Serialize: {
                        ExtractJson(serializedObject, resultDir, resultPath);
                        break;
                    }
                    default:
                        throw new NotSupportedException();
                }
            }
        }

        private static void ExtractJson(SerializedObject serializedObject, string resultDir, string resultPath) {
            if (serializedObject.ShouldDeserialize) {
                serializedObject.Deserialize(EntropyCore.Instance.Settings.ObjectOptions);
            }

            var data = JsonSerializer.Serialize<object>(serializedObject, EquilibriumOptions.JsonOptions);
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

            if (serializedObject is ISerializedResource serializedResource &&
                !serializedResource.StreamData.IsNull) {
                var resource = serializedResource.StreamData.GetData(EntropyCore.Instance.Collection, EntropyCore.Instance.Settings.ObjectOptions);
                resultPath = Path.ChangeExtension(resultPath, ".resS");
                using var outputResourceStream = File.OpenWrite(resultPath);
                outputResourceStream.SetLength(0);
                outputResourceStream.Write(resource.Span);
            }
        }

        private static string GetResultPath(string outputDirectory, SerializedObject serializedObject) {
            var path = outputDirectory;
            if (EntropyCore.Instance.Settings.GroupByType) {
                path = Path.Combine(path, ((Enum) serializedObject.ClassId).ToString("G"));
            }

            if (EntropyCore.Instance.Settings.UseContainerPaths &&
                !string.IsNullOrWhiteSpace(serializedObject.ObjectContainerPath)) {
                path = Path.Combine(path, "./" + serializedObject.ObjectContainerPath.SanitizeDirname());
            }

            return path;
        }
    }
}
