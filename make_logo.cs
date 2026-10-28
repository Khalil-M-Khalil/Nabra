using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

class Program
{
    static void Main(string[] args)
    {
        using (Bitmap bmp = new Bitmap(args[0]))
        {
            bmp.MakeTransparent(Color.White);
            for (int y = 0; y < bmp.Height; y++)
            {
                for (int x = 0; x < bmp.Width; x++)
                {
                    Color c = bmp.GetPixel(x, y);
                    if (c.R > 230 && c.G > 230 && c.B > 230 && c.A == 255)
                    {
                        int max = Math.Max(c.R, Math.Max(c.G, c.B));
                        int alpha = 255 - ((max - 230) * 255 / 25);
                        if (alpha < 0) alpha = 0;
                        if (alpha > 255) alpha = 255;
                        bmp.SetPixel(x, y, Color.FromArgb(alpha, c.R, c.G, c.B));
                    }
                }
            }
            bmp.Save(args[1], ImageFormat.Png);
        }
    }
}
