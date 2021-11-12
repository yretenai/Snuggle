using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Snuggle.Handlers;

namespace Snuggle.Windows;

/// <summary>
///     Interaction logic for MainWindow.xaml
/// </summary>
public partial class Main {
    public Main() {
        InitializeComponent();
        SnuggleCore.Instance.Dispatcher = Dispatcher.CurrentDispatcher;

        SnuggleCore.Instance.PropertyChanged += (_, args) => {
            switch (args.PropertyName) {
                case nameof(SnuggleCore.Filters):
                    SearchBox.Text = SnuggleCore.Instance.Search;
                    break;
            }
        };
    }

    private void ClearMemory(object sender, CancelEventArgs e) {
        try {
            SnuggleCore.Instance.Dispose();
        } catch {
            // ignored.
        }

        Environment.Exit(0);
    }

    private void Exit(object? sender, EventArgs e) {
        Environment.Exit(0);
    }

    private void DropFile(object sender, DragEventArgs e) {
        if (!e.Effects.HasFlag(DragDropEffects.Copy)) {
            return;
        }

        if (!e.Data.GetDataPresent("FileDrop")) {
            return;
        }

        try {
            if (e.Data.GetData("FileDrop") is not string[] names ||
                names.Length == 0) {
                return;
            }

            SnuggleFile.LoadDirectoriesAndFiles(names);
            e.Handled = true;
        } catch {
            // ignored
        }
    }

    private void TestDrag(object sender, GiveFeedbackEventArgs e) {
        e.UseDefaultCursors = !e.Effects.HasFlag(DragDropEffects.Copy);
        e.Handled = true;
    }

    private void Search(object sender, KeyEventArgs e) {
        if (e.Key == Key.Return) {
            e.Handled = true;

            var value = SearchBox.Text;
            SnuggleCore.Instance.Search = value;
            SnuggleCore.Instance.OnPropertyChanged(nameof(SnuggleCore.Filters)); // i know.
        }
    }
}
