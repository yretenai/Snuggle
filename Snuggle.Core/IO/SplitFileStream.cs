using System;
using System.IO;
using System.Linq;

namespace Snuggle.Core.IO;

public class SplitFileStream : Stream {
    public SplitFileStream(string path, FileMode mode = FileMode.Open, FileAccess access = FileAccess.Read, FileShare share = FileShare.ReadWrite) {
        path = Path.GetFullPath(path);
        if (Path.GetExtension(path) == ".split0") {
            path = Path.Combine(Path.GetDirectoryName(path)!, Path.GetFileNameWithoutExtension(path));
        }

        var splitFiles = Directory.GetFiles(Path.GetDirectoryName(path)!, Path.GetFileName(path) + ".split*", SearchOption.TopDirectoryOnly);
        if (splitFiles.Length == 0) {
            throw new FileNotFoundException("No split files found");
        }

        BasePath = path;
        Streams = new Stream[splitFiles.Length];
        var ordered = splitFiles.OrderBy(x => int.Parse(x[(path.Length + 6)..])).ToArray();
        var length = 0L;
        for (var i = 0; i < splitFiles.Length; i++) {
            Streams[i] = new FileStream(ordered[i], mode, access, share);
            length += Streams[i].Length;
        }

        Length = length;
    }

    public string BasePath { get; set; }
    public FileMode Mode { get; set; }
    public FileAccess Access { get; set; }
    public FileShare Share { get; set; }
    public Stream[] Streams { get; set; }

    public override bool CanRead => true;
    public override bool CanSeek => true;
    public override bool CanWrite => false;
    public override long Length { get; }
    public override long Position { get; set; }

    public override int Read(byte[] buffer, int offset, int count) {
        var splitOffset = Position % 1048576;
        var splitIndex = (int) (Position / 1048576);

        var total = count;
        while (count > 0) {
            if (splitIndex > Streams.Length - 1) {
                break;
            }

            var stream = Streams[splitIndex];
            stream.Position = splitOffset;
            var amount = (int) (count > stream.Length - stream.Position ? stream.Length - stream.Position : count);
            stream.ReadExactly(buffer, offset, amount);
            offset += amount;
            Position += amount;
            splitOffset = 0;
            splitIndex++;
            count -= amount;
        }

        var read = total - count;

        return read;
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

        if (Position > Length) {
            Position = Length;
        } else if (Position < 0) {
            Position = 0;
        }

        return Position;
    }

    public override void Flush() => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    public static long GetLength(string path) {
        path = Path.GetFullPath(path);
        if (Path.GetExtension(path) == ".split0") {
            path = Path.Combine(Path.GetDirectoryName(path)!, Path.GetFileNameWithoutExtension(path));
        }

        var splitFiles = Directory.GetFiles(Path.GetDirectoryName(path)!, Path.GetFileName(path) + ".split*", SearchOption.TopDirectoryOnly);
        return splitFiles.Length == 0 ? 0 : splitFiles.OrderBy(x => int.Parse(x[(path.Length + 6)..])).Sum(file => new FileInfo(file).Length);
    }
}
