using K4os.Compression.LZ4;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static WikiGen.Program;

namespace WikiGen.Assets
{
    public class AssetContext
    {
        public readonly Dictionary<string, string> cabToBundle = new();
        public readonly Dictionary<string, BasedStream> resourceStreams = new();
        public readonly Dictionary<string, AssetFile> assetIndex = new();
        public readonly Dictionary<string, List<AssetFile>> assetsByBundle = new();

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
                if (path.EndsWith(".fx")) continue;
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

            foreach (var assets in assetIndex.Values)
            {
                foreach (var external in assets.Externals)
                {
                    if (assetIndex.TryGetValue(external.fileName, out var externalResolved))
                        external.Resolved = externalResolved;
                    else
                        unresolved.Add((assets.OwningBundle, external));
                }
            }


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
            var reader = new BinaryReader2(stream, EndianType.Big);
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
            var blockStream = new BundleBlockStream();

            using (var blocksInfoReader = new BinaryReader2(blockInfoStream, EndianType.Big))
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
                    blockStream.DirectoryInfo.Add(new Node
                    {
                        offset = blocksInfoReader.ReadInt64(),
                        size = blocksInfoReader.ReadInt64(),
                        flags = blocksInfoReader.ReadUInt32(),
                        path = blocksInfoReader.ReadCString(),
                    });

                }
            }

            blockStream.FileReader = reader;
            blockStream.RawBegin = reader.BaseStream.Position;

            List<AssetFile> assetList = new();

            foreach (var dir in blockStream.DirectoryInfo)
            {
                lock (cabToBundle)
                {
                    cabToBundle[dir.path] = Path.GetFileName(path);
                }

                var dirReader = new BinaryReader2(blockStream, EndianType.Big);
                blockStream.Position = dir.offset;

                if (TryReadAssetsFile(dir, dirReader, out var assetFile))
                {
                    assetFile.blockStream = new(blockStream, dir.offset, dir.size);
                    assetFile.OwningBundle = Path.GetFileName(path);
                    assetFile.Context = this;

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

        private static bool TryReadAssetsFile(Node node, BinaryReader2 reader, out AssetFile assets)
        {
            assets = new();
            if (node.size < 20)
                return false;

            assets.m_MetadataSize = reader.ReadUInt32();
            assets.m_FileSize = reader.ReadUInt32();
            assets.m_Version = (AssVer)reader.ReadUInt32();
            assets.m_DataOffset = reader.ReadUInt32();
            assets.m_Endianess = reader.ReadByte();
            //Alignment
            reader.ReadBytes(3);

            if (assets.Has(AssVer.LargeFilesSupport))
            {
                if (node.size < 48)
                    return false;
                assets.m_MetadataSize = reader.ReadUInt32();
                assets.m_FileSize = reader.ReadInt64();
                assets.m_DataOffset = reader.ReadInt64();

                //Unknown
                reader.ReadInt64();
            }

            if (assets.m_FileSize != node.size)
                return false;
            if (assets.m_DataOffset >= node.size)
                return false;

            if (assets.m_Endianess == 0)
                reader.BigEndian = false;

            if (assets.Has(AssVer.Unknown_7))
            {
                assets.UnityVersion = reader.ReadCString();
                //Console.WriteLine("unity version: " + assets.UnityVersion);

                var buildSplit = Regex.Replace(assets.UnityVersion, @"\d", "").Split(new[] { "." }, StringSplitOptions.RemoveEmptyEntries);
                //var buildType = new BuildType(buildSplit[0]);
                var versionSplit = Regex.Replace(assets.UnityVersion, @"\D", ".").Split(new[] { "." }, StringSplitOptions.RemoveEmptyEntries);
                int[] versionComponents = versionSplit.Select(int.Parse).ToArray();
                assets.Version = new(versionComponents[0], versionComponents[1], versionComponents[2], versionComponents[3]);
            }

            if (assets.Has(AssVer.Unknown_8))
            {
                assets.TargetPlatform = (BuildTarget)reader.ReadInt32();
                if (!Enum.IsDefined(typeof(BuildTarget), assets.TargetPlatform))
                {
                    assets.TargetPlatform = BuildTarget.UnknownPlatform;
                }
            }

            if (assets.Has(AssVer.HasTypeTreeHashes))
            {
                assets.TypeTree = reader.ReadBoolean();
            }

            int typeCount = reader.ReadInt32();
            //Console.WriteLine("type count: " + typeCount);
            for (int i = 0; i < typeCount; i++)
            {
                SerializedType type = TypeReader.ReadSerializedType(reader, assets, false);

                assets.Types.Add(type);
            }

            if (assets.Has(AssVer.Unknown_7) && !assets.Has(AssVer.Unknown_14))
            {
                throw new Exception("unsupported version");
            }

            if (!assets.Has(AssVer.RefactorTypeData))
                throw new Exception("unsupported bundle version");


            int objectCount = reader.ReadInt32();
            //Console.WriteLine("Object count: " + objectCount);
            assets.ObjectIndex = new(objectCount);
            assets.ObjectLookup.EnsureCapacity(objectCount);

            for (int i = 0; i < objectCount; i++)
            {
                ObjectInfo obj = new();

                obj.Owner = assets;

                reader.AlignStream();
                obj.m_PathID = reader.ReadInt64();

                if (assets.Has(AssVer.LargeFilesSupport))
                    obj.byteStart = reader.ReadInt64();
                else
                    obj.byteStart = reader.ReadUInt32();

                obj.byteStart += assets.m_DataOffset;
                obj.byteSize = reader.ReadUInt32();
                obj.typeID = reader.ReadInt32();

                if (!assets.Has(AssVer.RefactoredClassId))
                    throw new Exception("unsupported bundle version");

                var type = assets.Types[obj.typeID];
                obj.serializedType = type;
                obj.classID = type.classID;


                assets.ObjectIndex.Add(obj);
                assets.ObjectLookup[obj.m_PathID] = obj;
            }

            int scriptCount = reader.ReadInt32();
            assets.ScriptTypes = new List<LocalSerializedObjectIdentifier>(scriptCount);
            for (int i = 0; i < scriptCount; i++)
            {
                var scriptType = new LocalSerializedObjectIdentifier();
                scriptType.FileIndex = reader.ReadInt32();
                reader.AlignStream();
                scriptType.Identifier = reader.ReadInt64();
                assets.ScriptTypes.Add(scriptType);
            }

            int externalsCount = reader.ReadInt32();
            assets.Externals = new List<FileIdentifier>(externalsCount);
            for (int i = 0; i < externalsCount; i++)
            {
                var externalFile = new FileIdentifier();
                reader.ReadStringToNull();
                externalFile.guid = new Guid(reader.ReadBytes(16));
                externalFile.type = reader.ReadInt32();
                externalFile.pathName = reader.ReadStringToNull();
                externalFile.fileName = Path.GetFileName(externalFile.pathName);
                assets.Externals.Add(externalFile);
            }

            return true;
        }

    }
}
