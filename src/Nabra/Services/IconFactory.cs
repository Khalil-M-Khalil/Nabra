using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using Nabra.Interop;

namespace Nabra.Services
{
    /// <summary>Draws the tray icon at runtime (accent-coloured when on, grey when off).</summary>
    internal static class IconFactory
    {
        public static Icon Create(bool enabled, byte r, byte g, byte b)
        {
            const int size = 32;
            using (var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb))
            {
                using (var gfx = Graphics.FromImage(bmp))
                {
                    gfx.SmoothingMode = SmoothingMode.AntiAlias;
                    gfx.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                    gfx.Clear(Color.Transparent);

                    var fill = enabled ? Color.FromArgb(r, g, b) : Color.FromArgb(120, 128, 138);
                    using (var path = RoundedRect(new RectangleF(1, 1, size - 2, size - 2), 8f))
                    using (var brush = new SolidBrush(fill))
                    {
                        gfx.FillPath(brush, path);
                    }

                    using (var font = new Font("Segoe UI", 19f, FontStyle.Bold, GraphicsUnit.Pixel))
                    using (var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                    {
                        gfx.DrawString("\u0646", font, Brushes.White, new RectangleF(0, 1, size, size), format);
                    }
                }

                IntPtr handle = bmp.GetHicon();
                try
                {
                    using (var temp = Icon.FromHandle(handle))
                    {
                        return (Icon)temp.Clone();
                    }
                }
                finally
                {
                    NativeMethods.DestroyIcon(handle);
                }
            }
        }

        private static GraphicsPath RoundedRect(RectangleF rect, float radius)
        {
            float d = radius * 2f;
            var path = new GraphicsPath();
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
