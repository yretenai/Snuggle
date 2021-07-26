using JetBrains.Annotations;

namespace Equilibrium.Meta {
    [PublicAPI]
    public interface IStatusReporter {
        public void SetStatus(string message);
        public void SetProgress(long value);
        public void SetProgressMax(long value);
        public void Reset();
        public void Log(string message);
    }
}
