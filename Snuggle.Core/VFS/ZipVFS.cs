using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Snuggle.Core.Interfaces;
using Snuggle.Core.IO;
using Snuggle.Core.Models.ZIP;

namespace Snuggle.Core.VFS;

public sealed record ZipVFS : IVirtualStorage {
    public unsafe ZipVFS(Stream data, object tag, IFileHandler handler) {
        Tag = tag;
        Handler = handler;

        Span<byte> buffer = stackalloc byte[sizeof(ZIPEndOfCentralDirectory)];
        data.Seek(-buffer.Length, SeekOrigin.End);
        data.ReadExactly(buffer);
        EOCD = MemoryMarshal.Read<ZIPEndOfCentralDirectory>(buffer);
        if (EOCD.Magic != 0x06054B50) {
            throw new InvalidDataException("Can't read end of central directory magic");
        }

        if (EOCD.IsZIP64) {
            buffer = stackalloc byte[sizeof(ZIP64EndOfCentralDirectoryLocator)];
            data.Seek(-(buffer.Length + sizeof(ZIPEndOfCentralDirectory)), SeekOrigin.End);
            data.ReadExactly(buffer);
            EOCD64Locator = MemoryMarshal.Read<ZIP64EndOfCentralDirectoryLocator>(buffer);
            if (EOCD64Locator.Magic != 0x07064B50) {
                throw new InvalidDataException("Can't read end of central directory locator magic");
            }

            buffer = stackalloc byte[sizeof(ZIP64EndOfCentralDirectory)];
            data.Seek(EOCD64Locator.EOCDOffset, SeekOrigin.Begin);
            data.ReadExactly(buffer);
            EOCD64 = MemoryMarshal.Read<ZIP64EndOfCentralDirectory>(buffer);
            if (EOCD64.Magic != 0x06064B50) {
                throw new InvalidDataException("Can't read end of central directory zip64 magic");
            }
        }

        var numberOfRecords = EOCD.IsZIP64 ? EOCD64.NumberOfDirectoryRecords : EOCD.NumberOfDirectoryRecords;
        data.Seek(EOCD.IsZIP64 ? EOCD64.DirectoryOffset : EOCD.DirectoryOffset, SeekOrigin.Begin);
        buffer = new byte[EOCD.IsZIP64 ? EOCD64.DirectorySize : EOCD.DirectorySize];
        data.ReadExactly(buffer);
        var offset = 0;
        for (var i = 0L; i < numberOfRecords; ++i) {
            var record = MemoryMarshal.Read<ZIPCentralDirectoryHeader>(buffer[offset..]);
            if (record.Magic != 0x02014B50) {
                throw new InvalidDataException("Can't read central directory header magic");
            }

            offset += sizeof(ZIPCentralDirectoryHeader);
            var name = Encoding.UTF8.GetString(buffer[offset..(offset + record.FileNameLength)]);
            offset += record.FileNameLength;
            var extraData = buffer[offset..(offset + record.ExtraFieldLength)];
            offset += record.ExtraFieldLength;
            var extra = new List<KeyValuePair<ZIPExtraHeader, object>>();
            for (var extraOffset = 0; extraOffset < extraData.Length;) {
                var extraField = MemoryMarshal.Read<ZIPExtraHeader>(extraData[extraOffset..]);
                extraOffset += sizeof(ZIPExtraHeader);
                var extraFieldData = extraData[extraOffset..(extraOffset + extraField.Length)];
                extraOffset += extraField.Length;
                switch (extraField.Id) {
                    case ZIPExtraHeaderId.ZIP64ExtraHeader:
                        var tmp = new byte[sizeof(ZIP64ExtendedInformation)].AsSpan();
                        extraFieldData.CopyTo(tmp);
                        extra.Add(new KeyValuePair<ZIPExtraHeader, object>(extraField, MemoryMarshal.Read<ZIP64ExtendedInformation>(tmp)));
                        break;
                    default:
                        extra.Add(new KeyValuePair<ZIPExtraHeader, object>(extraField, extraFieldData.ToArray()));
                        break;
                }
            }

            var comment = Encoding.UTF8.GetString(buffer[offset..(offset + record.CommentLength)]);
            offset += record.CommentLength;
            Entries.Add(new ZIPEntry(name, record.UncompressedSize, extra, comment, record));
        }
    }

    public ZIPEndOfCentralDirectory EOCD { get; }
    public ZIP64EndOfCentralDirectoryLocator EOCD64Locator { get; set; }
    public ZIP64EndOfCentralDirectory EOCD64 { get; set; }
    public List<IVirtualStorageEntry> Entries { get; set; } = new();

    public Stream Open(string path, bool leaveOpen = false) => Open(new VirtualStorageEntry(path), null, leaveOpen);

    public unsafe Stream Open(IVirtualStorageEntry entry, Stream? data = null, bool leaveOpen = false) {
        if (entry is not ZIPEntry zipEntry) {
            throw new ArgumentException("Entry is not a ZIP entry");
        }

        if ((zipEntry.Header.Flags & 0b1) == 1) {
            throw new NotSupportedException("Encrypted ZIP files are not supported");
        }

        data ??= Handler.OpenFile(Tag);

        var csize = (long) zipEntry.Header.CompressedSize;
        var msize = (long) zipEntry.Header.UncompressedSize;
        var offset = (long) zipEntry.Header.Offset;
        var zipInfoComposite = zipEntry.Extra.FirstOrDefault(x => x.Value is ZIP64ExtendedInformation);
        if (zipInfoComposite.Value is ZIP64ExtendedInformation zipInfo) {
            if (zipInfoComposite.Key.Length >= 8 && zipEntry.Header.CompressedSize == uint.MaxValue) {
                csize = zipInfo.CompressedSize;
            }

            if (zipInfoComposite.Key.Length >= 16 && zipEntry.Header.UncompressedSize == uint.MaxValue) {
                msize = zipInfo.UncompressedSize;
            }

            if (zipInfoComposite.Key.Length >= 24 && zipEntry.Header.Offset == uint.MaxValue) {
                offset = zipInfo.Offset;
            }
        }

        data.Seek(offset, SeekOrigin.Begin);
        Span<byte> buffer = stackalloc byte[sizeof(ZIPFileHeader)];
        data.ReadExactly(buffer);
        var header = MemoryMarshal.Read<ZIPFileHeader>(buffer);
        if (header.Magic != 0x04034B50) {
            throw new InvalidDataException("Can't read file header magic");
        }

        var shift = buffer.Length + header.ExtraFieldLength + header.FileNameLength;
        offset += shift;

        if (zipEntry.Header.Compression == ZIPCompression.Store) {
            return new OffsetStream(data, offset, msize, leaveOpen);
        }

        using var stream = new OffsetStream(data, offset, csize, leaveOpen);

        // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
        switch (zipEntry.Header.Compression) {
            case ZIPCompression.Deflate:
            case ZIPCompression.Deflate64: {
                var ms = new MemoryStream();
                using var ds = new DeflateStream(stream, CompressionMode.Decompress);
                ds.CopyTo(ms);
                ms.Position = 0;
                return ms;
            }
            default:
                throw new NotSupportedException($"Compression {zipEntry.Header.Compression} is not supported");
        }
    }

    public object Tag { get; set; }
    public IFileHandler Handler { get; set; }

    public static bool IsZipVFS(Stream data) {
        var pos = data.Position;
        try {
            Span<byte> span = stackalloc byte[4];
            data.ReadExactly(span);
            return BinaryPrimitives.ReadUInt32LittleEndian(span) == 0x04034b50;
        } catch {
            return false;
        } finally {
            data.Position = pos;
        }
    }
}
