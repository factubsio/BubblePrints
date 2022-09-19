using BubbleAssets;
using K4os.Compression.LZ4;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace WikiGen.Assets
{

    class RangeComparerer : IComparer<StorageBlock>
    {
        public int Compare(StorageBlock? x, StorageBlock? y)
        {
            if (x is null || y is null) return -1;

            var compareStart = x.End.CompareTo(y.Begin);
            if (compareStart != 0)
                return compareStart;
            return x.End.CompareTo(y.End);
        }

        public static RangeComparerer Instance = new();
    }

    class BundleBlockStream : Stream
    {
        public List<Node> DirectoryInfo = new();
        private readonly List<StorageBlock> _BlocksInfo = new();
        public BinaryReaderWithEndian FileReader;

        private int _LastOpenedBlock = -1;

        private long _Length = 0;
        private long _Pos = 0;
        internal long RawBegin;

        public BundleBlockStream(BinaryReaderWithEndian reader)
        {
            FileReader = reader;
        }

        public void AddBlock(StorageBlock block)
        {
            if (_BlocksInfo.Count == 0)
            {
                block.Begin = 0;
                block.RawBegin = 0;
            }
            else
            {
                block.Begin = _BlocksInfo[^1].End;
                block.RawBegin = _BlocksInfo[^1].RawEnd;
            }

            _Length += block.uncompressedSize;
            _BlocksInfo.Add(block);
        }

        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => false;

        public override long Length => _Length;

        public override long Position
        {
            get => _Pos; set { _Pos = value; }
        }

        private StorageBlock FindBlock(long value)
        {
            int index = _LastOpenedBlock;
            if (index != -1 && index < _BlocksInfo.Count && _BlocksInfo[index].Contains(value))
                return OpenBlock(index);

            index++;
            if (index != -1 && index < _BlocksInfo.Count && _BlocksInfo[index].Contains(value))
            {
                return OpenBlock(index);
            }

            index = _BlocksInfo.BinarySearch(new() { Begin = value, uncompressedSize = 0 }, RangeComparerer.Instance);
            if (index < 0)
                index = ~index;

            if (index >= _BlocksInfo.Count)
                throw new Exception("invalid block index");
            if (!_BlocksInfo[index].Contains(value))
                throw new Exception("found block does not contain position???");

            return OpenBlock(index);
        }

        private StorageBlock OpenBlock(int index)
        {
            var block = _BlocksInfo[index];
            if (!block.Open)
            {
                var buffer = new byte[block.uncompressedSize];
                FileReader.BaseStream.Position = block.RawBegin + RawBegin;
                int lz4Size = LZ4Codec.Decode(FileReader.ReadBytes((int)block.compressedSize), buffer);
                if (lz4Size != block.uncompressedSize)
                {
                    throw new Exception();
                }
                block.Buffer = buffer;
            }

            _LastOpenedBlock = index;
            return block;
        }
        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count)
        {
            var block = FindBlock(_Pos);

            int offsetInBlock = (int)(Position - block.Begin);
            int maxInBlock = (int)(block.uncompressedSize - offsetInBlock);

            int lengthFromBlock = Math.Min(count, maxInBlock);

            var inSpan = block.Buffer.AsSpan(offsetInBlock, lengthFromBlock);
            var outSpan = buffer.AsSpan(offset, lengthFromBlock);
            inSpan.CopyTo(outSpan);

            int read = 0;

            read += lengthFromBlock;
            Position += lengthFromBlock;

            while (read < count)
            {
                block = FindBlock(_Pos);
                lengthFromBlock = Math.Min((count - read), (int)block.uncompressedSize);

                inSpan = block.Buffer.AsSpan(0, lengthFromBlock);
                outSpan = buffer.AsSpan(offset + read, lengthFromBlock);
                inSpan.CopyTo(outSpan);

                read += lengthFromBlock;
                Position += lengthFromBlock;
            }

            return count;
        }

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

        public override void SetLength(long value) => throw new NotImplementedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotImplementedException();
    }

    [Flags]
    public enum StorageBlockFlags
    {
        CompressionTypeMask = 0x3f,
        Streamed = 0x40
    }

    public enum CompressionType
    {
        None,
        Lzma,
        Lz4,
        Lz4HC,
        Lzham
    }

    public class StorageBlock
    {
        public uint compressedSize;
        public uint uncompressedSize;
        public StorageBlockFlags flags;

        public bool Open => Buffer != null;
        public byte[]? Buffer = null;

        public long RawBegin;
        public long RawEnd => RawBegin + compressedSize;

        public long Begin;
        public long End => Begin + uncompressedSize;

        public bool Contains(long value)
        {
            return value < End && value >= Begin;
        }
    }
}
