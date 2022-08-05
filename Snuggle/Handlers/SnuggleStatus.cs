using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using Snuggle.Core.Interfaces;

namespace Snuggle.Handlers;

public class SnuggleStatus : IStatusReporter, INotifyPropertyChanged {
    public string Message { get; private set; } = string.Empty;
    public string SubMessage { get; private set; } = string.Empty;
    public Visibility SubMessageVisible => string.IsNullOrEmpty(SubMessage) ? Visibility.Collapsed : Visibility.Visible;

    public double Percent {
        get {
            if (InvalidValue) {
                return 0;
            }

            var value = (double) Value / Max * 100d;
            if (double.IsNaN(value)) {
                return 0;
            }

            return value;
        }
    }

    public long Max { get; private set; }
    public long Value { get; private set; }
    public bool InvalidValue => Max == 0 && Value != 0 || Value < 0 || Max < 0;

    public event PropertyChangedEventHandler? PropertyChanged;

    public void SetStatus(string message) {
        Message = message;
        OnPropertyChanged(nameof(Message));
    }

    public void SetSubStatus(string message) {
        SubMessage = message;
        OnPropertyChanged(nameof(SubMessage));
        OnPropertyChanged(nameof(SubMessageVisible));
    }

    public void SetProgress(long value) {
        Value = value;
        OnPropertyChanged(nameof(Value));
        OnPropertyChanged(nameof(Percent));
    }

    public void SetProgressMax(long value) {
        Max = value;
        OnPropertyChanged(nameof(Max));
        OnPropertyChanged(nameof(Percent));
    }

    public void Reset() {
        Message = string.Empty;
        SubMessage = string.Empty;
        Max = 0;
        Value = 0;

        OnPropertyChanged(nameof(Max));
        OnPropertyChanged(nameof(Value));
        OnPropertyChanged(nameof(Message));
        OnPropertyChanged(nameof(SubMessage));
        OnPropertyChanged(nameof(SubMessageVisible));
        OnPropertyChanged(nameof(Percent));
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
