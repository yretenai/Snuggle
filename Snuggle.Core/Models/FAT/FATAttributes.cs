using System;

namespace Snuggle.Core.Models.FAT;

[Flags]
public enum FATAttributes : byte {
    ReadOnly = 1,
    Hide = 2,
    System = 4,
    VolumeLabel = 8,
    Directory = 16,
    Archive = 32,
    Device = 64,
}
