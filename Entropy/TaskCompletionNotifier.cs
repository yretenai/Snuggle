using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using JetBrains.Annotations;

namespace Entropy {
    public sealed class TaskCompletionNotifier<T> : INotifyPropertyChanged {
        public TaskCompletionNotifier(object? carry, Task<T> task) {
            Task = task;
            Carried = carry;
            if (task.IsCompleted) {
                return;
            }

            var scheduler = SynchronizationContext.Current != null ? TaskScheduler.FromCurrentSynchronizationContext() : TaskScheduler.Current;

            task.ContinueWith(_ => {
                    OnPropertyChanged(nameof(Loading));
                    OnPropertyChanged(nameof(LoadingVisibility));
                    OnPropertyChanged(nameof(Result));
                },
                CancellationToken.None,
                TaskContinuationOptions.None,
                scheduler);
        }

        public object? Carried { get; }
        public Task<T> Task { get; }
        public bool Loading => Task.Status < TaskStatus.RanToCompletion;
        public Visibility LoadingVisibility => Loading ? Visibility.Visible : Visibility.Hidden;

        public T? Result => Task.Status != TaskStatus.RanToCompletion ? default : Task.Result;

        public event PropertyChangedEventHandler? PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string? propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
