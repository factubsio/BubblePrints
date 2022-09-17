using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace FontTested
{
    public class SaberRenderer : IDisposable
    {
        private readonly Bitmap atlas;
        private readonly BitmapData atlasData;
        private readonly Dictionary<char, BubbleGlyph> glyphs = new();

        public SaberRenderer(Bitmap atlas, IEnumerable<string> glyphData)
        {
            this.atlas = atlas;
            atlasData = atlas.LockBits(new(Point.Empty, atlas.Size), System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            foreach (var line in glyphData)
            {
                var c = line.Split();

                //    $"rect {rect.x} {rect.y} {rect.width} {rect.height} " +
                //    $"met {met.height} {met.width} {met.horizontalAdvance} {met.horizontalBearingX} {met.horizontalBearingY}");
                //> rect 500 982 16 27 met 25.90625 14.76563 21.03125 3.453125 32.25
                char ch = c[0] == "SPACE" ? ' ' : c[0][0];
                var glyph = new BubbleGlyph()
                {
                    src = new(int.Parse(c[2]), int.Parse(c[3]), int.Parse(c[4]), int.Parse(c[5])),
                    h = float.Parse(c[7]),
                    w = float.Parse(c[8]),
                    advance = float.Parse(c[9]),
                    bearingX = float.Parse(c[10]),
                    bearingY = float.Parse(c[11]),
                };
                glyph.src.Y = atlas.Height - glyph.src.Y - glyph.src.Height;
                glyphs[ch] = glyph;
            }
        }
        public void Render(string str, Bitmap target, SaberDrawParams data)
        {
            var bufferData = target.LockBits(new(Point.Empty, target.Size), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);

            if (data.Clear)
            {
                unsafe
                {
                    Unsafe.InitBlockUnaligned((byte*)bufferData.Scan0, 0, (uint)(bufferData.Stride * bufferData.Height));
                }
            }

            float x = 4;
            float y = target.Height - 16;
            float scale = data.TextScale;

            foreach (char ch in str)
            {
                var glyph = glyphs[ch];

                float pad = 8 * scale;
                for (float py = -pad; py < glyph.src.Height * scale + pad; py++)
                {
                    for (float px = -pad; px < glyph.src.Width * scale + pad; px++)
                    {
                        var value = GetPixel(glyph, px / ((double)glyph.src.Width * scale), py / (double)(glyph.src.Height * scale));
                        SetPixel((int)(x + px), (int)(y + py - glyph.bearingY * scale), value, data, target, bufferData);
                    }
                }

                x += (glyph.advance + data.LetterSpacing) * scale;
            }

            target.UnlockBits(bufferData);

        }
        private int GetOff(int x, int y)
        {
            return atlasData.Stride * y + x * 4;
        }

        private float GetPixel(BubbleGlyph glyph, double nx, double ny)
        {
            float rawx = (float)(nx * glyph.src.Width) + glyph.src.Left;
            int l = (int)Math.Floor(rawx);
            float factorRight = rawx - l;
            float factorLeft = 1 - factorRight;

            float rawy = (float)(ny * glyph.src.Height) + glyph.src.Top;
            int t = (int)Math.Floor(rawy);
            float factorBot = rawy - t;
            float factorTop = 1 - factorBot;

            float ul = Marshal.ReadByte(atlasData.Scan0, GetOff(l, t) + 3);
            float ur = Marshal.ReadByte(atlasData.Scan0, GetOff(l + 1, t) + 3);
            float ll = Marshal.ReadByte(atlasData.Scan0, GetOff(l, t + 1) + 3);
            float lr = Marshal.ReadByte(atlasData.Scan0, GetOff(l + 1, t + 1) + 3);

            float top = ul * factorLeft + ur * factorRight;
            float bot = ll * factorLeft + lr * factorRight;

            float total = top * factorTop + bot * factorBot;

            return total / 255.0f;
        }

        float smoothstep(float edge0, float edge1, float x)
        {
            if (x < edge0)
                return 0;

            if (x >= edge1)
                return 1;

            // Scale/bias into [0..1] range
            x = (x - edge0) / (edge1 - edge0);

            return x * x * (3 - 2 * x);
        }

        private void SetPixel(int x, int y, float a, SaberDrawParams data, Bitmap buffer, BitmapData bufferData)
        {
            if (x >= buffer.Width || y >= buffer.Height) return;

            float border = data.Border;

            int off = bufferData.Stride * y + (x * 4);
            a = smoothstep(1.0f - data.Thickness - data.Softness, 1.0f - data.Thickness + data.Softness, a);
            if (a < 0.2f) return;

            float outline = smoothstep(data.OutlineThickness - data.OutlineSoftness, data.OutlineThickness + data.OutlineSoftness, a);
            //if (a > 0.5f)

            float r = (outline * data.Color.X) + (1.0f - outline) * border;
            float g = (outline * data.Color.Y) + (1.0f - outline) * border;
            float b = (outline * data.Color.Z) + (1.0f - outline) * border;

            Marshal.WriteByte(bufferData.Scan0 + off + 0, (byte)(b * 255.0));
            Marshal.WriteByte(bufferData.Scan0 + off + 1, (byte)(g * 255.0));
            Marshal.WriteByte(bufferData.Scan0 + off + 2, (byte)(r * 255.0));
            Marshal.WriteByte(bufferData.Scan0 + off + 3, Math.Max((byte)0xee, (byte)(a * 255.0)));
        }

        public void Dispose()
        {
            atlas.UnlockBits(atlasData);
        }
    }
    public class SaberDrawParams
    {
        public float Thickness = 0.5f;
        public float Softness = 0.0f;
        public float OutlineThickness = 0.5f;
        public float OutlineSoftness = 0.1f;
        public float TextScale = 1.0f;

        public float LetterSpacing = 0.0f;

        public Vector3 Color = Vector3.One;
        public float Border = 0.02f;
        public bool Clear = true;
    }

    public class BubbleGlyph
    {
        public Rectangle src;
        public float w;
        public float h;
        public float advance;
        public float bearingX;
        public float bearingY;
    }
}