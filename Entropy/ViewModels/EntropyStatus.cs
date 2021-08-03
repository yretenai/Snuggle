using System.ComponentModel;
using System.Runtime.CompilerServices;
using Equilibrium.Interfaces;
using JetBrains.Annotations;

namespace Entropy.ViewModels {
    [PublicAPI]
    public class EntropyStatus : IStatusReporter, INotifyPropertyChanged {
        public string Message { get; private set; } = string.Empty;

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
            Max = 0;
            Value = 0;

            OnPropertyChanged(nameof(Max));
            OnPropertyChanged(nameof(Value));
            OnPropertyChanged(nameof(Message));
            OnPropertyChanged(nameof(Percent));
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
