using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace CopyTool
{
    public class ChunkedWriteStream : Stream
    {
        private long _offset;
        private long _length;
        private FileStream _innerStream;

        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => _length;

        public override long Position
        {
            get => _innerStream.Position - _offset;
            set => _innerStream.Position = value + _offset;
        }

        public ChunkedWriteStream(FileStream innerStream, long offset, long length)
        {
            _offset = offset;
            _length = length;
            _innerStream = innerStream;
            _innerStream.Seek(_offset, SeekOrigin.Begin);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return base.ReadAsync(buffer, offset, count, cancellationToken);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            var position = (int)(_innerStream.Position - _offset);

            if (position >= _length)
            {
                return;
            }

            count = (int) Math.Min(count, _length - position);
            _innerStream.Write(buffer, offset, count);
        }

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var position = (int)(_innerStream.Position - _offset);

            if (position >= _length)
            {
                return;
            }

            count = (int) Math.Min(count, _length - position);

            await _innerStream.WriteAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);
        }

        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public override void CopyTo(Stream destination, int bufferSize)
        {
            throw new NotImplementedException();
        }

        public override void Flush()
        {
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }
    }
}
