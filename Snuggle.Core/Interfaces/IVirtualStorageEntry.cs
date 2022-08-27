namespace Snuggle.Core.Interfaces;

public interface IVirtualStorageEntry {
    public string Path { get; }
    public long Length { get; }
}

public readonly record struct VirtualStorageEntry(string Path, long Length = 0) : IVirtualStorageEntry;
