using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using DragonLib;
using Equilibrium;
using Equilibrium.Implementations;
using Equilibrium.Interfaces;
using Equilibrium.Logging;
using Equilibrium.Options;
using JetBrains.Annotations;

namespace Entropy.ViewModels {
    [PublicAPI]
    public class EntropyCore : Singleton<EntropyCore>, INotifyPropertyChanged, IDisposable {
        public EntropyCore() {
            var workDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            SettingsFile = Path.Combine(workDir ?? "./", "Entropy.json");
            WorkerThread = new Thread(WorkLoop);
            WorkerThread.Start();
            LogTarget = new MultiLogger {
                Loggers = {
                    new ConsoleLogger(),
                    new DebugLogger(),
                    Log,
                },
            };
            SetOptions(File.Exists(SettingsFile) ? EntropySettings.FromJson(File.ReadAllText(SettingsFile)) : EntropySettings.Default);
        }

        public AssetCollection Collection { get; } = new();
        public EntropyStatus Status { get; } = new();
        public EntropyLog Log { get; set; } = new();
        public ILogger LogTarget { get; }
        public EntropySettings Settings { get; private set; } = EntropySettings.Default;
        public Thread WorkerThread { get; private set; }
        public CancellationTokenSource TokenSource { get; private set; } = new();
        private BlockingCollection<Action<CancellationToken>> Tasks { get; set; } = new();
        public List<EntropyObject> Objects => Collection.Files.SelectMany(x => x.Value.Objects.Values).Select(x => new EntropyObject(x)).ToList();
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
                        LogTarget.Error("Worker", "Failed to perform task", e);
                    }
                }
            } catch (TaskCanceledException) {
                // ignored
            } catch (OperationCanceledException) {
                // ignored
            } catch (Exception e) {
                LogTarget.Error("Worker", "Failed to get tasks", e);
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
            Log.Clear();
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

        public void SetOptions(EntropySettings options) {
            Settings = options with { Options = options.Options with { Reporter = Status, Logger = LogTarget } };
            File.WriteAllText(SettingsFile, Settings.ToJson());
            OnPropertyChanged(nameof(Settings));
        }

        public void SetOptions(EquilibriumOptions options) {
            Settings = Settings with { Options = options with { Reporter = Status, Logger = LogTarget } };
            File.WriteAllText(SettingsFile, Settings.ToJson());
            OnPropertyChanged(nameof(Settings));
        }
    }
}
