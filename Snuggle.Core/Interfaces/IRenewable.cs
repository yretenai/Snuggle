namespace Snuggle.Core.Interfaces;

public interface IRenewable {
    public object Tag { get; set; }
    public IFileHandler Handler { get; set; }
}
