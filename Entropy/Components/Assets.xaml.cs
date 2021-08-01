using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Entropy.ViewModels;

namespace Entropy.Components {
    public partial class Assets {
        public Assets() {
            InitializeComponent();
            EntropyCore.Instance.PropertyChanged += (_, args) => {
                switch (args.PropertyName) {
                    case nameof(EntropyCore.Collection): {
                        LastHeaderClicked = null;
                        LastDirection = ListSortDirection.Ascending;
                        var dataView = CollectionViewSource.GetDefaultView(Entries.ItemsSource);
                        dataView.SortDescriptions.Clear();
                        dataView.Refresh();
                        break;
                    }
                    case nameof(EntropyCore.Search):
                        Search(EntropyCore.Instance.Search);
                        break;
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
            dataView.SortDescriptions.Clear();
            var sd = new SortDescription(sortBy, direction);
            dataView.SortDescriptions.Add(sd);
            dataView.Refresh();
        }

        private void Search(string? value) {
            var dataView = CollectionViewSource.GetDefaultView(Entries.ItemsSource);
            if (string.IsNullOrEmpty(value)) {
                dataView.Filter = null;
                return;
            }

            dataView.Filter = o => {
                if (o is not EntropyObject entropyObject) {
                    return false;
                }

                return entropyObject.PathId.ToString().Contains(value, StringComparison.InvariantCultureIgnoreCase) ||
                       entropyObject.Name.Contains(value, StringComparison.InvariantCultureIgnoreCase) ||
                       entropyObject.Container.Contains(value, StringComparison.InvariantCultureIgnoreCase) ||
                       entropyObject.SerializedName.Contains(value, StringComparison.InvariantCultureIgnoreCase) ||
                       entropyObject.ClassId.ToString()?.Contains(value, StringComparison.InvariantCultureIgnoreCase) == true;
            };
        }

        private void UpdateSelected(object sender, RoutedEventArgs e) {
            var list = (ListView) sender;
            e.Handled = true;
            var selectedItems = list.SelectedItems;
            if (selectedItems.Count == 0) {
                return;
            }

            var selectedItem = (EntropyObject) selectedItems[^1]!;
            if (EntropyCore.Instance.Collection.Files.TryGetValue(selectedItem.SerializedName, out var serializedFile)) {
                if (serializedFile.Objects.TryGetValue(selectedItem.PathId, out var serializedObject)) {
                    EntropyCore.Instance.SelectedObject = serializedObject;
                    EntropyCore.Instance.OnPropertyChanged(nameof(EntropyCore.SelectedObject));
                }
            }
        }
    }
}
