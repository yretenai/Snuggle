using System;
using System.ComponentModel;
using Entropy.ViewModels;

namespace Entropy.Windows {
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class Main {
        public Main() {
            InitializeComponent();
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
    }
}
