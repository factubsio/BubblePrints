using BubbleAssets;
using K4os.Compression.LZ4;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace WikiGen.Assets
{
    public class AssetFile
    {
        public override string ToString() => $"<{OwningBundle}/asset-file, {ObjectIndex.Count} assets>";


        public Version Version = new();
        public uint m_MetadataSize;
        public long m_FileSize;
        public AssVer m_Version;
        public long m_DataOffset;
        public byte m_Endianess;
        internal string UnityVersion = "";
        public bool TypeTree = false;


        public readonly List<SerializedType> Types = new();
        public readonly List<ObjectInfo> ObjectIndex = new();
        public readonly List<PPtr<object>> ScriptTypes = new();
        public readonly List<FileIdentifier> Externals = new();
        public readonly Dictionary<long, ObjectInfo> ObjectLookup = new();

        public readonly string OwningBundle;
        public readonly AssetContext Context;
        public readonly BasedStream blockStream;

        public BuildTarget TargetPlatform;


        internal bool Has(AssVer feature) => m_Version >= feature;


        public AssetFile(BasedStream blockStream, string OwningBundle, AssetContext Context)
        {
            this.blockStream = blockStream;
            this.OwningBundle = OwningBundle;
            this.Context = Context;
        }

        public ObjectInfo LookupObject(long pathId)
        {
            return ObjectLookup[pathId];
        }

        internal static bool TryRead(
            Node node,
            BinaryReaderWithEndian reader,
            BundleBlockStream blockStream,
            string path,
            AssetContext assetContext,
            [NotNullWhen(returnValue: true)] out AssetFile? assets)
        {

            if (node.size < 20)
            {
                assets = null;
                return false;
            }

            assets = new(
                blockStream: new(blockStream, node.offset, node.size),
                OwningBundle: Path.GetFileName(path),
                Context: assetContext);

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
            assets.ObjectIndex.EnsureCapacity(objectCount);
            assets.ObjectLookup.EnsureCapacity(objectCount);

            for (int i = 0; i < objectCount; i++)
            {
                ObjectInfo obj = new(assets);

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
            assets.ScriptTypes.EnsureCapacity(scriptCount);
            for (int i = 0; i < scriptCount; i++)
            {
                var scriptType = reader.ReadPtr<object>(assets);
                //scriptType.SourceFile = assets;
                //scriptType.FileIndex = reader.ReadInt32();
                //reader.AlignStream();
                //scriptType.Identifier = reader.ReadInt64();
                assets.ScriptTypes.Add(scriptType);
            }

            int externalsCount = reader.ReadInt32();
            assets.Externals.EnsureCapacity(externalsCount);
            for (int i = 0; i < externalsCount; i++)
            {
                reader.ReadStringToNull();

                var externalFile = new FileIdentifier(
                    guid: new Guid(reader.ReadBytes(16)),
                    type: reader.ReadInt32(),
                    pathName: reader.ReadStringToNull());
                assets.Externals.Add(externalFile);
            }

            return true;
        }


    }

    //[Flags]
    //public enum ArchiveFlags
    //{
    //    CompressionTypeMask = 0x3f,
    //    BlocksAndDirectoryInfoCombined = 0x40,
    //    BlocksInfoAtTheEnd = 0x80,
    //    OldWebPluginCompatibility = 0x100,
    //    BlockInfoNeedPaddingAtStart = 0x200
    //}

    public class AssetFileReader : BinaryReaderWithEndian
    {
        public readonly AssetFile File;
        public Version Version => File.Version;
        public BuildTarget Platform => File.TargetPlatform;
        public AssetFileReader(ObjectInfo obj) : base(obj.Owner.blockStream.Based(obj), EndianType.Little)
        {
            File = obj.Owner;
            Reset();
        }

        public AssetFileReader(AssetFile file, Stream stream, EndianType endian) : base(stream, endian)
        {
            File = file;
            Reset();
        }

        public void Reset()
        {
            BaseStream.Position = 0;
        }
    }

    public class FileIdentifier
    {
        public readonly Guid guid;
        public readonly int type; //enum { kNonAssetType = 0, kDeprecatedCachedAssetType = 1, kSerializedAssetType = 2, kMetaAssetType = 3 };
        public readonly string pathName;

        public FileIdentifier(Guid guid, int type, string pathName)
        {
            this.guid = guid;
            this.type = type;
            this.pathName = pathName;
            this.fileName = Path.GetFileName(pathName);
        }

        public readonly string fileName;

        public AssetFile? Resolved;
    }
    public interface IAssetMaterializer<T>
    {
        public T Create(ObjectInfo info);
    }
    public class AssetMaterializer : IAssetMaterializer<object>, IAssetMaterializer<Texture2D>, IAssetMaterializer<Sprite>, IAssetMaterializer<SpriteAtlas>
    {
        object IAssetMaterializer<object>.Create(ObjectInfo info) => null!;

        private static void CheckType(ObjectInfo info, ClassIDType expected)
        {
            if (info.ClassType != expected)
                throw new Exception($"ClassType ({info.ClassType}) does not match expected ({expected})");
        }

        Texture2D IAssetMaterializer<Texture2D>.Create(ObjectInfo info)
        {
            CheckType(info, ClassIDType.Texture2D);
            return new(new(info));
        }

        Sprite IAssetMaterializer<Sprite>.Create(ObjectInfo info)
        {
            CheckType(info, ClassIDType.Sprite);
            return new(new(info));
        }

        SpriteAtlas IAssetMaterializer<SpriteAtlas>.Create(ObjectInfo info)
        {
            CheckType(info, ClassIDType.SpriteAtlas);
            return new(new(info));
        }
    }

    public class PPtr<T>
    {
        public readonly int FileIndex;
        public readonly long Identifier;
        public readonly AssetFile SourceFile;

        private T? _Materialized;

        public PPtr(int fileIndex, long identifier, AssetFile sourceFile)
        {
            FileIndex = fileIndex;
            Identifier = identifier;
            SourceFile = sourceFile;
        }

        public PPtr(UnityAssetReference assetRef, AssetFile source)
        {
            FileIndex = assetRef.m_FileID;
            Identifier = assetRef.m_PathID;
            SourceFile = source;
        }

        //public PPtr()

        public override string ToString() => $"file:{FileIndex} id:{Identifier}";

        public ObjectInfo RawInfo
        {
            get
            {
                AssetFile sourceFile;
                if (FileIndex == 0)
                {
                    sourceFile = SourceFile;
                }
                else
                {
                    sourceFile = SourceFile.Externals[FileIndex - 1].Resolved ?? throw new ArgumentNullException("Could not find resolved external file");
                }

                return sourceFile.LookupObject(Identifier);
            }
        }

        public T Object => _Materialized ??= (new AssetMaterializer() as IAssetMaterializer<T> ?? throw new Exception()).Create(RawInfo);
    }
    public abstract class AssetObject
    {
        public readonly AssetFile Owner;

        protected AssetObject(AssetFile owner)
        {
            Owner = owner;
        }
    }
}
