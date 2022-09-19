using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using WikiGen.Assets;

namespace BubbleAssets
{
    public class BlueprintReferencedAssets
    {
        public List<BlueprintAssetReference> m_Entries { get; set; } = new();
    }
    public class BlueprintAssetReference
    {
        public string AssetId { get; set; }
        public long FileId { get; set; }
        public UnityAssetReference Asset { get; set; }
        public BlueprintAssetReference(string AssetId, long FileId, UnityAssetReference Asset)
        {
            this.AssetId = AssetId;
            this.FileId = FileId;
            this.Asset = Asset;
        }
    }
    public class UnityAssetReference
    {
        public int m_FileID { get; set; }
        public long m_PathID { get; set; }

        public UnityAssetReference(int m_FileID, long m_PathID)
        {
            this.m_FileID = m_FileID;
            this.m_PathID = m_PathID;
        }
    }

    public class BlueprintAssetsContext
    {
        public Dictionary<string, UnityAssetReference> refToAsset = new();

        public BlueprintAssetsContext(string path)
        {
            LoadFrom(path);
        }

        public void LoadFrom(string path)
        {
            var references = JsonSerializer.Deserialize<BlueprintReferencedAssets>(File.ReadAllText(path));
            if (references is null) return;
            

            foreach (var entry in references.m_Entries)
                refToAsset[entry.AssetId] = entry.Asset;

        }

        public bool TryGetValue(string request, [NotNullWhen(true)] out UnityAssetReference? assetRef) => refToAsset.TryGetValue(request, out assetRef);

        private static Dictionary<string, byte[]> atlasCache = new();

        public bool TryRenderSprite(Sprite sprite, [NotNullWhen(true)] out Bitmap? image)
        {
            Rectf cut = sprite.m_Rect;
            Texture2D tex;
            string? atlasKey = null;

            if (sprite.m_SpriteAtlas?.Identifier != 0)
            {
                var atlas = sprite.m_SpriteAtlas!.Object;

                if (!atlas.m_RenderDataMap.TryGetValue(sprite.m_RenderDataKey, out var spriteAtlasData))
                {
                    Console.Error.WriteLine($"atlased sprite does not have atlasData");
                    image = null;
                    return false;
                }

                cut = spriteAtlasData.textureRect;
                tex = spriteAtlasData.texture.Object;
                atlasKey = $"{spriteAtlasData.texture.FileIndex}/{spriteAtlasData.texture.Identifier}";
            }
            else if (sprite.m_RD.texture.Identifier != 0)
            {
                tex = sprite.m_RD.texture.Object;
            }
            else
            {
                throw new Exception("No texture source for sprite");
            }

            AssetFileReader resourceStream;

            if (tex.m_StreamData?.path != null)
            {
                resourceStream = new(
                    tex.Owner,
                    stream: tex.Owner.Context.resourceStreams[Path.GetFileName(tex.m_StreamData.path)].Based(tex.m_StreamData.offset, tex.m_StreamData.size),
                    endian: EndianType.Little);
            }
            else
            {
                resourceStream = new(
                    tex.Owner,
                    stream: tex.Owner.blockStream.Based(tex.Owner.blockStream.Position, tex.image_data_size),
                    endian: EndianType.Little);
            }

            var converter = new Texture2DConverter(resourceStream, tex);

            bool returnBuf = true;
            byte[]? buff = null;
            try
            {
                if (atlasKey != null)
                {
                    if (!atlasCache.TryGetValue(atlasKey, out buff))
                    {
                        buff = BigArrayPool<byte>.Shared.Rent(tex.m_Width * tex.m_Height * 4);
                        converter.DecodeTexture2D(buff);
                        atlasCache[atlasKey] = buff;
                    }

                    returnBuf = false;
                }
                else
                {
                    buff = BigArrayPool<byte>.Shared.Rent(tex.m_Width * tex.m_Height * 4);
                    converter.DecodeTexture2D(buff);
                }


                const PixelFormat format = PixelFormat.Format32bppArgb;
                image = new Bitmap((int)cut.width, (int)cut.height, format);
                Rectangle rect = new(0, 0, image.Width, image.Height);

                var data = image.LockBits(rect, ImageLockMode.ReadWrite, format);
                IntPtr ptr = data.Scan0;
                if (cut.x != 0 || cut.y != 0 || cut.width != tex.m_Width || cut.height != tex.m_Height)
                {
                    for (int ly = 0; ly < cut.height; ly++)
                    {
                        int input_y = (int)(ly + cut.y);
                        int input_x = (int)cut.x;

                        Marshal.Copy(buff, (int)((input_y * tex.m_Width + input_x) * 4), ptr + ly * data.Stride, data.Stride);
                    }
                }
                else
                {
                    Marshal.Copy(buff, 0, ptr, data.Stride * data.Height);
                }
                image.UnlockBits(data);
                image.RotateFlip(RotateFlipType.RotateNoneFlipY);
            }
            finally
            {
                if (buff != null && returnBuf)
                    BigArrayPool<byte>.Shared.Return(buff);
            }

            return true;
        }



    }

}
