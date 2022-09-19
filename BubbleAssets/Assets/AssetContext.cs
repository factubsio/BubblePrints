using K4os.Compression.LZ4;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WikiGen.Assets
{
    public class AssetContext
    {
        public readonly Dictionary<string, string> cabToBundle = new();
        public readonly Dictionary<string, BasedStream> resourceStreams = new();
        public readonly Dictionary<string, AssetFile> assetIndex = new();
        public readonly Dictionary<string, List<AssetFile>> assetsByBundle = new();

        private static bool SkipFX(string path)
        {
            if (!path.EndsWith(".fx")) return false;
            if (path.EndsWith("object_fadein.fx")) return false;
            if (path.EndsWith("object_fadeout.fx")) return false;
            return true;
        }

        public void AddDirectory(string dir)
        {
            Stopwatch now = new();
            int totalBundleCount = 0;
            now.Start();
            List<Task> tasks = new();
            foreach (var path in Directory.EnumerateFiles(dir))
            {
                if (path.EndsWith(".unit")) continue;
                if (path.EndsWith(".scene")) continue;
                if (path.EndsWith(".scenes")) continue;
                if (SkipFX(path)) continue;
                if (path.EndsWith(".ee")) continue;
                if (path.EndsWith(".terrainlayers")) continue;
                if (path.EndsWith(".nav")) continue;
                if (path.EndsWith(".animations")) continue;

                tasks.Add(Task.Run(() =>
                {
                    AddBundle(path);
                }));
                totalBundleCount++;
            }

            Task.WaitAll(tasks.ToArray());

            List<(string, FileIdentifier)> unresolved = new();

            int totalExternal = 0;

            foreach (var assets in assetIndex.Values)
            {
                foreach (var external in assets.Externals)
                {
                    totalExternal++;
                    if (assetIndex.TryGetValue(external.fileName, out var externalResolved))
                        external.Resolved = externalResolved;
                    else
                        unresolved.Add((assets.OwningBundle, external));
                }
            }

            Console.WriteLine($"could not resolve {unresolved.Count} bundles (out of {totalExternal} total");


            now.Stop();
            Console.WriteLine($"Indexed {totalBundleCount} bundles " +
                $"({assetIndex.Values.Sum(x => x.ObjectIndex.Count)} assets and {resourceStreams.Count} resS) " +
                $"in {now.Elapsed.TotalMilliseconds / 1000.0} seconds");

            //foreach (var (bundle, link) in unresolved)
            //{
            //    Console.WriteLine($"could not resolve: {bundle} -> {link.fileName}");
            //}
        }

        public void AddBundle(string path)
        {
            var stream = File.OpenRead(path);
            var reader = new BinaryReaderWithEndian(stream, EndianType.Big);
            var sig = reader.ReadCString(10);

            if (sig != "UnityFS")
            {
                Console.WriteLine("Not a bundle: " + Path.GetFileName(path));
                return;
            }

            var version = reader.ReadUInt32();
            var playerVersion = reader.ReadCString();
            var playerRev = reader.ReadCString();

            var size = reader.ReadInt64();
            var blockInfoSize_Compressed = reader.ReadUInt32();
            var blockInfoSize_Uncompressed = reader.ReadUInt32();

            var flags = reader.ReadUInt32();

            //Console.WriteLine($"Bundle: " + Path.GetFileName(path));

            if (version >= 7)
            {
                reader.Align(16);
            }

            var blockBytes_Compressed = reader.ReadBytes((int)blockInfoSize_Compressed);
            var blockBytes_Uncompressed = new byte[blockInfoSize_Uncompressed];

            int lz4Size = LZ4Codec.Decode(blockBytes_Compressed, blockBytes_Uncompressed);
            if (lz4Size != blockInfoSize_Uncompressed)
            {
                throw new Exception();
            }

            var blockInfoStream = new MemoryStream(blockBytes_Uncompressed);
            var blockStream = new BundleBlockStream(reader);

            using (var blocksInfoReader = new BinaryReaderWithEndian(blockInfoStream, EndianType.Big))
            {
                var uncompressedDataHash = blocksInfoReader.ReadBytes(16);
                var blocksInfoCount = blocksInfoReader.ReadInt32();
                for (int i = 0; i < blocksInfoCount; i++)
                {
                    blockStream.AddBlock(new StorageBlock
                    {
                        uncompressedSize = blocksInfoReader.ReadUInt32(),
                        compressedSize = blocksInfoReader.ReadUInt32(),
                        flags = (StorageBlockFlags)blocksInfoReader.ReadUInt16()
                    });
                }

                var nodesCount = blocksInfoReader.ReadInt32();
                for (int i = 0; i < nodesCount; i++)
                {
                    blockStream.DirectoryInfo.Add(new(
                        offset: blocksInfoReader.ReadInt64(),
                        size: blocksInfoReader.ReadInt64(),
                        flags: blocksInfoReader.ReadUInt32(),
                        path: blocksInfoReader.ReadCString()));

                }
            }

            blockStream.RawBegin = reader.BaseStream.Position;

            List<AssetFile> assetList = new();

            foreach (var dir in blockStream.DirectoryInfo)
            {
                lock (cabToBundle)
                {
                    cabToBundle[dir.path] = Path.GetFileName(path);
                }

                var dirReader = new BinaryReaderWithEndian(blockStream, EndianType.Big);
                blockStream.Position = dir.offset;

                if (AssetFile.TryRead(dir, dirReader, blockStream, path, this, out var assetFile))
                {

                    assetList.Add(assetFile);

                    lock (assetIndex)
                    {
                        assetIndex[dir.path] = assetFile;
                        assetsByBundle[Path.GetFileName(path)] = assetList;
                    }
                }
                else
                {
                    lock (resourceStreams)
                    {
                        resourceStreams[dir.path] = new(blockStream, dir.offset, dir.size);
                    }
                }
            }
        }


    }
    public class Node
    {
        public readonly long offset;
        public readonly long size;
        public readonly uint flags;
        public readonly string path;

        public Node(long offset, long size, uint flags, string path)
        {
            this.offset = offset;
            this.size = size;
            this.flags = flags;
            this.path = path;
        }
    }

}
