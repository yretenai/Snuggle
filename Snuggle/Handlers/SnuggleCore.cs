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
using Ookii.Dialogs.Wpf;
using Serilog;
using Snuggle.Components;
using Snuggle.Converters;
using Snuggle.Core;
using Snuggle.Core.Options;

namespace Snuggle.Handlers;

public class SnuggleCore : Singleton<SnuggleCore>, INotifyPropertyChanged, IDisposable {
    private readonly object SaveLock = new();

    public SnuggleCore() {
        Dispatcher = Dispatcher.CurrentDispatcher;
        var workDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "./";
        SettingsFile = Path.Combine(workDir, $"{ProjectName}.json");
        WorkerThread = new Thread(WorkLoop);
        WorkerThread.Start();
        SetOptions(File.Exists(SettingsFile) ? SnuggleOptions.FromJson(File.ReadAllText(SettingsFile)) : SnuggleOptions.Default);
        RegisterHandlers();
        ResourceLocator.SetColorScheme(Application.Current.Resources, Settings.LightMode ? ResourceLocator.LightColorScheme : ResourceLocator.DarkColorScheme);
    }

    public Dispatcher Dispatcher { get; set; }
    public AssetCollection Collection { get; } = new();
    public SnuggleStatus Status { get; } = new();
    public SnuggleOptions Settings { get; private set; } = SnuggleOptions.Default;
    public Thread WorkerThread { get; private set; }
    public CancellationTokenSource GlobalTokenSource { get; private set; } = new();
    public CancellationTokenSource TokenSource { get; private set; } = new();
    private BlockingCollection<(string Name, CancellationToken CarryToken, Func<CancellationToken, Task> Work)> Tasks { get; set; } = new();
    public List<SnuggleObject> Objects => Collection.Files.SelectMany(x => x.Value.GetAllObjects()).Select(x => new SnuggleObject(x!)).ToList();
    public SnuggleObject? SelectedObject { get; set; }
    public HashSet<object> Filters { get; init; } = new();
    public IReadOnlyList<SnuggleObject> SelectedObjects { get; set; } = Array.Empty<SnuggleObject>();
    public string? Search { get; set; }
    public bool IsDisposed { get; private set; }

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

    internal const string BaseTitle = "Snuggle";
    private string SettingsFile { get; }

#if DEBUG
    public static Visibility IsDebugVisibility => Visibility.Visible;
#else
    public static Visibility IsDebugVisibility => Debugger.IsAttached ? Visibility.Visible : Visibility.Collapsed;
#endif

    public Visibility HasAssetsVisibility => Collection.Files.IsEmpty ? Visibility.Visible : Visibility.Collapsed;

    public string ProjectName { get; } = Assembly.GetExecutingAssembly().GetName().Name ?? "Snuggle";
    public string FormattedProjectName { get; } = Navigation.SplitName(Assembly.GetExecutingAssembly().GetName().Name ?? "Snuggle");

    public bool IsFree { get; set; } = true;

    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    ~SnuggleCore() {
        Dispose(false);
    }

    protected void Dispose(bool disposing) {
        if (IsDisposed) {
            return;
        }

        IsDisposed = true;
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
            foreach (var (name, token, task) in tasks.GetConsumingEnumerable(GlobalTokenSource.Token)) {
                if (TokenSource.Token.IsCancellationRequested) {
                    TokenSource.Dispose();
                    TokenSource = new CancellationTokenSource();
                }

                if (token.IsCancellationRequested) {
                    continue;
                }
                
                
                var cts = CancellationTokenSource.CreateLinkedTokenSource(TokenSource.Token);

                try {
                    sw.Start();
                    IsFree = false;
                    Dispatcher.Invoke(() => OnPropertyChanged(nameof(IsFree)));
                    task(cts.Token).Wait(cts.Token);
                    sw.Stop();
                    var elapsed = sw.Elapsed;
                    Log.Information("Spent {Elapsed} working on {Name} task", elapsed, name);
                    sw.Reset();
                } catch (Exception e) {
                    Log.Error(e, "Failed to perform {Name} task", name);
                } finally {
                    IsFree = true;
                    if (!GlobalTokenSource.Token.IsCancellationRequested) {
                        Dispatcher.Invoke(() => OnPropertyChanged(nameof(IsFree)));
                    }
                }

                Log.Information("Memory Tension: {Size}", GC.GetTotalMemory(false).GetHumanReadableBytes());
            }
        } catch (TaskCanceledException) {
            // ignored
        } catch (OperationCanceledException) {
            // ignored
        } catch (Exception e) {
            Log.Error(e, "Failed to get tasks");
        } finally {
            try {
                Dispatcher.Invoke(() => OnPropertyChanged(nameof(IsFree)));
            } catch {
                // ignored
                if (!GlobalTokenSource.Token.IsCancellationRequested) {
                    Dispatcher.Invoke(() => OnPropertyChanged(nameof(IsFree)));
                }
            }
        }
    }

    public void Respawn() {
        TokenSource.Cancel();
    }

    public void Iterate() {
        OnPropertyChanged(nameof(Objects));
        OnPropertyChanged(nameof(HasAssetsVisibility));
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(Filters));
        OnPropertyChanged(nameof(SelectedObject));
        OnPropertyChanged(nameof(SelectedObjects));
        OnPropertyChanged(nameof(IsFree));
    }

    public void Reset(bool respawn = true) {
        Tasks.CompleteAdding();
        Tasks = new BlockingCollection<(string, CancellationToken, Func<CancellationToken, Task>)>();
        GlobalTokenSource.Cancel();
        TokenSource.Cancel();
        TokenSource.Dispose();
        GlobalTokenSource.Dispose();
        SelectedObject = null;
        Status.Reset();
        Search = string.Empty;
        Filters.Clear();
        SnuggleTextureFile.ClearMemory();
        SnuggleSpriteFile.ClearMemory();
        Collection.Reset();
        if (respawn) {
            GlobalTokenSource = new CancellationTokenSource();
            TokenSource = new CancellationTokenSource();
            WorkerThread = new Thread(WorkLoop);
            WorkerThread.Start();
            Iterate();
        }
    }

    public void WorkerAction(string name, Action<CancellationToken> action, bool report, CancellationToken token = new()) {
        Log.Information("Enqueuing {Name} task", name);
        Tasks.Add(
            (name, token, linkedToken => {
                try {
                    if (report) {
                        Instance.Status.SetStatus($"Working on {name}...");
                    }

                    Log.Information("Working on {Name}...", name);
                    action(linkedToken);
                    if (report) {
                        Instance.Status.SetStatus($"{name} done.");
                    }

                    Log.Information("{Name} done", name);
                } catch (Exception e) {
                    Instance.Status.SetStatus($"{name} failed! {e.Message}");
                    Log.Error(e, "{Name} failed!", name);
                }
                
                return Task.CompletedTask;
            }),
            token);
    }

    public Task<T> WorkerAction<T>(string name, Func<CancellationToken, T> task, bool report, CancellationToken token = new()) {
        var tcs = new TaskCompletionSource<T>();
        Tasks.Add(
            (name, token, link => {
                try {
                    if (report) {
                        Instance.Status.SetStatus($"Working on {name}...");
                    }

                    Log.Information("Working on {Name}...", name);
                    tcs.SetResult(task(link));
                    if (report) {
                        Instance.Status.SetStatus($"{name} done.");
                    }

                    Log.Information("{Name} done", name);
                } catch (TaskCanceledException) {
                    tcs.SetCanceled(link);
                } catch (Exception e) {
                    Instance.Status.SetStatus($"{name} failed! {e.Message}");
                    Log.Error(e, "{Name} failed!", name);
                    tcs.SetException(e);
                }

                return Task.CompletedTask;
            }),
            token);
        return tcs.Task;
    }

    public void AsyncWorkerAction(string name, Func<CancellationToken, Task> action, bool report, CancellationToken token = new()) {
        Log.Information("Enqueuing {Name} task", name);
        Tasks.Add(
            (name, token, async linkedToken => {
                try {
                    if (report) {
                        Instance.Status.SetStatus($"Working on {name}...");
                    }

                    Log.Information("Working on {Name}...", name);
                    await action(linkedToken);
                    if (report) {
                        Instance.Status.SetStatus($"{name} done.");
                    }

                    Log.Information("{Name} done", name);
                } catch (Exception e) {
                    Instance.Status.SetStatus($"{name} failed! {e.Message}");
                    Log.Error(e, "{Name} failed!", name);
                }
            }),
            token);
    }

    public Task<T> AsyncWorkerAction<T>(string name, Func<CancellationToken, Task<T>> task, bool report, CancellationToken token = new()) {
        var tcs = new TaskCompletionSource<T>();
        Tasks.Add(
            (name, token, async link => {
                try {
                    if (report) {
                        Instance.Status.SetStatus($"Working on {name}...");
                    }

                    Log.Information("Working on {Name}...", name);
                    tcs.SetResult(await task(link));
                    if (report) {
                        Instance.Status.SetStatus($"{name} done.");
                    }

                    Log.Information("{Name} done", name);
                } catch (TaskCanceledException) {
                    tcs.SetCanceled(link);
                } catch (Exception e) {
                    Instance.Status.SetStatus($"{name} failed! {e.Message}");
                    Log.Error(e, "{Name} failed!", name);
                    tcs.SetException(e);
                }
            }),
            token);
        return tcs.Task;
    }

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
        Settings = options with { Options = options.Options with { Reporter = Status } };
        SaveOptions();
    }

    public void SetOptions(SnuggleCoreOptions options) {
        Settings = Settings with { Options = options with { Reporter = Status } };
        SaveOptions();
    }

    private void RegisterHandlers() {
        Settings.ObjectOptions.RequestAssemblyCallback = RequestAssembly;
    }

    private (string? Path, SnuggleCoreOptions? Options) RequestAssembly(string assemblyName) {
        if (!OperatingSystem.IsWindows()) {
            return (null, null);
        }

        if (Dispatcher.Thread != Thread.CurrentThread) {
            return (null, null);
        }

        var selection = new VistaOpenFileDialog {
            Multiselect = false,
            FileName = assemblyName,
            InitialDirectory = Path.GetDirectoryName(Settings.RecentFiles.LastOrDefault()),
            Title = "Select Assembly",
            Filter = "Assembly Files (*.dll)|*.dll|All Files (*.*)|*.*",
        };

        return selection.ShowDialog() != true ? (null, null) : (selection.FileName, Settings.Options);
    }

    public void FreeMemory() {
        foreach (var (_, file) in Collection.Files) {
            file.Free();
        }

        Collection.ClearCaches();

        SnuggleTextureFile.ClearMemory();
        SnuggleSpriteFile.ClearMemory();

        AssetCollection.Collect();
    }
}
