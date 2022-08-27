using System.Runtime.InteropServices;

namespace Snuggle.Core.Models.FAT;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct FATBIOSParameterBlock {
    public ushort Magic { get; init; }
    public byte NOP { get; init; }
    public ulong OEM { get; init; }
    public ushort BytesPerSector { get; init; }
    public byte SectorsPerCluster { get; init; }
    public ushort ReservedSectors { get; init; }
    public byte NumberOfFATs { get; init; }
    public ushort RootEntries { get; init; }
    public ushort TotalSectors { get; init; }
    public byte MediaDescriptor { get; init; }
    public ushort SectorsPerFAT { get; init; }
    public ushort SectorsPerTrack { get; init; }
    public ushort Heads { get; init; }
    public uint HiddenSectors { get; init; }
    public uint TotalSectors32 { get; init; }
}
