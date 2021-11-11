﻿using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using JetBrains.Annotations;
using Snuggle.Core.Interfaces;
using Snuggle.Core.Meta;

namespace Snuggle.Handlers {
    public sealed class SnuggleLog : ILogger, INotifyPropertyChanged {
        public SnuggleLog() => Context = SynchronizationContext.Current ?? throw new InvalidOperationException("Cannot get syncronization context");

        public ObservableCollection<string> Messages { get; } = new();
        public SynchronizationContext Context { get; }

        public void Log(LogLevel level, string? category, string message, Exception? exception) {
            Context.Post(m => {
                    Messages.Add((string) m!);

                    while (Messages.Count > 100) {
                        Messages.RemoveAt(0);
                    }

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