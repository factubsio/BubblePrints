namespace WikiGen.Assets
{
    public class SerializedType
    {
        public int classID;
        public bool IsStrippedType;
        public short ScriptTypeIndex = -1;
        public List<TypeTreeNode> TypeTree = new();
        public byte[]? ScriptID; //Hash128
        public byte[]? OldTypeHash; //Hash128
        public int[]? TypeDependencies;
        public string? ClassName;
        public string? NameSpace;
        public string? AsmName;
    }
    public class TypeTreeNode
    {
        public string Type = "";
        public string Name = "";

        public readonly int ByteSize;
        public readonly int Index;
        public readonly int TypeFlags; //IsArray
        public readonly int Version;
        public readonly int MetaFlag;
        public readonly int Level;
        public readonly uint TypeStrOffset;
        public readonly uint NameStrOffset;
        public ulong RefTypeHash;

        public TypeTreeNode(ushort Version, byte Level, byte TypeFlags, uint TypeStrOffset, uint NameStrOffset, int ByteSize, int Index, int MetaFlag)
        {
            this.Version = Version;
            this.Level = Level;
            this.TypeFlags = TypeFlags;
            this.TypeStrOffset = TypeStrOffset;
            this.NameStrOffset = NameStrOffset;
            this.ByteSize = ByteSize;
            this.Index = Index;
            this.MetaFlag = MetaFlag;
        }
    }

    public static class TypeReader
    {

        public static SerializedType ReadSerializedType(BinaryReaderWithEndian reader, AssetFile assets, bool isRefType)
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

                ReadTypeTree(reader, assets, isRefType, type.TypeTree);

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

        private static List<TypeTreeNode> ReadTypeTree(BinaryReaderWithEndian reader, AssetFile assets, bool isRefType, List<TypeTreeNode> nodes)
        {
            int nodeCount = reader.ReadInt32();
            int strBufferLen = reader.ReadInt32();
            nodes.EnsureCapacity(nodeCount);

            for (int n = 0; n < nodeCount; n++)
            {
                TypeTreeNode node = new(
                    Version: reader.ReadUInt16(),
                    Level: reader.ReadByte(),
                    TypeFlags: reader.ReadByte(),
                    TypeStrOffset: reader.ReadUInt32(),
                    NameStrOffset: reader.ReadUInt32(),
                    ByteSize: reader.ReadInt32(),
                    Index: reader.ReadInt32(),
                    MetaFlag: reader.ReadInt32());

                if (assets.Has(AssVer.TypeTreeNodeWithTypeFlags))
                {
                    node.RefTypeHash = reader.ReadUInt64();
                }

                nodes.Add(node);
            }

            var strBuffer = reader.ReadBytes(strBufferLen);
            using var stringReader = new BinaryReaderWithEndian(new MemoryStream(strBuffer), EndianType.Big);
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
