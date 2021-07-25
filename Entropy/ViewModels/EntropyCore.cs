using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using DragonLib;
using Equilibrium;
using Equilibrium.Meta;
using JetBrains.Annotations;

namespace Entropy.ViewModels {
    [PublicAPI]
    public class EntropyCore : Singleton<EntropyCore>, INotifyPropertyChanged, IDisposable {
        public EntropyCore() {
            var workDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            SettingsFile = Path.Combine(workDir ?? "./", "Entropy.json");
            Options = File.Exists(SettingsFile) ? EquilibriumOptions.FromJson(File.ReadAllText(SettingsFile)) : EquilibriumOptions.Default with { Reporter = Status };
            WorkerThread = new Thread(WorkLoop);
            WorkerThread.Start();
        }

        public AssetCollection Collection { get; } = new();
        public EntropyStatus Status { get; } = new();
        public EquilibriumOptions Options { get; private set; }
        public Thread WorkerThread { get; }
        public CancellationTokenSource? TokenSource { get; set; } = new();
        private Queue<Action> Tasks { get; } = new();

        private string SettingsFile { get; }

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        ~EntropyCore() {
            Dispose(false);
        }

        protected void Dispose(bool disposing) {
            Reset();

            if (disposing) {
                Collection.Dispose();
                TokenSource!.Dispose();
            }

            TokenSource = null;
            WorkerThread.Join();
            Environment.Exit(0);
        }

        private void WorkLoop() {
            while (TokenSource != null) {
                var token = TokenSource?.Token;
                if (token == null) {
                    break;
                }

                if (Tasks.Count > 0) {
                    var task = Tasks.Dequeue();

                    task();
                }

                Thread.Sleep(TimeSpan.FromSeconds(1));
            }
        }

        public void Reset() {
            Collection.Reset();
            Status.Reset();
            Tasks.Clear();
            if (TokenSource != null) {
                TokenSource.Cancel();
                TokenSource.Dispose();
            }

            TokenSource = new CancellationTokenSource();
        }

        public void WorkerAction(Action action) {
            Tasks.Enqueue(action);
        }

        public Task<T> WorkerAction<T>(Func<T> task) {
            var tcs = new TaskCompletionSource<T>();
            Tasks.Enqueue(() => {
                try {
                    tcs.SetResult(task());
                } catch (TaskCanceledException) {
                    tcs.SetCanceled();
                } catch (Exception e) {
                    tcs.SetException(e);
                }
            });
            return tcs.Task;
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void SetOptions(EquilibriumOptions options) {
            Options = options with { Reporter = Status };
            File.WriteAllText(SettingsFile, Options.ToJson());
            OnPropertyChanged(nameof(Options));
        }
    }
}
