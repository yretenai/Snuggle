using JetBrains.Annotations;

namespace Snuggle.Core.Interfaces;

[PublicAPI]
public interface IRenewable {
    public object Tag { get; set; }
    public IFileHandler Handler { get; set; }
}
