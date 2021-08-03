using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Entropy.ViewModels;
using Equilibrium.Meta;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace Entropy.Components {
    public partial class Navigation {
        private readonly Dictionary<UnityGame, MenuItem> UnityGameItems = new();

        public Navigation() {
            InitializeComponent();
            var instance = EntropyCore.Instance;
            CacheData.IsChecked = instance.Options.CacheData;
            CacheDataIfLZMA.IsChecked = instance.Options.CacheDataIfLZMA;
            var descriptions = typeof(UnityGame).GetFields(BindingFlags.Static | BindingFlags.Public).ToDictionary(x => (UnityGame) x.GetValue(null)!, x => x.GetCustomAttribute<DescriptionAttribute>()?.Description ?? x.Name);
            foreach (var game in Enum.GetValues<UnityGame>()) {
                var item = new MenuItem { Tag = game, Header = descriptions[game], IsChecked = instance.Options.Game == game, IsCheckable = true };
                item.Checked += UpdateGame;
                item.Unchecked += CancelEvent;
                UnityGameList.Items.Add(item);
                UnityGameItems[game] = item;
            }
        }

        private static void CancelEvent(object sender, RoutedEventArgs e) {
            if (sender is not MenuItem menuItem) {
                return;
            }

            if ((UnityGame) menuItem.Tag == EntropyCore.Instance.Options.Game) {
                menuItem.IsChecked = true;
            }

            e.Handled = true;
        }

        private void UpdateGame(object sender, RoutedEventArgs e) {
            if (sender is not MenuItem menuItem) {
                return;
            }

            var game = EntropyCore.Instance.Options.Game;
            if ((UnityGame) menuItem.Tag == game) {
                return;
            }

            EntropyCore.Instance.SetOptions(EntropyCore.Instance.Options with { Game = (UnityGame) menuItem.Tag });
            UnityGameItems[game].IsChecked = false;
            e.Handled = true;
        }

        private void CacheDataChecked(object sender, RoutedEventArgs e) {
            var instance = EntropyCore.Instance;
            instance.SetOptions(instance.Options with { CacheData = true });
        }

        private void CacheDataUnchecked(object sender, RoutedEventArgs e) {
            var instance = EntropyCore.Instance;
            instance.SetOptions(instance.Options with { CacheData = false });
        }

        private void CacheDataLZMAChecked(object sender, RoutedEventArgs e) {
            var instance = EntropyCore.Instance;
            instance.SetOptions(instance.Options with { CacheDataIfLZMA = true });
        }

        private void CacheDataLZMAUnchecked(object sender, RoutedEventArgs e) {
            var instance = EntropyCore.Instance;
            instance.SetOptions(instance.Options with { CacheDataIfLZMA = false });
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
            instance.WorkerAction(token => {
                // TODO: Split files.
                var files = directories.SelectMany(x => Directory.EnumerateFiles(x, "*", SearchOption.AllDirectories)).ToArray();
                instance.Status.SetProgressMax(files.Length);
                foreach (var file in files) {
                    if (token.IsCancellationRequested) {
                        return;
                    }

                    instance.Status.SetStatus($"Loading {file}");
                    instance.Status.SetProgress(instance.Status.Value + 1);
                    instance.Collection.LoadFile(file, instance.Options);
                }

                instance.Status.Reset();
                instance.Status.SetStatus("Finding container paths...");
                instance.Collection.FindAssetContainerNames();
                instance.Status.SetStatus($"Loaded {instance.Collection.Files.Count} files");
                instance.OnPropertyChanged(nameof(EntropyCore.Objects));
            });
        }

        private void LoadFiles(object sender, RoutedEventArgs e) {
            using var selection = new CommonOpenFileDialog {
                IsFolderPicker = false,
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
            instance.WorkerAction(token => {
                // TODO: Split files.
                instance.Status.SetProgressMax(files.Length);
                foreach (var file in files) {
                    if (token.IsCancellationRequested) {
                        return;
                    }

                    instance.Status.SetStatus($"Loading {file}");
                    instance.Status.SetProgress(instance.Status.Value + 1);
                    instance.Collection.LoadFile(file, instance.Options);
                }

                instance.Status.Reset();
                instance.Status.SetStatus("Finding container paths...");
                instance.Collection.FindAssetContainerNames();
                instance.Status.SetStatus($"Loaded  {instance.Collection.Files.Count} files");
                instance.OnPropertyChanged(nameof(EntropyCore.Objects));
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
            instance.WorkerAction(_ => {
                foreach (var bundle in instance.Collection.Bundles) {
                    bundle.ClearCache();
                }

                foreach (var serializedObject in instance.Collection.Files.SelectMany(x => x.Value.Objects.Values)) {
                    serializedObject.Free();
                }

                GC.Collect();
            });
        }

        private void Search(object sender, KeyEventArgs e) {
            if (e.Key == Key.Return) {
                e.Handled = true;

                var value = ((TextBox) sender).Text;
                EntropyCore.Instance.Search = value;
                EntropyCore.Instance.OnPropertyChanged(nameof(EntropyCore.Search));
            }
        }
    }
}
