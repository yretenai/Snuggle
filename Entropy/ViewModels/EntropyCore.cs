using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using DragonLib;
using Equilibrium;
using Equilibrium.Implementations;
using Equilibrium.Options;
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
        public Thread WorkerThread { get; private set; }
        public CancellationTokenSource TokenSource { get; private set; } = new();
        private BlockingCollection<Action<CancellationToken>> Tasks { get; set; } = new();
        public IImmutableList<SerializedObject> Objects => Collection.Files.SelectMany(x => x.Value.Objects.Values).ToImmutableArray();
        public SerializedObject? SelectedObject { get; set; }
        public string? Search { get; set; }

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
            Reset(false);

            if (disposing) {
                Collection.Dispose();
            }

            Environment.Exit(0);
        }

        private void WorkLoop() {
            try {
                var tasks = Tasks;
                foreach (var task in tasks.GetConsumingEnumerable(TokenSource.Token)) {
                    try {
                        task(TokenSource.Token);
                    } catch (Exception e) {
                        Debug.WriteLine(e);
                        // TODO show an alert.
                    }
                }
            } catch (TaskCanceledException) { } catch (Exception e) {
                Debug.WriteLine(e);
            }
        }

        public void Reset(bool respawn = true) {
            Tasks.CompleteAdding();
            Tasks = new BlockingCollection<Action<CancellationToken>>();
            TokenSource.Cancel();
            TokenSource.Dispose();
            WorkerThread.Join();
            SelectedObject = null;
            Collection.Reset();
            Status.Reset();
            if (respawn) {
                TokenSource = new CancellationTokenSource();
                WorkerThread = new Thread(WorkLoop);
                WorkerThread.Start();
                OnPropertyChanged(nameof(Objects));
                OnPropertyChanged(nameof(SelectedObject));
            }
        }

        public void WorkerAction(Action<CancellationToken> action) {
            Tasks.Add(action);
        }

        public Task<T> WorkerAction<T>(Func<CancellationToken, T> task) {
            var tcs = new TaskCompletionSource<T>();
            Tasks.Add(token => {
                try {
                    tcs.SetResult(task(token));
                } catch (TaskCanceledException) {
                    tcs.SetCanceled(token);
                } catch (Exception e) {
                    tcs.SetException(e);
                }
            });
            return tcs.Task;
        }

        [NotifyPropertyChangedInvocator]
        public virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void SetOptions(EquilibriumOptions options) {
            Options = options with { Reporter = Status };
            File.WriteAllText(SettingsFile, Options.ToJson());
            OnPropertyChanged(nameof(Options));
        }
    }
}
