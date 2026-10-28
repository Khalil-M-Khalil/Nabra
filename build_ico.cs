using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        using (Bitmap src = new Bitmap("src\\Nabra\\Assets\\logo.png"))
        {
            int[] sizes = new int[] { 256, 128, 64, 48, 32, 16 };
            List<byte[]> imageBuffers = new List<byte[]>();
            List<int> imageSizes = new List<int>();

            foreach (int s in sizes)
            {
                using (Bitmap target = new Bitmap(s, s, PixelFormat.Format32bppArgb))
                using (Graphics g = Graphics.FromImage(target))
                {
                    g.SmoothingMode = SmoothingMode.HighQuality;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    g.CompositingQuality = CompositingQuality.HighQuality;
                    g.Clear(Color.Transparent);

                    // Add soft glow
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            if (dx != 0 || dy != 0)
                            {
                                ColorMatrix cm = new ColorMatrix(new float[][] {
                                    new float[] { 0, 0, 0, 0, 0 },
                                    new float[] { 0, 0, 0, 0, 0 },
                                    new float[] { 0, 0, 0, 0, 0 },
                                    new float[] { 0, 0, 0, 0.22f, 0 },
                                    new float[] { 1f, 1f, 1f, 0, 1 }
                                });
                                using (ImageAttributes ia = new ImageAttributes())
                                {
                                    ia.SetColorMatrix(cm);
                                    g.DrawImage(src, new Rectangle(dx, dy, s, s), 0, 0, src.Width, src.Height, GraphicsUnit.Pixel, ia);
                                }
                            }
                        }
                    }
                    g.DrawImage(src, new Rectangle(0, 0, s, s), 0, 0, src.Width, src.Height, GraphicsUnit.Pixel);

                    if (s == 256)
                    {
                        using (MemoryStream ms = new MemoryStream())
                        {
                            target.Save(ms, ImageFormat.Png);
                            imageBuffers.Add(ms.ToArray());
                        }
                    }
                    else
                    {
                        imageBuffers.Add(CreateDib(target));
                    }
                    imageSizes.Add(s);
                }
            }

            // Write full ICO file
            using (FileStream fs = new FileStream("src\\Nabra\\Assets\\nabra.ico", FileMode.Create))
            using (BinaryWriter bw = new BinaryWriter(fs))
            {
                // ICONDIR
                bw.Write((short)0); // Reserved
                bw.Write((short)1); // Type 1 = Icon
                bw.Write((short)sizes.Length); // Image count

                int offset = 6 + (16 * sizes.Length);

                for (int i = 0; i < sizes.Length; i++)
                {
                    int s = imageSizes[i];
                    byte bSize = (byte)(s == 256 ? 0 : s);
                    bw.Write(bSize); // Width
                    bw.Write(bSize); // Height
                    bw.Write((byte)0); // Colors
                    bw.Write((byte)0); // Reserved
                    bw.Write((short)1); // Planes
                    bw.Write((short)32); // BitCount
                    bw.Write((int)imageBuffers[i].Length); // BytesInRes
                    bw.Write((int)offset); // ImageOffset

                    offset += imageBuffers[i].Length;
                }

                for (int i = 0; i < sizes.Length; i++)
                {
                    bw.Write(imageBuffers[i]);
                }
            }
        }
    }

    static byte[] CreateDib(Bitmap bmp)
    {
        int width = bmp.Width;
        int height = bmp.Height;
        int maskRowBytes = ((width + 31) / 32) * 4;
        int maskSize = maskRowBytes * height;
        int imageSize = width * height * 4;
        int totalSize = 40 + imageSize + maskSize;

        byte[] data = new byte[totalSize];
        using (MemoryStream ms = new MemoryStream(data))
        using (BinaryWriter bw = new BinaryWriter(ms))
        {
            bw.Write(40);
            bw.Write(width);
            bw.Write(height * 2);
            bw.Write((short)1);
            bw.Write((short)32);
            bw.Write(0);
            bw.Write(imageSize);
            bw.Write(0);
            bw.Write(0);
            bw.Write(0);
            bw.Write(0);

            for (int y = height - 1; y >= 0; y--)
            {
                for (int x = 0; x < width; x++)
                {
                    Color c = bmp.GetPixel(x, y);
                    bw.Write(c.B);
                    bw.Write(c.G);
                    bw.Write(c.R);
                    bw.Write(c.A);
                }
            }

            byte[] mask = new byte[maskSize];
            bw.Write(mask);
        }
        return data;
    }
}
