using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

class Program
{
    static void Main()
    {
        using (Bitmap src = new Bitmap("src\\Nabra\\Assets\\logo.png"))
        {
            int[] sizes = new int[] { 256, 128, 64, 48, 32, 16 };
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

                    // Add a very subtle, soft glow so it's visible on dark taskbars
                    // while remaining clean on light desktops
                    // Draw soft halo (1px offset in all directions with low alpha)
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
                                    new float[] { 0, 0, 0, 0.25f, 0 },
                                    new float[] { 1f, 1f, 1f, 0, 1 } // turn to white with 25% opacity
                                });
                                using (ImageAttributes ia = new ImageAttributes())
                                {
                                    ia.SetColorMatrix(cm);
                                    g.DrawImage(src, new Rectangle(dx, dy, s, s), 0, 0, src.Width, src.Height, GraphicsUnit.Pixel, ia);
                                }
                            }
                        }
                    }

                    // Draw main image on top
                    g.DrawImage(src, new Rectangle(0, 0, s, s), 0, 0, src.Width, src.Height, GraphicsUnit.Pixel);

                    target.Save("test_" + s + ".png", ImageFormat.Png);
                }
            }
        }
    }
}
