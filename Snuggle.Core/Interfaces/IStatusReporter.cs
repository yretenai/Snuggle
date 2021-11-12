using JetBrains.Annotations;

namespace Snuggle.Core.Interfaces;

[PublicAPI]
public interface IStatusReporter {
    public void SetStatus(string message);
    public void SetProgress(long value);
    public void SetProgressMax(long value);
    public void Reset();
}
