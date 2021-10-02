using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Entropy.Handlers;

namespace Entropy.Components {
    public partial class Assets {
        public Assets() {
            InitializeComponent();
            EntropyCore.Instance.PropertyChanged += (_, args) => {
                switch (args.PropertyName) {
                    case nameof(EntropyCore.Objects): {
                        LastHeaderClicked = null;
                        LastDirection = ListSortDirection.Ascending;
                        var dataView = CollectionViewSource.GetDefaultView(Entries.ItemsSource);
                        using var defer = dataView.DeferRefresh();
                        dataView.SortDescriptions.Clear();
                        break;
                    }
                    case nameof(EntropyCore.Filters): {
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
            if (o is not EntropyObject entropyObject) {
                return false;
            }

            if (EntropyCore.Instance.Filters.Count > 0 &&
                !EntropyCore.Instance.Filters.Contains(entropyObject.ClassId)) {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(EntropyCore.Instance.Search)) {
                var value = EntropyCore.Instance.Search;
                if (!(entropyObject.PathId.ToString().Contains(value, StringComparison.InvariantCultureIgnoreCase) ||
                      entropyObject.Name.Contains(value, StringComparison.InvariantCultureIgnoreCase) ||
                      entropyObject.Container.Contains(value, StringComparison.InvariantCultureIgnoreCase) ||
                      entropyObject.SerializedName.Contains(value, StringComparison.InvariantCultureIgnoreCase) ||
                      entropyObject.ClassId.ToString()?.Contains(value, StringComparison.InvariantCultureIgnoreCase) == true)) {
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

            EntropyCore.Instance.SelectedObject = (EntropyObject) selectedItems[^1]!;
            var serializedObject = EntropyCore.Instance.SelectedObject.GetObject();
            if (serializedObject is { ShouldDeserialize: true }) {
                serializedObject.Deserialize(EntropyCore.Instance.Settings.ObjectOptions);
            }
            EntropyCore.Instance.OnPropertyChanged(nameof(EntropyCore.SelectedObject));
            EntropyCore.Instance.SelectedObjects = selectedItems.Cast<EntropyObject>().ToList();
            EntropyCore.Instance.OnPropertyChanged(nameof(EntropyCore.SelectedObjects));
        }
    }
}
