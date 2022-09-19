using System.IO;

namespace WikiGen
{
    public class BasedStream : Stream
    {
        public readonly Stream BaseStream;
        public readonly long BaseOffset;
        public readonly long _Length;

        public BasedStream(Stream baseStream, long offset, long length)
        {
            BaseStream = baseStream;
            BaseOffset = offset;
            _Length = length;
            Position = 0;
        }

        public override bool CanRead => BaseStream.CanRead;

        public override bool CanSeek => BaseStream.CanSeek;

        public override bool CanWrite => BaseStream.CanWrite;

        public override long Length => _Length;

        public override long Position
        {
            get => BaseStream.Position - BaseOffset;
            set => BaseStream.Position = value + BaseOffset;
        }

        public override void Flush() => BaseStream.Flush();
        public override int Read(byte[] buffer, int offset, int count) => BaseStream.Read(buffer, offset, count);
        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    Position = offset;
                    break;
                case SeekOrigin.Current:
                    Position += offset;
                    break;
                case SeekOrigin.End:
                    Position = Length - offset;
                    break;
            }
            return Position;
        }
        public override void SetLength(long value) => BaseStream.SetLength(value);
        public override void Write(byte[] buffer, int offset, int count) => BaseStream.Write(buffer, offset, count);
    }
}
