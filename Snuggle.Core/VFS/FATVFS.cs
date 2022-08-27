using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Snuggle.Core.Interfaces;
using Snuggle.Core.IO;
using Snuggle.Core.Models.FAT;

namespace Snuggle.Core.VFS;

// reference: https://github.com/Gregwar/fatcat/tree/99cb99fc86eb1601ac7ae27f5bba23add04d2543
public record FATVFS : IVirtualStorage {
    public const uint FATFSInfoSignature1 = 0x41615252;
    public const uint FATFSInfoSignature2 = 0x61417272;

    public FATVFS(Stream data, object tag, IFileHandler handler) {
        Tag = tag;
        Handler = handler;

        Span<byte> buffer = stackalloc byte[90];
        data.ReadExactly(buffer);
        BPB = MemoryMarshal.Read<FATBIOSParameterBlock>(buffer);
        if (buffer[0] != 0xEB || buffer[2] != 0x90) {
            throw new InvalidDataException("Invalid FAT boot sector");
        }

        if (BPB.TotalSectors > 0) {
            throw new NotSupportedException("FAT16 is not supported");
        }

        EBR = MemoryMarshal.Read<FATExtendedBootRecord>(buffer[0x24..]);
        FSInfo = MemoryMarshal.Read<FATFSInfo>(ReadRawSector(EBR.FSInfoSector, data)[0x1E4..]);
        if (FSInfo.Signature != 0x61417272) {
            throw new InvalidDataException("Invalid FAT FSInfo sector");
        }

        FATStart = (uint) (BPB.BytesPerSector * BPB.ReservedSectors);
        FATSize = EBR.SectorsPerFAT * BPB.BytesPerSector;
        BytesPerCluster = (uint) (BPB.BytesPerSector * BPB.SectorsPerCluster);
        TotalSize = BPB.TotalSectors32 * BytesPerCluster;
        TotalClusters = FATSize * 8 / 32;
        DataStart = FATStart + BPB.NumberOfFATs * EBR.SectorsPerFAT * BPB.BytesPerSector;
        DataSize = TotalClusters * BytesPerCluster;

        FAT = new uint[BPB.NumberOfFATs][];
        for (var i = 0; i < BPB.NumberOfFATs; ++i) {
            var fat = new uint[FATSize >> 2];
            FAT[i] = fat;
            data.Position = FATStart + FATSize * i;
            data.ReadExactly(MemoryMarshal.AsBytes(fat.AsSpan()));
        }

        var cluster = EBR.RootCluster;
        ProcessDirectory(data, cluster, 0, string.Empty);
    }

    public uint FATStart { get; init; }
    public uint DataStart { get; init; }
    public uint BytesPerCluster { get; init; }
    public uint TotalSize { get; init; }
    public uint FATSize { get; init; }
    public uint TotalClusters { get; init; }
    public uint DataSize { get; init; }
    public uint[][] FAT { get; init; }
    public FATBIOSParameterBlock BPB { get; init; }
    public FATExtendedBootRecord EBR { get; init; }
    public FATFSInfo FSInfo { get; init; }
    public object Tag { get; set; }
    public IFileHandler Handler { get; set; }
    public List<IVirtualStorageEntry> Entries { get; set; } = new();
    public Stream Open(string path, bool leaveOpen = false) => Open(Entries.First(x => x.Path == path), null, leaveOpen);

    public Stream Open(IVirtualStorageEntry entry, Stream? data = null, bool leaveOpen = false) {
        if (entry is not FATEntry fatEntry) {
            throw new ArgumentException("Entry is not a FAT entry");
        }

        data ??= Handler.OpenFile(Tag);

        return new OffsetStream(data, GetClusterAddress(fatEntry.Cluster) + fatEntry.Stack[0], fatEntry.Length, leaveOpen);
    }

    private void ProcessDirectory(Stream data, uint cluster, uint fat, string parentDir) {
        Span<byte> buffer = stackalloc byte[0x20];
        var filename = new FATName(string.Empty);
        do {
            var address = GetClusterAddress(cluster);
            for (var offset = 0; offset < BytesPerCluster; offset += 0x20) {
                data.Position = address + offset;
                data.ReadExactly(buffer);

                if (buffer[0xb] == 0xF) {
                    filename.Append(buffer);
                } else {
                    var shortName = Encoding.ASCII.GetString(buffer[..11]);
                    shortName = shortName[..8].Trim() + "." + shortName.Substring(8, 3).Trim();
                    if (buffer[0] == 0xE5) {
                        shortName = shortName[1..];
                    }

                    if (shortName[^1] == '.') {
                        shortName = shortName[..^1];
                    }

                    var name = filename.ToString();
                    var size = BinaryPrimitives.ReadUInt32LittleEndian(buffer[0x1C..]);
                    var clusterLo = BinaryPrimitives.ReadUInt16LittleEndian(buffer[0x1a..]);
                    var clusterHi = BinaryPrimitives.ReadUInt16LittleEndian(buffer[0x14..]);
                    var entryCluster = ((uint) clusterHi << 16) | clusterLo;
                    var attribute = (FATAttributes) buffer[0xb];
                    var creationDate = BinaryPrimitives.ReadUInt32LittleEndian(buffer[0x10..]);
                    var modDate = BinaryPrimitives.ReadUInt32LittleEndian(buffer[0x16..]);
                    var entry = new FATEntry {
                        LongPath = name,
                        ShortPath = shortName,
                        ParentPath = parentDir,
                        Attributes = attribute,
                        Cluster = entryCluster,
                        Size = size,
                        Length = size,
                        CreationDate = creationDate,
                        ModificationDate = modDate,
                        Stack = buffer.ToArray(),
                    };

                    if (entry.IsDirectory() && entry.Path == "." || entry.Path == "..") {
                        continue;
                    }

                    if (!entry.IsZero() && entry.IsValid() && !entry.IsDeleted()) {
                        if (entry.IsDirectory() && entry.Cluster != cluster) {
                            ProcessDirectory(data, entry.Cluster, fat, parentDir + entry.Path + "/");
                        } else {
                            Entries.Add(entry);
                        }
                    }
                }
            }

            cluster = FAT[fat][cluster] & 0x0FFFFFFF;
            if (cluster > 0xffffff0) {
                break;
            }
        } while (true);
    }

    public Span<byte> ReadRawSector(uint sector, Stream? stream = null) {
        var offset = (long) sector * BPB.BytesPerSector;
        stream ??= Handler.OpenFile(Tag);
        stream.Position = offset;
        Span<byte> buffer = new byte[BPB.BytesPerSector];
        stream.ReadExactly(buffer);
        return buffer;
    }

    public uint GetClusterAddress(uint cluster) => DataStart + BytesPerCluster * (cluster - 2);

    public static bool IsFAT32VFS(Stream data) {
        var pos = data.Position;
        try {
            Span<byte> span = stackalloc byte[3];
            data.ReadExactly(span);
            return span[0] == 0xEB && span[1] > 0 && span[2] == 0x90;
        } catch {
            return false;
        } finally {
            data.Position = pos;
        }
    }

    public readonly record struct FATFSInfo {
        public uint Signature { get; init; }
        public uint FreeClusters { get; init; }
        public uint NextCluster { get; init; }
    }
}
