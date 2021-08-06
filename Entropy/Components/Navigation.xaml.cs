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

        private void CacheDataChecked(object sender, RoutedEventArgs e) {
            var instance = EntropyCore.Instance;
            instance.SetOptions(instance.Settings.Options with { CacheData = true });
        }

        private void CacheDataUnchecked(object sender, RoutedEventArgs e) {
            var instance = EntropyCore.Instance;
            instance.SetOptions(instance.Settings.Options with { CacheData = false });
        }

        private void CacheDataLZMAChecked(object sender, RoutedEventArgs e) {
            var instance = EntropyCore.Instance;
            instance.SetOptions(instance.Settings.Options with { CacheDataIfLZMA = true });
        }

        private void CacheDataLZMAUnchecked(object sender, RoutedEventArgs e) {
            var instance = EntropyCore.Instance;
            instance.SetOptions(instance.Settings.Options with { CacheDataIfLZMA = false });
        }

        private void WriteNativeTexturesChecked(object sender, RoutedEventArgs e) {
            var instance = EntropyCore.Instance;
            instance.SetOptions(instance.Settings with { WriteNativeTextures = true });
        }

        private void WriteNativeTexturesUnchecked(object sender, RoutedEventArgs e) {
            var instance = EntropyCore.Instance;
            instance.SetOptions(instance.Settings with { WriteNativeTextures = false });
        }

        private void UseContainerPathsChecked(object sender, RoutedEventArgs e) {
            var instance = EntropyCore.Instance;
            instance.SetOptions(instance.Settings with { UseContainerPaths = true });
        }

        private void UseContainerPathsUnchecked(object sender, RoutedEventArgs e) {
            var instance = EntropyCore.Instance;
            instance.SetOptions(instance.Settings with { UseContainerPaths = false });
        }

        private void GroupByTypeChecked(object sender, RoutedEventArgs e) {
            var instance = EntropyCore.Instance;
            instance.SetOptions(instance.Settings with { GroupByType = true });
        }

        private void GroupByTypeUnchecked(object sender, RoutedEventArgs e) {
            var instance = EntropyCore.Instance;
            instance.SetOptions(instance.Settings with { GroupByType = false });
        }

        private void LoadDirectory(object sender, RoutedEventArgs e) {
            EntropyFile.LoadDirectory();
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
