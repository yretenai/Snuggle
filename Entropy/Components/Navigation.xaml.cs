using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Entropy.Handlers;
using Entropy.Windows;
using Equilibrium.Meta;

namespace Entropy.Components {
    public partial class Navigation {
        private readonly Dictionary<UnityGame, MenuItem> UnityGameItems = new();

        public Navigation() {
            InitializeComponent();
            var instance = EntropyCore.Instance;

            CacheData.IsChecked = instance.Settings.Options.CacheData;
            CacheDataIfLZMA.IsChecked = instance.Settings.Options.CacheDataIfLZMA;
            WriteNativeTextures.IsChecked = instance.Settings.WriteNativeTextures;
            UseContainerPaths.IsChecked = instance.Settings.UseContainerPaths;
            GroupByType.IsChecked = instance.Settings.GroupByType;

            var descriptions = typeof(UnityGame).GetFields(BindingFlags.Static | BindingFlags.Public).ToDictionary(x => (UnityGame) x.GetValue(null)!, x => x.GetCustomAttribute<DescriptionAttribute>()?.Description ?? x.Name);
            foreach (var game in Enum.GetValues<UnityGame>()) {
                var item = new MenuItem { Tag = game, Header = "_" + descriptions[game], IsChecked = instance.Settings.Options.Game == game, IsCheckable = true };
                item.Checked += UpdateGame;
                item.Unchecked += CancelEvent;
                UnityGameList.Items.Add(item);
                UnityGameItems[game] = item;
            }

            PopulateRecentItems();

            instance.PropertyChanged += (_, args) => {
                switch (args.PropertyName) {
                    case nameof(EntropyCore.Settings):
                        PopulateRecentItems();
                        break;
                    case nameof(EntropyCore.Objects):
                        PopulateItemTypes();
                        break;
                    case nameof(EntropyCore.Filters):
                        SearchBox.Text = EntropyCore.Instance.Search;
                        break;
                }
            };
        }

        private void PopulateItemTypes() {
            var instance = EntropyCore.Instance;
            Filters.Items.Clear();
            foreach (var item in instance.Objects.DistinctBy(x => x.ClassId).Select(x => x.ClassId).OrderBy(x => ((Enum) x).ToString("G"))) {
                var menuItem = new MenuItem { Tag = item, Header = "_" + ((Enum) item).ToString("G"), IsCheckable = true, IsChecked = instance.Filters.Contains(item) };
                menuItem.Click += ToggleFilter;
                Filters.Items.Add(menuItem);
            }
        }

        private void PopulateRecentItems() {
            var instance = EntropyCore.Instance;
            RecentItems.Items.Clear();
            foreach (var item in instance.Settings.RecentFiles.Select(recentFile =>
                new MenuItem { Tag = recentFile, Header = "_" + recentFile })) {
                item.Click += LoadDirectoryOrFile;
                RecentItems.Items.Add(item);
            }

            if (!RecentItems.Items.IsEmpty &&
                instance.Settings.RecentDirectories.Count > 0) {
                RecentItems.Items.Add(new Separator());
            }

            foreach (var item in instance.Settings.RecentDirectories.Select(recentDirectory =>
                new MenuItem { Tag = recentDirectory, Header = "_" + recentDirectory })) {
                item.Click += LoadDirectoryOrFile;
                RecentItems.Items.Add(item);
            }

            RecentItems.Visibility = RecentItems.Items.IsEmpty ? Visibility.Collapsed : Visibility.Visible;
        }

        private static void ToggleFilter(object sender, RoutedEventArgs e) {
            var tag = ((FrameworkElement) sender).Tag;
            var instance = EntropyCore.Instance;
            if (instance.Filters.Contains(tag)) {
                instance.Filters.Remove(tag);
            } else {
                instance.Filters.Add(tag);
            }

            instance.OnPropertyChanged(nameof(EntropyCore.Filters));
        }

        private static void LoadDirectoryOrFile(object sender, RoutedEventArgs e) {
            if (sender is not MenuItem { Tag: string directory }) {
                return;
            }

            EntropyFile.LoadDirectoriesAndFiles(directory);
        }

        private static void CancelEvent(object sender, RoutedEventArgs e) {
            if (sender is not MenuItem menuItem) {
                return;
            }

            if ((UnityGame) menuItem.Tag == EntropyCore.Instance.Settings.Options.Game) {
                menuItem.IsChecked = true;
            }

            e.Handled = true;
        }

        private void UpdateGame(object sender, RoutedEventArgs e) {
            if (sender is not MenuItem menuItem) {
                return;
            }

            var game = EntropyCore.Instance.Settings.Options.Game;
            var tag = (UnityGame) menuItem.Tag;
            if (tag == game) {
                return;
            }

            EntropyCore.Instance.SetOptions(EntropyCore.Instance.Settings.Options with { Game = tag });
            UnityGameItems[game].IsChecked = false;
            e.Handled = true;
        }

        private void ToggleCacheData(object sender, RoutedEventArgs e) {
            var enabled = ((MenuItem) sender).IsChecked;
            var instance = EntropyCore.Instance;
            instance.SetOptions(instance.Settings.Options with { CacheData = enabled });
        }

        private void ToggleCacheDataLZMA(object sender, RoutedEventArgs e) {
            var enabled = ((MenuItem) sender).IsChecked;
            var instance = EntropyCore.Instance;
            instance.SetOptions(instance.Settings.Options with { CacheDataIfLZMA = enabled });
        }

        private void ToggleWriteNativeTextures(object sender, RoutedEventArgs e) {
            var enabled = ((MenuItem) sender).IsChecked;
            var instance = EntropyCore.Instance;
            instance.SetOptions(instance.Settings with { WriteNativeTextures = enabled });
        }

        private void ToggleUseContainerPaths(object sender, RoutedEventArgs e) {
            var enabled = ((MenuItem) sender).IsChecked;
            var instance = EntropyCore.Instance;
            instance.SetOptions(instance.Settings with { UseContainerPaths = enabled });
        }

        private void ToggleGroupByType(object sender, RoutedEventArgs e) {
            var enabled = ((MenuItem) sender).IsChecked;
            var instance = EntropyCore.Instance;
            instance.SetOptions(instance.Settings with { GroupByType = enabled });
        }

        private void LoadDirectories(object sender, RoutedEventArgs e) {
            EntropyFile.LoadDirectories();
        }

        private void LoadFiles(object sender, RoutedEventArgs e) {
            EntropyFile.LoadFiles();
        }

        private void ExitTrampoline(object sender, RoutedEventArgs e) {
            Application.Current.MainWindow?.Close();
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

                foreach (var (_, file) in instance.Collection.Files) {
                    file.Free();
                }

                GC.Collect();
            });
        }

        private void Search(object sender, KeyEventArgs e) {
            if (e.Key == Key.Return) {
                e.Handled = true;

                var value = SearchBox.Text;
                EntropyCore.Instance.Search = value;
                EntropyCore.Instance.OnPropertyChanged(nameof(EntropyCore.Filters)); // i know.
            }
        }

        private void OpenLog(object sender, RoutedEventArgs e) {
            App.OpenWindow<DebugLog>();
        }

        private void ExtractRaw(object sender, RoutedEventArgs e) {
            EntropyFile.Extract(ExtractMode.Raw, (sender as MenuItem)?.Tag == null);
        }

        private void ExtractConvert(object sender, RoutedEventArgs e) {
            EntropyFile.Extract(ExtractMode.Convert, (sender as MenuItem)?.Tag == null);
        }

        private void ExtractSerialize(object sender, RoutedEventArgs e) {
            EntropyFile.Extract(ExtractMode.Serialize, (sender as MenuItem)?.Tag == null);
        }
    }
}
