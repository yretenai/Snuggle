using System;
using System.IO;
using System.Linq;
using System.Windows;
using Entropy.ViewModels;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace Entropy.Components {
    public partial class Navigation {
        public Navigation() {
            InitializeComponent();
            CacheData.IsChecked = EntropyCore.Instance.Options.CacheData;
        }

        private void CacheDataChecked(object sender, RoutedEventArgs e) {
            var instance = EntropyCore.Instance;
            instance.SetOptions(instance.Options with { CacheData = true });
        }

        private void CacheDataUnchecked(object sender, RoutedEventArgs e) {
            var instance = EntropyCore.Instance;
            instance.SetOptions(instance.Options with { CacheData = false });
        }

        private void LoadDirectory(object sender, RoutedEventArgs e) {
            using var selection = new CommonOpenFileDialog {
                IsFolderPicker = true,
                Multiselect = true,
                AllowNonFileSystemItems = false,
                Title = "Select folder to load",
                ShowPlacesList = true,
            };

            if (selection.ShowDialog() != CommonFileDialogResult.Ok) {
                return;
            }

            var directories = selection.FileNames.ToArray();
            var instance = EntropyCore.Instance;
            instance.WorkerAction(() => {
                // TODO: Split files.
                var files = directories.SelectMany(x => Directory.EnumerateFiles(x, "*", SearchOption.AllDirectories)).ToArray();
                instance.Status.SetProgressMax(files.Length);
                foreach (var file in files) {
                    instance.Status.SetStatus($"Loading {file}");
                    instance.Status.SetProgress(instance.Status.Value + 1);
                    instance.Collection.LoadFile(file, instance.Options);
                }

                instance.Status.Reset();
                instance.Status.SetStatus($"Loaded {instance.Collection.Files.Count} files");
            });
        }

        private void LoadFiles(object sender, RoutedEventArgs e) {
            using var selection = new CommonOpenFileDialog {
                IsFolderPicker = true,
                Multiselect = true,
                AllowNonFileSystemItems = false,
                Title = "Select files to load",
                ShowPlacesList = true,
            };

            if (selection.ShowDialog() != CommonFileDialogResult.Ok) {
                return;
            }

            var files = selection.FileNames.ToArray();
            var instance = EntropyCore.Instance;
            instance.WorkerAction(() => {
                // TODO: Split files.
                instance.Status.SetProgressMax(files.Length);
                foreach (var file in files) {
                    instance.Status.SetStatus($"Loading {file}");
                    instance.Status.SetProgress(instance.Status.Value + 1);
                    instance.Collection.LoadFile(file, instance.Options);
                }

                instance.Status.Reset();
                instance.Status.SetStatus($"Loaded  {instance.Collection.Files.Count} files");
            });
        }

        private void ExitTrampoline(object sender, RoutedEventArgs e) {
            EntropyCore.Instance.Dispose();
        }

        private void ResetTrampoline(object sender, RoutedEventArgs e) {
            EntropyCore.Instance.Reset();
        }

        private void FreeMemory(object sender, RoutedEventArgs e) {
            var instance = EntropyCore.Instance;
            instance.WorkerAction(() => {
                foreach (var bundle in instance.Collection.Bundles) {
                    bundle.ClearCache();
                }

                foreach (var serializedObject in instance.Collection.Files.SelectMany(x => x.Value.Objects.Values)) {
                    serializedObject.Free();
                }

                GC.Collect();
            });
        }
    }
}
