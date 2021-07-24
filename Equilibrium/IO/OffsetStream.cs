using System;
using System.IO;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Equilibrium.IO {
    [PublicAPI]
    public class OffsetStream : Stream {
        private Stream BaseStream { get; }
        public long Start { get; }
        public long End { get; }

        public OffsetStream(Stream stream, long? offset = null, long? length = null) {
            BaseStream = stream;
            Start = offset ?? stream.Position;
            End = Start + (length ?? stream.Length - Start);
        }

        public override void Close() {
            BaseStream.Close();
        }

        public override async ValueTask DisposeAsync() {
            await BaseStream.DisposeAsync();
            GC.SuppressFinalize(this);
        }

        public override void Flush() {
            BaseStream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count) {
            if (Position < 0) { // stream is reused oh no.
                Seek(0, SeekOrigin.Begin);
            }
            
            if (BaseStream.Position + count > End) {
                throw new IOException();
            }

            return BaseStream.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin) {
            if (offset == 0) {
                return Position;
            }

            var absolutePosition = origin switch {
                SeekOrigin.Begin => Start + offset,
                SeekOrigin.Current => BaseStream.Position + offset,
                SeekOrigin.End => End - offset,
                _ => throw new IOException(),
            };

            if (absolutePosition > End) {
                throw new IOException();
            }

            if (absolutePosition < Start) {
                throw new IOException();
            }

            BaseStream.Seek(absolutePosition, SeekOrigin.Begin);
            return Position;
        }

        public override void SetLength(long value) {
            throw new IOException();
        }

        public override void Write(byte[] buffer, int offset, int count) {
            throw new IOException();
        }

        public override bool CanRead => BaseStream.CanRead;

        public override bool CanSeek => BaseStream.CanSeek;

        public override bool CanWrite => false;

        public override long Length => End - Start;

        public override long Position {
            get => BaseStream.Position - Start;
            set => Seek(value, SeekOrigin.Begin);
        }
    }
}
