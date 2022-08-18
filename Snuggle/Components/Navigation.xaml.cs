using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using AdonisUI;
using DragonLib.IO;
using Microsoft.WindowsAPICodePack.Dialogs;
using Snuggle.Core.Implementations;
using Snuggle.Core.Meta;
using Snuggle.Core.Models.Bundle;
using Snuggle.Core.Options;
using Snuggle.Handlers;
using Snuggle.Windows;

namespace Snuggle.Components;

public partial class Navigation {
    private static readonly Regex SplitPattern = new(@"(?<=[a-z])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])|(?<=[a-z])(?=[0-9])", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.CultureInvariant);
    private readonly Dictionary<RendererType, MenuItem> RendererTypeItems = new();
    private readonly Dictionary<UnityGame, MenuItem> UnityGameItems = new();

    public Navigation() {
        InitializeComponent();
        var instance = SnuggleCore.Instance;

        CacheData.IsChecked = instance.Settings.Options.CacheData;
        CacheDataIfLZMA.IsChecked = instance.Settings.Options.CacheDataIfLZMA;
        LightMode.IsChecked = instance.Settings.LightMode;

        BuildEnumMenu(UnityGameList, UnityGameItems, new[] { instance.Settings.Options.Game }, UpdateGame, CancelGameEvent);
        BuildEnumMenu(RendererTypes, RendererTypeItems, instance.Settings.MeshExportOptions.EnabledRenders, AddRenderer, RemoveRenderer);
        BuildSettingMenu(SerializationOptions, typeof(SnuggleExportOptions), nameof(SnuggleOptions.ExportOptions));
        BuildSettingMenu(SerializationOptions, typeof(ObjectDeserializationOptions), nameof(SnuggleOptions.ObjectOptions));
        BuildSettingMenu(RendererOptions, typeof(SnuggleMeshExportOptions), nameof(SnuggleOptions.MeshExportOptions));
        PopulateRecentItems();
        PopulateItemTypes();

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

    private static bool LockFilters { get; set; }

    private static void BuildSettingMenu(ItemsControl menu, Type type, string objectName) {
        var current = typeof(SnuggleOptions).GetProperty(objectName)!.GetValue(SnuggleCore.Instance.Settings)!;
        var i = 0;
        var descriptions = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
        var constructors = type.GetConstructors();
        foreach (var constructor in constructors) {
            var parameters = constructor.GetParameters();
            foreach (var parameter in parameters) {
                var description = parameter.GetCustomAttribute<DescriptionAttribute>()?.Description;
                if (string.IsNullOrWhiteSpace(description)) {
                    continue;
                }

                descriptions[parameter.Name!] = description;
            }
        }

        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance)) {
            if (property.PropertyType != typeof(bool) && property.PropertyType != typeof(string)) {
                continue;
            }

            if (!descriptions.TryGetValue(property.Name, out var description)) {
                description = property.GetCustomAttribute<DescriptionAttribute>()?.Description ?? string.Empty;
            }

            var item = new MenuItem {
                Tag = (type, objectName, property), Header = SplitName(property.Name), ToolTip = description, IsCheckable = property.PropertyType == typeof(bool),
            };

            if (item.IsCheckable) {
                item.IsChecked = property.GetValue(current) is true;
                item.Checked += UpdateToggle;
                item.Unchecked += UpdateToggle;
            } else {
                if (property.PropertyType == typeof(string)) {
                    item.Click += UpdateStringSetting;
                }
            }

            menu.Items.Insert(i++, item);
        }
    }

    private static void UpdateStringSetting(object sender, RoutedEventArgs e) {
        if (sender is not MenuItem item) {
            return;
        }

        var (type, objectName, targetProperty) = item.Tag is (Type, string, PropertyInfo) ? ((Type, string, PropertyInfo)) item.Tag : (null, null, null);
        var value = GetCurrentSetting(type, objectName, targetProperty);
        if (value is not string currentString) {
            currentString = string.Empty;
        }

        var dialog = new StringParamDialog(currentString, item.ToolTip as string ?? "");
        if (dialog.ShowDialog() == true) {
            UpdateSetting(type, objectName, targetProperty, dialog.Text);
        }
    }

    internal static string SplitName(string name) => string.Join(' ', SplitPattern.Split(name));

    private static void UpdateToggle(object sender, RoutedEventArgs args) {
        if (sender is not MenuItem item) {
            return;
        }

        var (type, objectName, targetProperty) = item.Tag is (Type, string, PropertyInfo) ? ((Type, string, PropertyInfo)) item.Tag : (null, null, null);

        UpdateSetting(type, objectName, targetProperty, item.IsChecked);
    }

    private static object? GetCurrentSetting(Type? type, string? objectName, PropertyInfo? targetProperty) {
        if (type == null || objectName == null || targetProperty == null) {
            return null;
        }

        var currentProperty = typeof(SnuggleOptions).GetProperty(objectName)!;
        var current = currentProperty.GetValue(SnuggleCore.Instance.Settings)!;
        return targetProperty.GetValue(current);
    }

    private static void UpdateSetting(Type? type, string? objectName, PropertyInfo? targetProperty, object value) {
        if (type == null || objectName == null || targetProperty == null) {
            return;
        }

        var propertyMap = new Dictionary<string, object?>(StringComparer.InvariantCultureIgnoreCase);
        var propertyInfoMap = new Dictionary<string, PropertyInfo>(StringComparer.InvariantCultureIgnoreCase);
        var currentProperty = typeof(SnuggleOptions).GetProperty(objectName)!;
        var current = currentProperty.GetValue(SnuggleCore.Instance.Settings)!;
        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance)) {
            propertyMap[property.Name] = property == targetProperty ? value : property.GetValue(current);
            propertyInfoMap[property.Name] = property;
        }

        var constructors = type.GetConstructors();
        foreach (var constructor in constructors) {
            var parameters = constructor.GetParameters().ToDictionary(x => x.Name!, y => y, StringComparer.InvariantCultureIgnoreCase);
            if (!parameters.Keys.All(propertyMap.ContainsKey)) {
                continue;
            }

            if (parameters.Keys.Any(x => !propertyMap.ContainsKey(x))) {
                continue;
            }

            var constructorParams = parameters.Select(x => propertyMap[x.Key]).ToArray();
            var newSettings = Activator.CreateInstance(type, constructorParams);
            foreach (var (properyKeyName, properyValue) in propertyMap) {
                if (!parameters.ContainsKey(properyKeyName)) {
                    propertyInfoMap[properyKeyName].SetValue(newSettings, properyValue);
                }
            }

            currentProperty.SetValue(SnuggleCore.Instance.Settings, newSettings);
            SnuggleCore.Instance.SaveOptions();
            SnuggleCore.Instance.OnPropertyChanged(objectName);
            SnuggleCore.Instance.OnPropertyChanged($"{objectName}.{targetProperty.Name}");
        }
    }

    private static void BuildEnumMenu<T>(ItemsControl menu, IDictionary<T, MenuItem> items, IReadOnlyCollection<T> currentValue, RoutedEventHandler @checked, RoutedEventHandler @unchecked) where T : struct, Enum {
        var descriptions = typeof(T).GetFields(BindingFlags.Static | BindingFlags.Public).ToDictionary(x => (T) x.GetValue(null)!, x => x.GetCustomAttribute<DescriptionAttribute>()?.Description.Split('\n').FirstOrDefault() ?? SplitName(x.Name));
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
        var letters = new Dictionary<char, MenuItem>();
        foreach (var item in instance.Objects.DistinctBy(x => x.ClassId).Select(x => x.ClassId).OrderBy(x => ((Enum) x).ToString("G"))) {
            var name = ((Enum) item).ToString("G");
            var menuItem = new MenuItem { Tag = item, Header = "_" + name, IsCheckable = true, IsChecked = instance.Filters.Contains(item) };
            menuItem.Click += ToggleFilter;
            var letter = char.ToUpper(name[0]);
            if (!letters.TryGetValue(letter, out var letterMenuItem)) {
                letterMenuItem = new MenuItem { Header = $"_{letter}" };
                letters[letter] = letterMenuItem;
                Filters.Items.Add(letterMenuItem);
            }

            letterMenuItem.Items.Add(menuItem);
        }

        if (Filters.Items.IsEmpty) {
            var reset = new MenuItem { Header = "Nothing to show" };
            Filters.Items.Add(reset);
        } else {
            var reset = new MenuItem { Header = "Reset Filters" };
            reset.Click += ResetFilter;
            Filters.Items.Add(new Separator());
            Filters.Items.Add(reset);
        }

        Filters.Visibility = Filters.Items.IsEmpty ? Visibility.Collapsed : Visibility.Visible;
    }

    private void PopulateRecentItems() {
        try {
            var instance = SnuggleCore.Instance;
            RecentItems.Items.Clear();
            foreach (var item in instance.Settings.RecentFiles.Select(recentFile => new MenuItem { IsEnabled = File.Exists(recentFile), Tag = recentFile, Header = "_" + recentFile })) {
                item.Click += LoadDirectoryOrFile;
                RecentItems.Items.Add(item);
            }

            if (!RecentItems.Items.IsEmpty && instance.Settings.RecentDirectories.Count > 0) {
                RecentItems.Items.Add(new Separator());
            }

            foreach (var item in instance.Settings.RecentDirectories.Select(recentDirectory => new MenuItem { IsEnabled = Directory.Exists(recentDirectory), Tag = recentDirectory, Header = "_" + recentDirectory })) {
                item.Click += LoadDirectoryOrFile;
                RecentItems.Items.Add(item);
            }

            if (RecentItems.Items.IsEmpty) {
                return;
            }

            RecentItems.Items.Add(new Separator());
            var clear = new MenuItem { Header = "Clear Recent Items" };
            clear.Click += ClearRecentValues;
            RecentItems.Items.Add(clear);
        } finally {
            RecentItems.Visibility = RecentItems.Items.IsEmpty ? Visibility.Collapsed : Visibility.Visible;
        }
    }

    private static void ClearRecentValues(object sender, RoutedEventArgs e) {
        SnuggleCore.Instance.SetOptions(SnuggleCore.Instance.Settings with { RecentFiles = new List<string>(), RecentDirectories = new List<string>() });
    }

    private static void ToggleFilter(object sender, RoutedEventArgs e) {
        if (LockFilters) {
            return;
        }

        var tag = ((FrameworkElement) sender).Tag;
        var instance = SnuggleCore.Instance;
        if (instance.Filters.Contains(tag)) {
            instance.Filters.Remove(tag);
        } else {
            instance.Filters.Add(tag);
        }

        instance.OnPropertyChanged(nameof(SnuggleCore.Filters));
    }

    public void ResetFilter(object sender, RoutedEventArgs e) {
        if (LockFilters) {
            return;
        }

        LockFilters = true;
        UncheckAllMenuItems(Filters.Items);
        LockFilters = false;

        var instance = SnuggleCore.Instance;
        instance.Filters.Clear();
        instance.OnPropertyChanged(nameof(SnuggleCore.Filters));
    }

    private static void UncheckAllMenuItems(IEnumerable collection) {
        foreach (var item in collection) {
            if (item is MenuItem menuItem) {
                if (menuItem.IsCheckable) {
                    menuItem.IsChecked = false;
                }

                if (menuItem.Items.Count > 0) {
                    UncheckAllMenuItems(menuItem.Items);
                }
            }
        }
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
        e.Handled = true;
    }

    private static void AddRenderer(object sender, RoutedEventArgs e) {
        if (sender is not MenuItem menuItem) {
            return;
        }

        var tag = (RendererType) menuItem.Tag;
        SnuggleCore.Instance.Settings.MeshExportOptions.EnabledRenders.Add(tag);
        SnuggleCore.Instance.SaveOptions();
        e.Handled = true;
    }

    private static void RemoveRenderer(object sender, RoutedEventArgs e) {
        if (sender is not MenuItem menuItem) {
            return;
        }

        var tag = (RendererType) menuItem.Tag;
        SnuggleCore.Instance.Settings.MeshExportOptions.EnabledRenders.Remove(tag);
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
        instance.WorkerAction("FreeMemory", _ => { SnuggleCore.Instance.FreeMemory(); }, true);
    }

    private void DumpGameObjectTree(object sender, RoutedEventArgs e) {
        using var selection = new CommonSaveFileDialog {
            DefaultFileName = "gametree.txt",
            Filters = { new CommonFileDialogFilter("JSON File", ".json") },
            InitialDirectory = SnuggleCore.Instance.Settings.LastSaveDirectory,
            Title = "Select file to save to",
            ShowPlacesList = true,
        };

        if (selection.ShowDialog() != CommonFileDialogResult.Ok) {
            return;
        }

        using var file = new FileStream(selection.FileName, FileMode.Create);
        using var writer = new StreamWriter(file);
        var nodes = BuildTreeNode(SnuggleCore.Instance.Collection.GameObjectTree);
        TreePrinter.PrintTree(writer, nodes);
    }

    private static List<TreePrinter.TreeNode> BuildTreeNode(IEnumerable<GameObject?> gameObjects) {
        return gameObjects.Where(x => x != null).Select(x => new TreePrinter.TreeNode(x!.Name, BuildTreeNode(x.Children.Select(y => y.Value)))).ToList();
    }

    private void ExtractRaw(object sender, RoutedEventArgs e) {
        var filter = (ExtractFilter) int.Parse((string) ((FrameworkElement) sender).Tag);
        SnuggleFile.Extract(ExtractMode.Raw, filter);
    }

    private void ExtractConvert(object sender, RoutedEventArgs e) {
        var filter = (ExtractFilter) int.Parse((string) ((FrameworkElement) sender).Tag);
        SnuggleFile.Extract(ExtractMode.Convert, filter);
    }

    private void ExtractSerialize(object sender, RoutedEventArgs e) {
        var filter = (ExtractFilter) int.Parse((string) ((FrameworkElement) sender).Tag);
        SnuggleFile.Extract(ExtractMode.Serialize, filter);
    }

    private void FreeTypes(object sender, RoutedEventArgs e) {
        var instance = SnuggleCore.Instance;
        instance.WorkerAction("FreeTypeMemory", _ => { SnuggleCore.Instance.Collection.ClearTypeTrees(); }, true);
    }

    private void RebuildAssets(object sender, RoutedEventArgs e) {
        if (sender is not MenuItem menuItem) {
            return;
        }

        if (!Enum.TryParse<UnityCompressionType>(menuItem.Tag as string, out var compression)) {
            return;
        }

        using var selection = new CommonOpenFileDialog {
            IsFolderPicker = true,
            Multiselect = false,
            AllowNonFileSystemItems = false,
            Title = "Select folder to save to",
            ShowPlacesList = true,
        };

        if (selection.ShowDialog() != CommonFileDialogResult.Ok) {
            return;
        }

        var instance = SnuggleCore.Instance;
        var path = selection.FileName;
        instance.WorkerAction(
            "RebuildAssets",
            token => {
                var settings = compression switch {
                    UnityCompressionType.None => BundleSerializationOptions.Default,
                    UnityCompressionType.LZMA => BundleSerializationOptions.LZMA,
                    UnityCompressionType.LZ4 => BundleSerializationOptions.LZ4,
                    UnityCompressionType.LZ4HC => BundleSerializationOptions.LZ4HC,
                    _ => throw new NotSupportedException(),
                };
                instance.Collection.RebuildBundles(path, settings, instance.Settings.Options, token);
            },
            false);
    }
}
