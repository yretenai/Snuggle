using System.Runtime.InteropServices;

namespace Snuggle.Core.Models.FAT;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct FATExtendedBootRecord {
    public uint SectorsPerFAT { get; init; }
    public ushort Flags { get; init; }
    public ushort Version { get; init; }
    public uint RootCluster { get; init; }
    public ushort FSInfoSector { get; init; }
    public ushort BackupBootSector { get; init; }
    public ulong ReservedA { get; init; }
    public uint ReservedB { get; init; }
    public byte DriveNumber { get; init; }
    public byte NTFlags { get; init; }
    public byte Signature { get; init; }
    public uint Serial { get; init; }
    public byte VolumeLabelA { get; init; }
    public ushort VolumeLabelB { get; init; }
    public ulong VolumeLabelC { get; init; }
    public ulong SystemIdentifier { get; init; }
}
