using System;
using System.Drawing;
using System.Drawing.Imaging;

class Program
{
    static void Main()
    {
        using (Bitmap bmp = new Bitmap("src\\Nabra\\Assets\\logo.png"))
        {
            for (int y = 0; y < bmp.Height; y++)
            {
                for (int x = 0; x < bmp.Width; x++)
                {
                    Color c = bmp.GetPixel(x, y);
                    // The orange dot is around R=242 G=90 B=41.
                    // The black part is R<50, G<50, B<50.
                    if (c.A > 0 && c.R < 100 && c.G < 100 && c.B < 100)
                    {
                        // Invert black to white, keep anti-aliasing alpha
                        // If it's grayish anti-aliasing, invert it.
                        // Actually, just change it to white but keep the alpha channel!
                        bmp.SetPixel(x, y, Color.FromArgb(c.A, 255, 255, 255));
                    }
                }
            }
            bmp.Save("src\\Nabra\\Assets\\logo_dark.png", ImageFormat.Png);
        }
    }
}
