using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using Equilibrium.Interfaces;
using Equilibrium.Meta;
using JetBrains.Annotations;

namespace Entropy.ViewModels {
    public sealed class EntropyLog : ILogger, INotifyPropertyChanged {
        public EntropyLog() => Context = SynchronizationContext.Current ?? throw new InvalidOperationException("Cannot get syncronization context");

        public ObservableCollection<string> Messages { get; } = new();
        public SynchronizationContext Context { get; }

        public void Log(LogLevel level, string? category, string message, Exception? exception) {
            Context.Post(m => {
                    if (m is not string msg) {
                        return;
                    }

                    Messages.Add(msg);
                    OnPropertyChanged();
                },
                message);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = nameof(Messages)) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Clear() {
            Application.Current.Dispatcher.Invoke(() => {
                Messages.Clear();
                OnPropertyChanged();
            });
        }
    }
}
