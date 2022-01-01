using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Snuggle.Core;
using Snuggle.Core.Options;
using Snuggle.Handlers;

namespace Snuggle.Components;

public partial class Assets {
    public Assets() {
        InitializeComponent();
        SnuggleCore.Instance.PropertyChanged += (_, args) => {
            switch (args.PropertyName) {
                case nameof(SnuggleCore.Objects): {
                    LastHeaderClicked = null;
                    LastDirection = ListSortDirection.Ascending;
                    var dataView = CollectionViewSource.GetDefaultView(Entries.ItemsSource);
                    using var defer = dataView.DeferRefresh();
                    dataView.SortDescriptions.Clear();
                    break;
                }
                case nameof(SnuggleCore.Filters):
                case $"{nameof(SnuggleOptions.ExportOptions)}.{nameof(SnuggleExportOptions.OnlyWithCABPath)}": {
                    var dataView = CollectionViewSource.GetDefaultView(Entries.ItemsSource);
                    using var defer = dataView.DeferRefresh();
                    dataView.Filter = Filter;
                    break;
                }
            }
        };
    }

    private GridViewColumnHeader? LastHeaderClicked { get; set; }
    private ListSortDirection LastDirection { get; set; } = ListSortDirection.Ascending;

    // https://docs.microsoft.com/en-us/dotnet/desktop/wpf/controls/how-to-sort-a-gridview-column-when-a-header-is-clicked?view=netframeworkdesktop-4.8
    private void SortColumn(object sender, RoutedEventArgs e) {
        if (e.OriginalSource is GridViewColumnHeader headerClicked) {
            if (headerClicked.Role != GridViewColumnHeaderRole.Padding) {
                ListSortDirection direction;
                if (headerClicked != LastHeaderClicked) {
                    direction = ListSortDirection.Ascending;
                } else {
                    direction = LastDirection == ListSortDirection.Ascending ? ListSortDirection.Descending : ListSortDirection.Ascending;
                }

                var columnBinding = headerClicked.Column.DisplayMemberBinding as Binding;
                var sortBy = columnBinding?.Path.Path ?? headerClicked.Column.Header;
                if (sortBy == null) {
                    return;
                }

                Sort((string) sortBy, direction);
                LastHeaderClicked = headerClicked;
                LastDirection = direction;
            }
        }
    }

    private void Sort(string sortBy, ListSortDirection direction) {
        var dataView = CollectionViewSource.GetDefaultView(Entries.ItemsSource);
        using var defer = dataView.DeferRefresh();
        dataView.SortDescriptions.Clear();
        var sd = new SortDescription(sortBy, direction);
        dataView.SortDescriptions.Add(sd);
    }

    private static bool Filter(object o) => o is SnuggleObject snuggleObject && SnuggleFile.Filter(snuggleObject);

    private void UpdateSelected(object sender, RoutedEventArgs e) {
        var list = (ListView) sender;
        e.Handled = true;
        var selectedItems = list.SelectedItems;
        if (selectedItems.Count == 0) {
            return;
        }

        SnuggleCore.Instance.SelectedObject = (SnuggleObject) selectedItems[^1]!;
        var serializedObject = SnuggleCore.Instance.SelectedObject.GetObject();
        try {
            serializedObject?.Deserialize(SnuggleCore.Instance.Settings.ObjectOptions);
        } catch (Exception ex) {
            SnuggleCore.Instance.LogTarget.Error($"Failed to deserialize {serializedObject?.PathId}", ex);
            SnuggleCore.Instance.SelectedObject = null;
        }

        SnuggleCore.Instance.OnPropertyChanged(nameof(SnuggleCore.SelectedObject));
        SnuggleCore.Instance.SelectedObjects = selectedItems.Cast<SnuggleObject>().ToList();
        SnuggleCore.Instance.OnPropertyChanged(nameof(SnuggleCore.SelectedObjects));
    }

    private void ExtractRaw(object sender, RoutedEventArgs e) {
        SnuggleFile.Extract(ExtractMode.Raw, ExtractFilter.Selected);
    }

    private void ExtractConvert(object sender, RoutedEventArgs e) {
        SnuggleFile.Extract(ExtractMode.Convert, ExtractFilter.Selected);
    }

    private void ExtractSerialize(object sender, RoutedEventArgs e) {
        SnuggleFile.Extract(ExtractMode.Serialize, ExtractFilter.Selected);
    }

    private void Resolve(object sender, RoutedEventArgs e) {
        if (sender is not FrameworkElement element) {
            return;
        }

        if (element.DataContext is not SnuggleObject snuggleObject) {
            return;
        }

        var tag = snuggleObject.GetObject()?.SerializedFile.Tag;
        if (tag == null) {
            return;
        }

        var path = Utils.GetStringFromTag(tag);
        try {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) {
                return;
            }

            var dir = Path.GetDirectoryName(Path.GetFullPath(path))!;
            if (Environment.OSVersion.Platform == PlatformID.Win32NT) {
                Process.Start("explorer.exe", $"/e, /select, \"{Path.GetFullPath(path)}\"");
            } else { // maybe?
                Process.Start(new ProcessStartInfo { FileName = dir, Verb = "Open", UseShellExecute = true });
            }
        } catch {
            // return
        }
    }
}
