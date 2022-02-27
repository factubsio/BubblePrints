using K4os.Compression.LZ4;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BlueprintExplorer
{
    public enum ChunkTypes
    {
        Header = 32,
        Blueprints,
        Strings,
        TypeNames,
        ComponentNames,
        Defaults,
    }

    public struct ChunkSubTypes
    {
        public enum Blueprints
        {
            References = 1,
            Components,
        }
    }

    public class Chunk
    {
        public UInt16 Id;
        public UInt16 SubType;
        public UInt32 Length;
        public UInt32 CompressedLength;
        public UInt32 Hash;

        public long FileLength => CompressedLength > 0 ? CompressedLength : Length;

        public long FileOffset;
    }

    public class CompositeChunk
    {
        public readonly List<Chunk> Chunks = new();

        public Chunk ForSubType(ushort sub) => Chunks.FirstOrDefault(ch => ch.SubType == sub);

        public Chunk Main => ForSubType(0);
    }

    public class BPFile
    {
        private readonly Dictionary<UInt16, List<CompositeChunk>> Contents = new();

        public const int BUFFER_SIZE = 64_000_000;

        private void Add(Chunk chunk)
        {
            if (!Contents.TryGetValue(chunk.Id, out var list))
            {
                list = new();
                Contents[chunk.Id] = list;
            }

            if (chunk.SubType == 0)
                list.Add(new());

            list[^1].Chunks.Add(chunk);
        }

        public IEnumerable<CompositeChunk> GetChunks(UInt16 type)
        {
            if (Contents.TryGetValue(type, out var list))
                return list;

            return Enumerable.Empty<CompositeChunk>();
        }

        public CompositeChunk GetChunk(UInt16 type) => GetChunks(type).FirstOrDefault();

        public class ChunkStream
        {
            private readonly byte[] Buffer;
            private readonly MemoryStream BufferAsStream;
            public readonly BinaryWriter Writer;

            public ReadOnlySpan<byte> Span => Buffer.AsSpan<byte>(0, (int)Writer.BaseStream.Position);

            public ChunkStream()
            {
                Buffer = new byte[BUFFER_SIZE];
                BufferAsStream = new(Buffer, true);
                Writer = new(BufferAsStream);
            }
            public void Reset()
            {
                Writer.BaseStream.Position = 0;
            }
        }

        public class ChunkWriter : IDisposable
        {
            private readonly UInt16 Id;
            private readonly BPWriter Parent;

            private static readonly List<ChunkStream> StreamPool = new();

            private readonly Dictionary<ushort, ChunkStream> ActiveStreams = new();

            public ChunkWriter(UInt16 id, BPWriter parent)
            {
                Id = id;
                Parent = parent;
            }

            public BinaryWriter GetStream(ushort type)
            {
                if (!ActiveStreams.TryGetValue(type, out var stream))
                {
                    if (StreamPool.Count == 0)
                        stream = new();
                    else
                    {
                        stream = StreamPool[^1];
                        StreamPool.RemoveAt(StreamPool.Count - 1);
                    }
                    stream.Reset();
                    ActiveStreams[type] = stream;
                }
                return stream.Writer;
            }
            public BinaryWriter Stream => GetStream(0);

            public void Dispose() {
                foreach (var stream in ActiveStreams.OrderBy(kv => kv.Key))
                    Parent.Write(Id, stream.Key, stream.Value.Span);
                StreamPool.AddRange(ActiveStreams.Values);
            }
        }

        public class BPWriter : IDisposable
        {
            private readonly BinaryWriter Stream;

            private readonly byte[] CompressionBuffer = new byte[BUFFER_SIZE];

            public void Dispose() => Stream.Dispose();

            public BPWriter(string path)
            {
                Stream = new BinaryWriter(File.OpenWrite(path));
                Stream.BaseStream.SetLength(0);
            }

            public ChunkWriter Begin(UInt16 type) => new(type, this);

            public void Write(UInt16 id, UInt16 sub, ReadOnlySpan<byte> data)
            {
                Stream.Write(id);
                Stream.Write(sub);
                Stream.Write((UInt32)data.Length);

                int compressedLength = 0;

                if (data.Length > 1024)
                {
                    compressedLength = LZ4Codec.Encode(data, CompressionBuffer.AsSpan<byte>(), LZ4Level.L10_OPT);
                }

                Stream.Write((UInt32)compressedLength);
                Stream.Write((UInt32)0);
                if (compressedLength > 0)
                    Stream.Write(CompressionBuffer, 0, compressedLength);
                else
                    Stream.Write(data);
            }
        }

        public class ReadContext : IDisposable
        {
            private readonly byte[] Data;

            private byte[] BounceBuffer = new byte[BUFFER_SIZE];

            public ReadContext(byte[] data) {
                Data = data;
            }

            public ReadOnlyMemory<byte> OpenRaw(Chunk chunk)
            {
                if (chunk == null)
                    return null;

                if (chunk.CompressedLength > 0)
                {
                    LZ4Codec.Decode(Data, (int)chunk.FileOffset, (int)chunk.CompressedLength, BounceBuffer, 0, BUFFER_SIZE);
                    return new(BounceBuffer, 0, (int)chunk.Length);
                }
                else
                {
                    return new(Data, (int)chunk.FileOffset, (int)chunk.Length);
                }

            }

            public BinaryReader Open(Chunk chunk)
            {
                if (chunk == null)
                    return null;
                var raw = OpenRaw(chunk);
                if (raw.IsEmpty)
                    return null;

                if (MemoryMarshal.TryGetArray(raw, out var array))
                    return new(new MemoryStream(array.Array, array.Offset, array.Count));
                throw new Exception("WTF???");
            }


            public void Dispose() => BounceBuffer = null;
        }

        public class Reader
        {
            private readonly byte[] Data;
            public BPFile Handle { get; private set; }
            public Reader(string path)
            {
                Handle = new();
                Data = File.ReadAllBytes(path);
                using var scanner = new BinaryReader(new MemoryStream(Data));

                while (scanner.BaseStream.Position < scanner.BaseStream.Length)
                {
                    Chunk chunk = new();
                    chunk.Id = scanner.ReadUInt16();
                    chunk.SubType = scanner.ReadUInt16();
                    chunk.Length = scanner.ReadUInt32();
                    chunk.CompressedLength = scanner.ReadUInt32();
                    chunk.Hash = scanner.ReadUInt32();

                    chunk.FileOffset = scanner.BaseStream.Position;

                    Handle.Add(chunk);

                    scanner.BaseStream.Seek(chunk.FileLength, SeekOrigin.Current);
                }
            }

            public ReadContext CreateReadContext()
            {
                return new(Data);
            }

            internal CompositeChunk Get(ushort typeNames) => Handle.GetChunk(typeNames);
            internal IEnumerable<CompositeChunk> GetAll(ushort typeNames) => Handle.GetChunks(typeNames);
        }
    }

}
