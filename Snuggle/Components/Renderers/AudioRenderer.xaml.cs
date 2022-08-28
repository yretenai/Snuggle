using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using NAudio.Utils;
using NAudio.Vorbis;
using NAudio.Wave;
using Snuggle.Converters;
using Snuggle.Core.Implementations;
using Timer = System.Timers.Timer;

namespace Snuggle.Components.Renderers;

public sealed partial class AudioRenderer : INotifyPropertyChanged {
    public AudioRenderer() {
        InitializeComponent();
        Timer = new Timer();
        Timer.Interval = 33;
        Timer.Elapsed += (_, _) => {
            OnPropertyChanged(nameof(Progress));
            OnPropertyChanged(nameof(ProgressTime));
            OnPropertyChanged(nameof(Time));
        };
        Timer.Start();
    }

    public static string Attribution => "Audio Engine: FMOD Studio by Firelight Technologies Pty Ltd.";

    public bool SafeToUse { get; set; }

    public TimeSpan Position {
        get {
            if (!SafeToUse || OutputDevice == null) {
                return TimeSpan.Zero;
            }

            try {
                return OutputDevice.GetPositionTimeSpan();
            } catch {
                SafeToUse = false;
                return TimeSpan.Zero;
            }
        }
    }

    public double Progress => Position.TotalMilliseconds;
    public double ProgressMax => ((DataContext as AudioClip)?.Duration ?? 0) * 1000;
    public string ProgressTime => Position.ToString("g");
    public string Time => TimeSpan.FromMilliseconds(((DataContext as AudioClip)?.Duration ?? 0) * 1000).ToString("g");

    public float Volume {
        get {
            if (OutputDevice == null || !SafeToUse) {
                return 0.5f;
            }

            try {
                return OutputDevice.Volume;
            } catch {
                SafeToUse = false;
            }

            return 0.5f;
        }
        set {
            if (OutputDevice == null || !SafeToUse) {
                return;
            }

            try {
                OutputDevice.Volume = value;
            } catch {
                SafeToUse = false;
            }
        }
    }

    private WaveOutEvent? OutputDevice { get; set; }
    private WaveStream? Source { get; set; }

    private Timer Timer { get; }
    private CancellationTokenSource CancellationTokenSource { get; set; } = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    private void Refresh(object sender, DependencyPropertyChangedEventArgs e) {
        CancellationTokenSource.Cancel();
        CancellationTokenSource = new CancellationTokenSource();

        var volume = SafeToUse ? OutputDevice?.Volume : 0.5f;
        Source?.Dispose();
        OutputDevice?.Dispose();
        OutputDevice = new WaveOutEvent();
        OutputDevice.Volume = volume ?? 0.5f;
        OnPropertyChanged(nameof(Progress));
        OnPropertyChanged(nameof(ProgressTime));
        OnPropertyChanged(nameof(Time));
        OnPropertyChanged(nameof(Volume));
        OnPropertyChanged(nameof(ProgressMax));

        DecodeAndPlay(DataContext as AudioClip, CancellationTokenSource.Token);
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void DecodeAndPlay(AudioClip? clip, CancellationToken token) {
        if (clip == null) {
            return;
        }

        new Thread(DecodeThread).Start((clip, token));
    }

    private void DecodeThread(object? obj) {
        if (obj == null) {
            return;
        }

        var (clip, token) = ((AudioClip, CancellationToken)) obj;
        if (token.IsCancellationRequested) {
            return;
        }

        var data = SnuggleAudioFile.GetPCM(clip, out var ext);
        if (token.IsCancellationRequested || data.Length == 0) {
            return;
        }


        Source?.Dispose();
        if (ext == "ogg") {
            Source = new VorbisWaveReader(new MemoryStream(data), true);
        } else {
            using var br = new BinaryReader(new MemoryStream(data));
            br.BaseStream.Position = 20;
            Source = new RawSourceWaveStream(data, 0, 0, WaveFormat.FromFormatChunk(br, 16));
        }

        if (token.IsCancellationRequested) {
            return;
        }

        OutputDevice!.Init(Source);
        SafeToUse = true;
        OutputDevice.Play();

        Dispatcher.Invoke(
            () => {
                OnPropertyChanged(nameof(Progress));
                OnPropertyChanged(nameof(ProgressTime));
                OnPropertyChanged(nameof(Time));
                OnPropertyChanged(nameof(Volume));
            });
    }

    private void Pause(object sender, RoutedEventArgs e) {
        if (!SafeToUse || OutputDevice == null) {
            return;
        }

        OutputDevice.Pause();
    }

    private void Play(object sender, RoutedEventArgs e) {
        if (!SafeToUse || OutputDevice == null) {
            return;
        }

        if (OutputDevice.PlaybackState == PlaybackState.Stopped) {
            var volume = OutputDevice.Volume;
            OutputDevice.Dispose();
            OutputDevice = new WaveOutEvent();
            OutputDevice.Volume = volume;
            Source!.Position = 0;
            OutputDevice.Init(Source);
        }

        OutputDevice.Play();
    }
}
