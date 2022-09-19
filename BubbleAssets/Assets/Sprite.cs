using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace WikiGen.Assets
{
    public class SecondarySpriteTexture
    {
        public PPtr<Texture2D> texture;
        public string name;

        public SecondarySpriteTexture(AssetFileReader reader)
        {
            texture = reader.ReadPtr<Texture2D>(reader.File);
            name = reader.ReadCString();
        }
    }

    public enum SpritePackingRotation
    {
        None = 0,
        FlipHorizontal = 1,
        FlipVertical = 2,
        Rotate180 = 3,
        Rotate90 = 4
    };

    public enum SpritePackingMode
    {
        Tight = 0,
        Rectangle
    };

    public enum SpriteMeshType
    {
        FullRect,
        Tight
    };

    public class SpriteSettings
    {
        public uint settingsRaw;

        public uint packed;
        public SpritePackingMode packingMode;
        public SpritePackingRotation packingRotation;
        public SpriteMeshType meshType;

        public SpriteSettings(AssetFileReader reader)
        {
            settingsRaw = reader.ReadUInt32();

            packed = settingsRaw & 1; //1
            packingMode = (SpritePackingMode)(settingsRaw >> 1 & 1); //1
            packingRotation = (SpritePackingRotation)(settingsRaw >> 2 & 0xf); //4
            meshType = (SpriteMeshType)(settingsRaw >> 6 & 1); //1
            //reserved
        }
    }

    public class SpriteVertex
    {
        public Vector3 pos;

        public SpriteVertex(AssetFileReader reader)
        {
            pos = reader.ReadVector3();
        }
    }


    public class SpriteRenderData
    {
        public PPtr<Texture2D> texture;
        public PPtr<Texture2D> alphaTexture;
        public SecondarySpriteTexture[]? secondaryTextures;
        public SubMesh[] m_SubMeshes;
        public byte[] m_IndexBuffer;
        public VertexData m_VertexData;
        public Matrix4x4[]? m_Bindpose;
        public BoneWeights4[]? m_SourceSkin;
        public Rectf textureRect;
        public Vector2 textureRectOffset;
        public Vector2 atlasRectOffset;
        public SpriteSettings settingsRaw;
        public Vector4 uvTransform;
        public float downscaleMultiplier;

        //public SpriteVertex[] vertices;
        //public ushort[] indices;

        public SpriteRenderData(AssetFileReader reader)
        {
            var version = reader.Version;

            texture = reader.ReadPtr<Texture2D>();
            //if (version.Major > 5 || version.Major == 5 && version.Minor >= 2) //5.2 and up
            //{
                alphaTexture = reader.ReadPtr<Texture2D>();
            //}

            if (version.Major >= 2019) //2019 and up
            {
                var secondaryTexturesSize = reader.ReadInt32();
                secondaryTextures = new SecondarySpriteTexture[secondaryTexturesSize];
                for (int i = 0; i < secondaryTexturesSize; i++)
                {
                    secondaryTextures[i] = new SecondarySpriteTexture(reader);
                }
            }

            //if (version.Major > 5 || version.Major == 5 && version.Minor >= 6) //5.6 and up
            //{
                var m_SubMeshesSize = reader.ReadInt32();
                m_SubMeshes = new SubMesh[m_SubMeshesSize];
                for (int i = 0; i < m_SubMeshesSize; i++)
                {
                    m_SubMeshes[i] = new SubMesh(reader);
                }

                m_IndexBuffer = reader.ReadUInt8Array();
                reader.AlignStream();

                m_VertexData = new VertexData(reader);
            //}
            //else
            //{
            //    throw new Exception("unsupported version");
            //}

            if (version.Major >= 2018) //2018 and up
            {
                m_Bindpose = reader.ReadMatrixArray();

                if (version.Major == 2018 && version.Minor < 2) //2018.2 down
                {
                    var m_SourceSkinSize = reader.ReadInt32();
                    m_SourceSkin = new BoneWeights4[m_SourceSkinSize];
                    for (int i = 0; i < m_SourceSkinSize; i++)
                    {
                        m_SourceSkin[i] = new BoneWeights4(reader);
                    }
                }
            }

            textureRect = new Rectf(reader);
            textureRectOffset = reader.ReadVector2();
            atlasRectOffset = reader.ReadVector2();

            settingsRaw = new SpriteSettings(reader);
            uvTransform = reader.ReadVector4();

            if (version.Major >= 2017) //2017 and up
            {
                downscaleMultiplier = reader.ReadSingle();
            }
        }
    }

    public struct Rectf
    {
        public float x;
        public float y;
        public float width;
        public float height;

        public Rectf(AssetFileReader reader)
        {
            x = reader.ReadSingle();
            y = reader.ReadSingle();
            width = reader.ReadSingle();
            height = reader.ReadSingle();
        }
    }

    public sealed class Sprite : AssetObject {
        public Rectf m_Rect;
        public Vector2 m_Offset;
        public Vector4 m_Border;
        public float m_PixelsToUnits;
        public Vector2 m_Pivot = new Vector2(0.5f, 0.5f);
        public uint m_Extrude;
        public bool m_IsPolygon;
        public KeyValuePair<Guid, long> m_RenderDataKey;

        public string[]? m_AtlasTags;
        public PPtr<SpriteAtlas>? m_SpriteAtlas;
        public Vector2[][]? m_PhysicsShape;

        public SpriteRenderData m_RD;
        public string Name;

        public Sprite(AssetFileReader reader) : base(reader.File)
        {
            Name = reader.ReadAlignedString();
            var version = reader.Version;
            m_Rect = new Rectf(reader);
            m_Offset = reader.ReadVector2();
            m_Border = reader.ReadVector4();

            m_PixelsToUnits = reader.ReadSingle();
            m_Pivot = reader.ReadVector2();

            m_Extrude = reader.ReadUInt32();
            m_IsPolygon = reader.ReadBoolean();
            reader.AlignStream();

            if (version.Major >= 2017) //2017 and up
            {
                var first = new Guid(reader.ReadBytes(16));
                var second = reader.ReadInt64();
                m_RenderDataKey = new KeyValuePair<Guid, long>(first, second);

                m_AtlasTags = reader.ReadStringArray();

                m_SpriteAtlas = reader.ReadPtr<SpriteAtlas>();
            }

            m_RD = new SpriteRenderData(reader);

            if (version.Major >= 2017) //2017 and up
            {
                var m_PhysicsShapeSize = reader.ReadInt32();
                m_PhysicsShape = new Vector2[m_PhysicsShapeSize][];
                for (int i = 0; i < m_PhysicsShapeSize; i++)
                {
                    m_PhysicsShape[i] = reader.ReadVector2Array();
                }
            }

            //vector m_Bones 2018 and up
        }
    }
}
