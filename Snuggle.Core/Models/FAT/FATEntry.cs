using Snuggle.Core.Interfaces;

namespace Snuggle.Core.Models.FAT;

public record FATEntry : IVirtualStorageEntry {
    public string LongPath { get; init; } = null!;
    public string ShortPath { get; init; } = null!;
    public string ParentPath { get; init; } = null!;
    public FATAttributes Attributes { get; init; }
    public uint Cluster { get; init; }
    public long Size { get; init; }
    public uint CreationDate { get; set; }
    public uint ModificationDate { get; set; }
    public byte[] Stack { get; init; } = new byte[32];
    public string Path => ParentPath + (string.IsNullOrEmpty(LongPath) ? ShortPath : LongPath);
    public long Length { get; init; }

    public bool IsDirectory() => Attributes.HasFlag(FATAttributes.Directory);

    public bool IsArchive() => Attributes.HasFlag(FATAttributes.Archive);

    public bool IsDevice() => Attributes.HasFlag(FATAttributes.Device);

    public bool IsHidden() => Attributes.HasFlag(FATAttributes.Hide);

    public bool IsReadOnly() => Attributes.HasFlag(FATAttributes.ReadOnly);

    public bool IsSystem() => Attributes.HasFlag(FATAttributes.System);

    public bool IsVolumeLabel() => Attributes.HasFlag(FATAttributes.VolumeLabel);

    public bool IsDeleted() => Stack[0] == 0xE5;

    public bool IsValid() {
        if (Attributes > 0 && !Attributes.HasFlag(FATAttributes.Directory) && !Attributes.HasFlag(FATAttributes.Archive)) {
            return false;
        }

        if (Path[ParentPath.Length..] is "." or "..") {
            return false;
        }

        for (var i = 1; i < 11; i++) {
            if (char.IsAscii((char) Stack[i]) && !char.IsControl((char) Stack[i])) {
                return true;
            }
        }

        return false;
    }

    public bool IsZero() {
        for (var i = 0; i < 32; ++i) {
            if (Stack[i] != 0) {
                return false;
            }
        }

        return true;
    }
}
