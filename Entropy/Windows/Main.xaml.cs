using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Entropy.Handlers;

namespace Entropy.Windows; 

/// <summary>
///     Interaction logic for MainWindow.xaml
/// </summary>
public partial class Main {
    public Main() {
        InitializeComponent();
        EntropyCore.Instance.Dispatcher = Dispatcher.CurrentDispatcher;

        EntropyCore.Instance.PropertyChanged += (_, args) => {
            switch (args.PropertyName) {
                case nameof(EntropyCore.Filters):
                    SearchBox.Text = EntropyCore.Instance.Search;
                    break;
            }
        };
    }

    private void ClearMemory(object sender, CancelEventArgs e) {
        try {
            EntropyCore.Instance.Dispose();
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

            EntropyFile.LoadDirectoriesAndFiles(names);
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
            EntropyCore.Instance.Search = value;
            EntropyCore.Instance.OnPropertyChanged(nameof(EntropyCore.Filters)); // i know.
        }
    }
}
