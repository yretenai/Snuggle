using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
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
                case nameof(SnuggleCore.Filters): {
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
                    direction = LastDirection == ListSortDirection.Ascending
                        ? ListSortDirection.Descending
                        : ListSortDirection.Ascending;
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

    private static bool Filter(object o) {
        if (o is not SnuggleObject SnuggleObject) {
            return false;
        }

        if (SnuggleCore.Instance.Filters.Count > 0 &&
            !SnuggleCore.Instance.Filters.Contains(SnuggleObject.ClassId)) {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(SnuggleCore.Instance.Search)) {
            var value = SnuggleCore.Instance.Search;
            if (!(SnuggleObject.PathId.ToString().Contains(value, StringComparison.InvariantCultureIgnoreCase) ||
                  SnuggleObject.Name.Contains(value, StringComparison.InvariantCultureIgnoreCase) ||
                  SnuggleObject.Container.Contains(value, StringComparison.InvariantCultureIgnoreCase) ||
                  SnuggleObject.SerializedName.Contains(value, StringComparison.InvariantCultureIgnoreCase) ||
                  SnuggleObject.ClassId.ToString()?.Contains(value, StringComparison.InvariantCultureIgnoreCase) == true)) {
                return false;
            }
        }

        return true;
    }

    private void UpdateSelected(object sender, RoutedEventArgs e) {
        var list = (ListView) sender;
        e.Handled = true;
        var selectedItems = list.SelectedItems;
        if (selectedItems.Count == 0) {
            return;
        }

        SnuggleCore.Instance.SelectedObject = (SnuggleObject) selectedItems[^1]!;
        var serializedObject = SnuggleCore.Instance.SelectedObject.GetObject();
        if (serializedObject is { ShouldDeserialize: true }) {
            serializedObject.Deserialize(SnuggleCore.Instance.Settings.ObjectOptions);
        }

        SnuggleCore.Instance.OnPropertyChanged(nameof(SnuggleCore.SelectedObject));
        SnuggleCore.Instance.SelectedObjects = selectedItems.Cast<SnuggleObject>().ToList();
        SnuggleCore.Instance.OnPropertyChanged(nameof(SnuggleCore.SelectedObjects));
    }
}
