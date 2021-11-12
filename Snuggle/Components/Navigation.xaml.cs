using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using AdonisUI;
using Snuggle.Converters;
using Snuggle.Core;
using Snuggle.Core.Meta;
using Snuggle.Core.Options;
using Snuggle.Handlers;
using Snuggle.Windows;

namespace Snuggle.Components;

public partial class Navigation {
    private readonly Dictionary<UniteVersion, MenuItem> PokemonUniteVersionItems = new();
    private readonly Dictionary<RendererType, MenuItem> RendererTypeItems = new();
    private readonly Dictionary<UnityGame, MenuItem> UnityGameItems = new();

    public Navigation() {
        InitializeComponent();
        var instance = SnuggleCore.Instance;

        CacheData.IsChecked = instance.Settings.Options.CacheData;
        CacheDataIfLZMA.IsChecked = instance.Settings.Options.CacheDataIfLZMA;
        WriteNativeTextures.IsChecked = instance.Settings.WriteNativeTextures;
        UseContainerPaths.IsChecked = instance.Settings.UseContainerPaths;
        GroupByType.IsChecked = instance.Settings.GroupByType;
        LightMode.IsChecked = instance.Settings.LightMode;
        BubbleGameObjectsDown.IsChecked = instance.Settings.BubbleGameObjectsDown;
        BubbleGameObjectsUp.IsChecked = instance.Settings.BubbleGameObjectsUp;
        DisplayRelationshipLines.IsChecked = instance.Settings.DisplayRelationshipLines;

        BuildEnumMenu(UnityGameList, UnityGameItems, new[] { instance.Settings.Options.Game }, UpdateGame, CancelGameEvent);
        BuildEnumMenu(RendererTypes, RendererTypeItems, instance.Settings.EnabledRenders, AddRenderer, RemoveRenderer);
        PopulateGameOptions();
        PopulateRecentItems();

        instance.PropertyChanged += (_, args) => {
            switch (args.PropertyName) {
                case nameof(SnuggleCore.Settings):
                    PopulateRecentItems();
                    break;
                case nameof(SnuggleCore.Objects):
                    PopulateItemTypes();
                    break;
            }
        };
    }

    private void PopulateGameOptions() {
        var selected = SnuggleCore.Instance.Settings.Options.Game;

        GameOptions.Visibility = Visibility.Collapsed;

        if (selected == UnityGame.PokemonUnite) {
            SetPokemonUniteOptionValues();
        } else {
            GameOptions.Items.Clear();
        }
    }

    private void SetPokemonUniteOptionValues() {
        GameOptions.Visibility = Visibility.Visible;
        GameOptions.Header = UnityGameItems[UnityGame.PokemonUnite].Header + " Options";

        var instance = SnuggleCore.Instance;
        if (!instance.Settings.Options.GameOptions.TryGetOptionsObject<UniteOptions>(UnityGame.PokemonUnite, out var uniteOptions)) {
            uniteOptions = UniteOptions.Default;
        }

        var optionsMenuItem = new MenuItem { Tag = "PokemonUnite_Version", Header = "_Version" };
        GameOptions.Items.Add(optionsMenuItem);
        BuildEnumMenu(optionsMenuItem, PokemonUniteVersionItems, new[] { uniteOptions.GameVersion }, UpdatePokemonUniteVersion, CancelPokemonUniteVersionEvent);
    }

    private static void BuildEnumMenu<T>(ItemsControl menu, IDictionary<T, MenuItem> items, IReadOnlyCollection<T> currentValue, RoutedEventHandler @checked, RoutedEventHandler @unchecked) where T : struct, Enum {
        var descriptions = typeof(T).GetFields(BindingFlags.Static | BindingFlags.Public).ToDictionary(x => (T) x.GetValue(null)!, x => x.GetCustomAttribute<DescriptionAttribute>()?.Description ?? x.Name);
        foreach (var value in Enum.GetValues<T>()) {
            var item = new MenuItem {
                Tag = value, Header = "_" + descriptions[value], IsChecked = currentValue.Any(x => x.Equals(value)), IsCheckable = true,
            };
            item.Checked += @checked;
            item.Unchecked += @unchecked;
            menu.Items.Add(item);
            items[value] = item;
        }
    }

    private void PopulateItemTypes() {
        var instance = SnuggleCore.Instance;
        Filters.Items.Clear();
        foreach (var item in instance.Objects.DistinctBy(x => x.ClassId).Select(x => x.ClassId).OrderBy(x => ((Enum) x).ToString("G"))) {
            var menuItem = new MenuItem { Tag = item, Header = "_" + ((Enum) item).ToString("G"), IsCheckable = true, IsChecked = instance.Filters.Contains(item) };
            menuItem.Click += ToggleFilter;
            Filters.Items.Add(menuItem);
        }

        Filters.Visibility = Filters.Items.IsEmpty ? Visibility.Collapsed : Visibility.Visible;
    }

    private void PopulateRecentItems() {
        var instance = SnuggleCore.Instance;
        RecentItems.Items.Clear();
        foreach (var item in instance.Settings.RecentFiles.Select(recentFile => new MenuItem { Tag = recentFile, Header = "_" + recentFile })) {
            item.Click += LoadDirectoryOrFile;
            RecentItems.Items.Add(item);
        }

        if (!RecentItems.Items.IsEmpty && instance.Settings.RecentDirectories.Count > 0) {
            RecentItems.Items.Add(new Separator());
        }

        foreach (var item in instance.Settings.RecentDirectories.Select(recentDirectory => new MenuItem { Tag = recentDirectory, Header = "_" + recentDirectory })) {
            item.Click += LoadDirectoryOrFile;
            RecentItems.Items.Add(item);
        }

        RecentItems.Visibility = RecentItems.Items.IsEmpty ? Visibility.Collapsed : Visibility.Visible;
    }

    private static void ToggleFilter(object sender, RoutedEventArgs e) {
        var tag = ((FrameworkElement) sender).Tag;
        var instance = SnuggleCore.Instance;
        if (instance.Filters.Contains(tag)) {
            instance.Filters.Remove(tag);
        } else {
            instance.Filters.Add(tag);
        }

        instance.OnPropertyChanged(nameof(SnuggleCore.Filters));
    }

    private static void LoadDirectoryOrFile(object sender, RoutedEventArgs e) {
        if (sender is not MenuItem { Tag: string directory }) {
            return;
        }

        SnuggleFile.LoadDirectoriesAndFiles(directory);
    }

    private static void CancelGameEvent(object sender, RoutedEventArgs e) {
        if (sender is not MenuItem menuItem) {
            return;
        }

        if ((UnityGame) menuItem.Tag == SnuggleCore.Instance.Settings.Options.Game) {
            menuItem.IsChecked = true;
        }

        e.Handled = true;
    }

    private void UpdateGame(object sender, RoutedEventArgs e) {
        if (sender is not MenuItem menuItem) {
            return;
        }

        var game = SnuggleCore.Instance.Settings.Options.Game;
        var tag = (UnityGame) menuItem.Tag;
        if (tag == game) {
            return;
        }

        SnuggleCore.Instance.SetOptions(SnuggleCore.Instance.Settings.Options with { Game = tag });
        UnityGameItems[game].IsChecked = false;
        PopulateGameOptions();
        e.Handled = true;
    }

    private void AddRenderer(object sender, RoutedEventArgs e) {
        if (sender is not MenuItem menuItem) {
            return;
        }

        var tag = (RendererType) menuItem.Tag;
        SnuggleCore.Instance.Settings.EnabledRenders.Add(tag);
        SnuggleCore.Instance.SaveOptions();
        e.Handled = true;
    }

    private void RemoveRenderer(object sender, RoutedEventArgs e) {
        if (sender is not MenuItem menuItem) {
            return;
        }

        var tag = (RendererType) menuItem.Tag;
        SnuggleCore.Instance.Settings.EnabledRenders.Remove(tag);
        SnuggleCore.Instance.SaveOptions();
        e.Handled = true;
    }

    private void ToggleCacheData(object sender, RoutedEventArgs e) {
        var enabled = ((MenuItem) sender).IsChecked;
        var instance = SnuggleCore.Instance;
        instance.SetOptions(instance.Settings.Options with { CacheData = enabled });
    }

    private void ToggleCacheDataLZMA(object sender, RoutedEventArgs e) {
        var enabled = ((MenuItem) sender).IsChecked;
        var instance = SnuggleCore.Instance;
        instance.SetOptions(instance.Settings.Options with { CacheDataIfLZMA = enabled });
    }

    private void ToggleWriteNativeTextures(object sender, RoutedEventArgs e) {
        var enabled = ((MenuItem) sender).IsChecked;
        var instance = SnuggleCore.Instance;
        instance.SetOptions(instance.Settings with { WriteNativeTextures = enabled });
    }

    private void ToggleUseContainerPaths(object sender, RoutedEventArgs e) {
        var enabled = ((MenuItem) sender).IsChecked;
        var instance = SnuggleCore.Instance;
        instance.SetOptions(instance.Settings with { UseContainerPaths = enabled });
    }

    private void ToggleGroupByType(object sender, RoutedEventArgs e) {
        var enabled = ((MenuItem) sender).IsChecked;
        var instance = SnuggleCore.Instance;
        instance.SetOptions(instance.Settings with { GroupByType = enabled });
    }

    private void ToggleBubbleGameObjectDown(object sender, RoutedEventArgs e) {
        var enabled = ((MenuItem) sender).IsChecked;
        var instance = SnuggleCore.Instance;
        instance.SetOptions(instance.Settings with { BubbleGameObjectsDown = enabled });
    }

    private void ToggleDisplayRelationshipLines(object sender, RoutedEventArgs e) {
        var enabled = ((MenuItem) sender).IsChecked;
        var instance = SnuggleCore.Instance;
        instance.SetOptions(instance.Settings with { DisplayRelationshipLines = enabled });
    }

    private void ToggleBubbleGameObjectUp(object sender, RoutedEventArgs e) {
        var enabled = ((MenuItem) sender).IsChecked;
        var instance = SnuggleCore.Instance;
        instance.SetOptions(instance.Settings with { BubbleGameObjectsUp = enabled });
    }

    private void ToggleLightMode(object sender, RoutedEventArgs e) {
        var enabled = ((MenuItem) sender).IsChecked;
        var instance = SnuggleCore.Instance;
        instance.SetOptions(instance.Settings with { LightMode = enabled });
        ResourceLocator.SetColorScheme(Application.Current.Resources, instance.Settings.LightMode ? ResourceLocator.LightColorScheme : ResourceLocator.DarkColorScheme);
    }

    private void LoadDirectories(object sender, RoutedEventArgs e) {
        SnuggleFile.LoadDirectories();
    }

    private void LoadFiles(object sender, RoutedEventArgs e) {
        SnuggleFile.LoadFiles();
    }

    private void ExitTrampoline(object sender, RoutedEventArgs e) {
        Application.Current.MainWindow?.Close();
    }

    private void ResetTrampoline(object sender, RoutedEventArgs e) {
        SnuggleCore.Instance.Reset();
    }

    private void FreeMemory(object sender, RoutedEventArgs e) {
        var instance = SnuggleCore.Instance;
        instance.WorkerAction(
            "FreeMemory",
            _ => {
                foreach (var bundle in instance.Collection.Bundles) {
                    bundle.ClearCache();
                }

                foreach (var (_, file) in instance.Collection.Files) {
                    file.Free();
                }

                SnuggleTextureFile.ClearMemory();

                AssetCollection.Collect();
            },
            true);
    }

    private void OpenGameObjectTree(object sender, RoutedEventArgs e) {
        App.OpenWindow<GameObjectTree>();
    }

    private void ExtractRaw(object sender, RoutedEventArgs e) {
        SnuggleFile.Extract(ExtractMode.Raw, (sender as MenuItem)?.Tag == null);
    }

    private void ExtractConvert(object sender, RoutedEventArgs e) {
        SnuggleFile.Extract(ExtractMode.Convert, (sender as MenuItem)?.Tag == null);
    }

    private void ExtractSerialize(object sender, RoutedEventArgs e) {
        SnuggleFile.Extract(ExtractMode.Serialize, (sender as MenuItem)?.Tag == null);
    }

    private void UpdatePokemonUniteVersion(object sender, RoutedEventArgs e) {
        var version = (UniteVersion) ((MenuItem) sender).Tag;
        var instance = SnuggleCore.Instance;
        if (!instance.Settings.Options.GameOptions.TryGetOptionsObject<UniteOptions>(UnityGame.PokemonUnite, out var uniteOptions)) {
            uniteOptions = UniteOptions.Default;
        }

        instance.SetOptions(UnityGame.PokemonUnite, uniteOptions with { GameVersion = version });

        foreach (var (itemVersion, item) in PokemonUniteVersionItems) {
            if (itemVersion == version) {
                continue;
            }

            item.IsChecked = false;
        }

        e.Handled = true;
    }

    private void CancelPokemonUniteVersionEvent(object sender, RoutedEventArgs e) {
        if (sender is not MenuItem menuItem) {
            return;
        }

        var instance = SnuggleCore.Instance;
        if (!instance.Settings.Options.GameOptions.TryGetOptionsObject<UniteOptions>(UnityGame.PokemonUnite, out var uniteOptions)) {
            uniteOptions = UniteOptions.Default;
        }

        if (menuItem.Tag?.Equals(uniteOptions.Version) == true) {
            menuItem.IsChecked = true;
        }

        e.Handled = true;
    }
}
