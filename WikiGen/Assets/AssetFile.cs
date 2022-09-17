using K4os.Compression.LZ4;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace WikiGen.Assets
{
    public class AssetFile
    {

        public override string ToString() => $"<{OwningBundle}/asset-file, {ObjectIndex.Count} assets>";


        public Version Version;
        public uint m_MetadataSize;
        public long m_FileSize;
        public AssVer m_Version;
        public long m_DataOffset;
        public byte m_Endianess;
        internal string UnityVersion;
        public bool TypeTree = false;

        public List<SerializedType> Types = new();

        public string OwningBundle;

        public List<ObjectInfo> ObjectIndex;
        internal List<LocalSerializedObjectIdentifier> ScriptTypes;
        internal List<FileIdentifier> Externals;
        internal Dictionary<long, ObjectInfo> ObjectLookup = new();
        internal BuildTarget TargetPlatform;
        public BasedStream blockStream;

        internal bool Has(AssVer feature) => m_Version >= feature;

        public AssetContext Context;

        public ObjectInfo Resolve(long pathId)
        {
            return ObjectLookup[pathId];
        }
    }

    [Flags]
    public enum ArchiveFlags
    {
        CompressionTypeMask = 0x3f,
        BlocksAndDirectoryInfoCombined = 0x40,
        BlocksInfoAtTheEnd = 0x80,
        OldWebPluginCompatibility = 0x100,
        BlockInfoNeedPaddingAtStart = 0x200
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
        public byte[] Buffer = null;

        public long RawBegin;
        public long RawEnd => RawBegin + compressedSize;

        public long Begin;
        public long End => Begin + uncompressedSize;

        public bool Contains(long value)
        {
            return value < End && value >= Begin;
        }
    }

    public class Node
    {
        public long offset;
        public long size;
        public uint flags;
        public string path;
    }

    public enum EndianType { Big, Little }

    class RangeComparerer : IComparer<StorageBlock>
    {
        public int Compare(StorageBlock x, StorageBlock y)
        {
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
        public BinaryReader2 FileReader;

        private int _LastOpenedBlock = -1;

        private long _Length = 0;
        private long _Pos = 0;
        internal long RawBegin;

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


    public class BinaryReader2 : BinaryReader
    {
        public bool BigEndian = false;
        public BinaryReader2(System.IO.Stream stream, EndianType endian) : base(stream)
        {
            BigEndian = endian == EndianType.Big;
        }

        public void Align(int to)
        {
            long next = ((BaseStream.Position + to - 1) / to) * to;
            BaseStream.Position = next;
        }
        public override UInt16 ReadUInt16()
        {
            var data = base.ReadBytes(2);
            if (BigEndian)
                Array.Reverse(data);
            return BitConverter.ToUInt16(data, 0);
        }

        public override UInt32 ReadUInt32()
        {
            var data = base.ReadBytes(4);
            if (BigEndian)
                Array.Reverse(data);
            return BitConverter.ToUInt32(data, 0);
        }

        public override UInt64 ReadUInt64()
        {
            var data = base.ReadBytes(8);
            if (BigEndian)
                Array.Reverse(data);
            return BitConverter.ToUInt64(data, 0);
        }

        public override Int16 ReadInt16()
        {
            var data = base.ReadBytes(2);
            if (BigEndian)
                Array.Reverse(data);
            return BitConverter.ToInt16(data, 0);
        }

        public override Int32 ReadInt32()
        {
            var data = base.ReadBytes(4);
            if (BigEndian)
                Array.Reverse(data);
            return BitConverter.ToInt32(data, 0);
        }

        public override Int64 ReadInt64()
        {
            var data = base.ReadBytes(8);
            if (BigEndian)
                Array.Reverse(data);
            return BitConverter.ToInt64(data, 0);
        }

        public string ReadCString(int sofLimit = int.MaxValue, int hardLimit = 32000)
        {
            StringBuilder str = new();

            while (true)
            {
                var next = base.ReadByte();
                if (next == 0)
                    break;

                str.Append((char)next);

                if (str.Length >= sofLimit)
                    break;

                if (str.Length > hardLimit)
                    throw new Exception();
            }


            return str.ToString();
        }
    }

    public class VersionedReader : BinaryReader2
    {
        public readonly Version Version;
        public readonly BuildTarget Platform;
        public VersionedReader(ObjectInfo obj) : base(obj.Owner.blockStream.Based(obj), EndianType.Little)
        {
            Version = obj.Owner.Version;
            Platform = obj.Owner.TargetPlatform;
            Reset();
        }

        public VersionedReader(Version version, BuildTarget target, Stream stream, EndianType endian) : base(stream, endian)
        {
            Version = version;
            Platform = target;
            Reset();
        }

        public void Reset()
        {
            BaseStream.Position = 0;
        }
    }
    public class SerializedType
    {
        public int classID;
        public bool IsStrippedType;
        public short ScriptTypeIndex = -1;
        public List<TypeTreeNode> TypeTree;
        public byte[] ScriptID; //Hash128
        public byte[] OldTypeHash; //Hash128
        public int[] TypeDependencies;
        public string ClassName;
        public string NameSpace;
        public string AsmName;

    }


    public static class TypeReader
    {

        public static SerializedType ReadSerializedType(BinaryReader2 reader, AssetFile assets, bool isRefType)
        {
            var type = new SerializedType();
            type.classID = reader.ReadInt32();

            if (assets.Has(AssVer.RefactoredClassId))
            {
                type.IsStrippedType = reader.ReadBoolean();
            }

            if (assets.Has(AssVer.RefactorTypeData))
            {
                type.ScriptTypeIndex = reader.ReadInt16();
            }

            if (assets.Has(AssVer.HasTypeTreeHashes))
            {
                if (isRefType && type.ScriptTypeIndex >= 0)
                    type.ScriptID = reader.ReadBytes(16);
                else if ((!assets.Has(AssVer.RefactoredClassId) && type.classID < 0) || (assets.Has(AssVer.RefactoredClassId) && type.classID == 114))
                    type.ScriptID = reader.ReadBytes(16);

                type.OldTypeHash = reader.ReadBytes(16);
            }

            if (assets.TypeTree)
            {
                if (!assets.Has(AssVer.Unknown_12))
                    throw new Exception();

                type.TypeTree = ReadTypeTree(reader, assets, isRefType);

                if (assets.Has(AssVer.StoresTypeDependencies))
                {
                    if (isRefType)
                    {
                        type.ClassName = reader.ReadCString();
                        type.NameSpace = reader.ReadCString();
                        type.AsmName = reader.ReadCString();
                    }
                    else
                    {
                        type.TypeDependencies = reader.ReadInt32Array();
                    }
                }
            }

            return type;
        }

        private static List<TypeTreeNode> ReadTypeTree(BinaryReader2 reader, AssetFile assets, bool isRefType)
        {
            var nodes = new List<TypeTreeNode>();
            int nodeCount = reader.ReadInt32();
            int strBufferLen = reader.ReadInt32();
            nodes.Capacity = nodeCount;

            for (int n = 0; n < nodeCount; n++)
            {
                TypeTreeNode node = new();

                node.Version = reader.ReadUInt16();
                node.Level = reader.ReadByte();
                node.TypeFlags = reader.ReadByte();
                node.TypeStrOffset = reader.ReadUInt32();
                node.NameStrOffset = reader.ReadUInt32();
                node.ByteSize = reader.ReadInt32();
                node.Index = reader.ReadInt32();
                node.MetaFlag = reader.ReadInt32();
                if (assets.Has(AssVer.TypeTreeNodeWithTypeFlags))
                {
                    node.RefTypeHash = reader.ReadUInt64();
                }

                nodes.Add(node);
            }

            var strBuffer = reader.ReadBytes(strBufferLen);
            using var stringReader = new BinaryReader2(new MemoryStream(strBuffer), EndianType.Big);
            foreach (var node in nodes)
            {
                node.Type = ReadString(node.TypeStrOffset);
                node.Name = ReadString(node.NameStrOffset);
            }

            string ReadString(uint value)
            {
                var isOffset = (value & 0x80000000) == 0;
                if (isOffset)
                {
                    stringReader.BaseStream.Position = value;
                    return stringReader.ReadStringToNull();
                }
                var offset = value & 0x7FFFFFFF;
                return CommonString.Get(offset);
            }

            return nodes;
        }
    }
}
