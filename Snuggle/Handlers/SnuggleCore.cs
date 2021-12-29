using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using AdonisUI;
using DragonLib;
using JetBrains.Annotations;
using Snuggle.Components;
using Snuggle.Converters;
using Snuggle.Core;
using Snuggle.Core.Interfaces;
using Snuggle.Core.Logging;
using Snuggle.Core.Meta;
using Snuggle.Core.Options;
using Snuggle.Core.Options.Game;

namespace Snuggle.Handlers;

[PublicAPI]
public class SnuggleCore : Singleton<SnuggleCore>, INotifyPropertyChanged, IDisposable {
    private object SaveLock = new();

    public SnuggleCore() {
        Dispatcher = Dispatcher.CurrentDispatcher;
        var workDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "./";
        SettingsFile = Path.Combine(workDir, $"{ProjectName}.json");
        WorkerThread = new Thread(WorkLoop);
        WorkerThread.Start();
        if (!Directory.Exists(Path.Combine(workDir, "Log"))) {
            Directory.CreateDirectory(Path.Combine(workDir, "Log"));
        }

        LogTarget = new MultiLogger { Loggers = { new ConsoleLogger(), new DebugLogger(), new FileLogger(new FileStream(Path.Combine(workDir, "Log", $"SnuggleLog_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds():D}.log"), FileMode.Create)) } };
        SetOptions(File.Exists(SettingsFile) ? SnuggleOptions.FromJson(File.ReadAllText(SettingsFile)) : SnuggleOptions.Default);
        ResourceLocator.SetColorScheme(Application.Current.Resources, Settings.LightMode ? ResourceLocator.LightColorScheme : ResourceLocator.DarkColorScheme);
    }

    public Dispatcher Dispatcher { get; set; }
    public AssetCollection Collection { get; } = new();
    public SnuggleStatus Status { get; } = new();
    public ILogger LogTarget { get; }
    public SnuggleOptions Settings { get; private set; } = SnuggleOptions.Default;
    public Thread WorkerThread { get; private set; }
    public CancellationTokenSource TokenSource { get; private set; } = new();
    private BlockingCollection<(string Name, Action<CancellationToken> Work)> Tasks { get; set; } = new();
    public List<SnuggleObject> Objects => Collection.Files.SelectMany(x => x.Value.GetAllObjects()).Select(x => new SnuggleObject(x)).ToList();
    public SnuggleObject? SelectedObject { get; set; }
    public HashSet<object> Filters { get; set; } = new();
    public IReadOnlyList<SnuggleObject> SelectedObjects { get; set; } = Array.Empty<SnuggleObject>();
    public string? Search { get; set; }

    public string Title {
        get {
            var str = BaseTitle;
            if (!string.IsNullOrEmpty(Collection.PlayerSettings?.CombinedName)) {
                str += $" | {Collection.PlayerSettings.CombinedName}";
                str += $" | Unity {Collection.PlayerSettings.SerializedFile.Version.ToStringSafe()}";
            } else if (Objects.Count > 0) {
                str += $" | Unity {Objects[0].GetObject()!.SerializedFile.Version.ToStringSafe()}";
            }

            return str;
        }
    }

    private const string BaseTitle = "Snuggle";
    private string SettingsFile { get; }

#if DEBUG
    public static Visibility IsDebugVisibility => Visibility.Visible;
#else
    public static Visibility IsDebugVisibility => Debugger.IsAttached ? Visibility.Visible : Visibility.Collapsed;
#endif

    public Visibility HasAssetsVisibility => Collection.Files.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

    public string ProjectName { get; } = Assembly.GetExecutingAssembly().GetName().Name ?? "Snuggle";
    public string FormattedProjectName { get; } = Navigation.SplitName(Assembly.GetExecutingAssembly().GetName().Name ?? "Snuggle");

    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    ~SnuggleCore() {
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
            var sw = new Stopwatch();
            foreach (var (name, task) in tasks.GetConsumingEnumerable(TokenSource.Token)) {
                try {
                    sw.Start();
                    task(TokenSource.Token);
                    sw.Stop();
                    var elapsed = sw.Elapsed;
                    LogTarget.Info("Worker", $"Spent {elapsed} working on {name} task");
                    sw.Reset();
                } catch (Exception e) {
                    LogTarget.Error("Worker", $"Failed to perform {name} task", e);
                }

                LogTarget.Info("Worker", $"Memory Tension: {GC.GetTotalMemory(false).GetHumanReadableBytes()}");
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
        Tasks = new BlockingCollection<(string Name, Action<CancellationToken> Work)>();
        TokenSource.Cancel();
        TokenSource.Dispose();
        WorkerThread.Join();
        SelectedObject = null;
        Status.Reset();
        Search = string.Empty;
        Filters.Clear();
        SnuggleTextureFile.ClearMemory();
        SnuggleSpriteFile.ClearMemory();
        Collection.Reset();
        if (respawn) {
            TokenSource = new CancellationTokenSource();
            WorkerThread = new Thread(WorkLoop);
            WorkerThread.Start();
            OnPropertyChanged(nameof(Objects));
            OnPropertyChanged(nameof(HasAssetsVisibility));
            OnPropertyChanged(nameof(Title));
            OnPropertyChanged(nameof(Filters));
            OnPropertyChanged(nameof(SelectedObject));
            OnPropertyChanged(nameof(SelectedObjects));
        }
    }

    public void WorkerAction(string name, Action<CancellationToken> action, bool report) {
        Tasks.Add(
            (name, token => {
                try {
                    if (report) {
                        Instance.Status.SetStatus($"Working on {name}...");
                        Instance.LogTarget.Info($"Working on {name}...");
                    }

                    action(token);
                    if (report) {
                        Instance.Status.SetStatus($"{name} done.");
                        Instance.LogTarget.Info($"{name} done.");
                    }
                } catch (Exception e) {
                    Instance.Status.SetStatus($"{name} failed! {e.Message}");
                    Instance.LogTarget.Error("Worker", $"{name} failed! {e.Message}", e);
                }
            }));
    }

    public Task<T> WorkerAction<T>(string name, Func<CancellationToken, T> task, bool report) {
        var tcs = new TaskCompletionSource<T>();
        Tasks.Add(
            (name, token => {
                try {
                    if (report) {
                        Instance.Status.SetStatus($"Working on {name}...");
                    }

                    tcs.SetResult(task(token));
                    if (report) {
                        Instance.Status.SetStatus($"{name} done.");
                    }
                } catch (TaskCanceledException) {
                    tcs.SetCanceled(token);
                } catch (Exception e) {
                    Instance.Status.SetStatus($"{name} failed! {e.Message}");
                    Instance.LogTarget.Error($"{name} failed! {e.Message}", e);
                    tcs.SetException(e);
                }
            }));
        return tcs.Task;
    }

    [NotifyPropertyChangedInvocator]
    public virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) {
        Dispatcher.Invoke(() => { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)); });
    }

    public void SaveOptions() {
        lock (SaveLock) {
            File.WriteAllText(SettingsFile, Settings.ToJson());
        }

        OnPropertyChanged(nameof(Settings));
    }

    public void SetOptions(SnuggleOptions options) {
        Settings = options with { Options = options.Options with { Reporter = Status, Logger = LogTarget } };
        SaveOptions();
    }

    public void SetOptions(SnuggleCoreOptions options) {
        Settings = Settings with { Options = options with { Reporter = Status, Logger = LogTarget } };
        SaveOptions();
    }

    public void SetOptions(UnityGame game, IUnityGameOptions options) {
        Settings.Options.GameOptions.SetOptions(game, options);
        SaveOptions();
    }

    public void FreeMemory(bool bundles) {
        if (bundles) {
            foreach (var bundle in Collection.Bundles) {
                bundle.ClearCache();
            }
        }

        foreach (var (_, file) in Collection.Files) {
            file.Free();
        }

        SnuggleTextureFile.ClearMemory();
        SnuggleSpriteFile.ClearMemory();

        AssetCollection.Collect();
    }
}
