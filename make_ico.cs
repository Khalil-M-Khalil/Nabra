using System;
using System.IO;

class Program
{
    static void Main(string[] args)
    {
        byte[] pngBytes = File.ReadAllBytes(args[0]);
        using (FileStream fs = new FileStream(args[1], FileMode.Create))
        using (BinaryWriter bw = new BinaryWriter(fs))
        {
            // ICONDIR structure
            bw.Write((short)0); // Reserved
            bw.Write((short)1); // Type: 1 for icon
            bw.Write((short)1); // Image count

            // ICONDIRENTRY structure
            bw.Write((byte)0); // Width: 0 for 256
            bw.Write((byte)0); // Height: 0 for 256
            bw.Write((byte)0); // Color count
            bw.Write((byte)0); // Reserved
            bw.Write((short)1); // Color planes
            bw.Write((short)32); // Bits per pixel
            bw.Write((int)pngBytes.Length); // Image size
            bw.Write((int)22); // Offset (22 bytes header)

            // Image data
            bw.Write(pngBytes);
        }
    }
}
