using System;

namespace WikiGen.Assets
{
    public class ObjectInfo
    {
        public readonly AssetFile Owner;

        public ObjectInfo(AssetFile owner)
        {
            Owner = owner;
        }

        public ClassIDType ClassType
        {
            get
            {

                ClassIDType objType = ClassIDType.UnknownType;

                if (Enum.IsDefined(typeof(ClassIDType), classID))
                {
                    objType = (ClassIDType)classID;
                }

                return objType;
            }
        }

        public long byteStart;
        public uint byteSize;
        public int typeID;
        public int classID;
        public ushort isDestroyed;
        public byte stripped;

        public long m_PathID;
        public SerializedType serializedType = new();
    }
}
