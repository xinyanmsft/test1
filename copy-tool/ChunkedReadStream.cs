using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CopyTool
{
    public class ChunkedReadStream : Stream
    {
        private long _offset;
        private long _length;
        private FileStream _innerStream;

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => _length;

        public override long Position
        {
            get => _innerStream.Position - _offset;
            set => _innerStream.Position = value + _offset;
        }

        public ChunkedReadStream(FileStream innerStream, long offset, long length)
        {
            _offset = offset;
            _length = length;
            _innerStream = innerStream;
            _innerStream.Seek(_offset, SeekOrigin.Begin);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var position = (int) (_innerStream.Position - _offset);

            if (position >= _length)
            {
                return 0;
            }

            count = (int) Math.Min(count, _length - position);
            return _innerStream.Read(buffer, offset, count);
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var position = (int)(_innerStream.Position - _offset);

            if (position >= _length)
            {
                return 0;
            }

            count = (int) Math.Min(count, _length - position);

            return await _innerStream.ReadAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
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
