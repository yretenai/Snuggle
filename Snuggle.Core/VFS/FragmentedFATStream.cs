using System;
using System.IO;

namespace Snuggle.Core.VFS; 

public class FragmentedFATStream : Stream {
    public FragmentedFATStream(Stream stream, int offset, long size, uint[] sequence, FATVFS vfs) {
        BaseStream = stream;
        Length = size;
        Offset = offset;
        Cluster = 0;
        Sequence = sequence;
        VFS = vfs;
    }

    private uint[] Sequence { get; }
    public Stream BaseStream { get; }
    public override void Flush() { }

    public override int Read(byte[] buffer, int offset, int count) {
        var originalCount = count;
        while (count > 0) {
            var localOffset = Cluster == 1 ? Offset : 0;
            var sectorOffset = (int) ((Position + localOffset) % VFS.BytesPerCluster);
            var bytesLeftInThisSector = (int) VFS.BytesPerCluster - sectorOffset;
            var newPos = VFS.GetClusterAddress(Sequence[Cluster]) + sectorOffset;
            if (BaseStream.Position != newPos) {
                BaseStream.Position = newPos;
            }

            var bytesToRead = Math.Min(bytesLeftInThisSector, count);
            var bytesRead = BaseStream.Read(buffer, offset, bytesToRead);
            Position += bytesRead;
            offset += bytesRead;
            count -= bytesRead;
            CalculateCluster();

            if (bytesRead == 0) {
                return count;
            }
        }

        return originalCount - count;
    }

    public override long Seek(long offset, SeekOrigin origin) {
        switch (origin) {
            case SeekOrigin.Begin:
                Position = offset;
                break;
            case SeekOrigin.Current:
                Position += offset;
                break;
            case SeekOrigin.End:
                Position = Length - offset;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(origin), origin, null);
        }
        
        CalculateCluster();
        return Position;
    }

    private void CalculateCluster() { 
        Cluster = (uint) ((Position + Offset) % VFS.BytesPerCluster);
    }

    public override void SetLength(long value) {
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count) {
        throw new NotSupportedException();
    }

    public override bool CanRead => BaseStream.CanRead;
    public override bool CanSeek => BaseStream.CanSeek;
    public override bool CanWrite => false;
    public override long Length { get; }
    public FATVFS VFS { get; }
    public override long Position { get; set; }
    public int Offset { get; }
    public uint Cluster { get; private set; }
}
