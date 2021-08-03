using JetBrains.Annotations;

namespace Equilibrium.Interfaces {
    [PublicAPI]
    public interface IStatusReporter {
        public void SetStatus(string message);
        public void SetProgress(long value);
        public void SetProgressMax(long value);
        public void Reset();
    }
}
