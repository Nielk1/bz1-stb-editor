using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Drawing;

namespace BZSpriteEditor
{
    class MapFile
    {
        private Int16 width;
        private MapType type;
        private Int16 height;
        private byte[] raw;
        private int bpp;

        public enum MapType
        {
            P8       = 0, // 0 - 8 bit pallet - SW only
            A4R4G4B4 = 1, // 1 - 4444 - terrible hack
            R5G6B5   = 2, // 2 - 565 - 1.5
            A8R8G8B8 = 3, // 3 - 8888 - 1.5
            X8R8G8B8 = 4, // 4 - 8888 - 1.5
        }

        public static int GetBPP(MapType type)
        {
            int bpp = 0;
            switch (type)
            {
                case MapType.P8:
                    bpp = 1; break;
                case MapType.A4R4G4B4:
                case MapType.R5G6B5:
                    bpp = 2; break;
                case MapType.A8R8G8B8:
                case MapType.X8R8G8B8:
                    bpp = 4; break;
            }

            Debug.Assert(bpp > 0, "MAP BPP not set, this should be impossible given enum paramater");
            return bpp;
        }

        public int Width { get { return (int)width; } }
        public int Height { get { return (int)height; } }

        public static MapFile FromFile(string filename)
        {
            using (FileStream stream = File.Open(filename, FileMode.Open))
            {
                return new MapFile(stream);
            }
        }

        public MapFile(Int16 width, Int16 height, MapType type)
        {
            this.width = width;
            this.height = height;
            this.type = type;
            this.bpp = GetBPP(type);
            this.raw = new byte[this.width * this.height * this.bpp];
        }

        public MapFile(Stream dataStream)
        {
            byte[] headerBytes = new byte[8];
            dataStream.Read(headerBytes, 0, 8);
            this.type = (MapType)BitConverter.ToInt16(headerBytes, 2);
            this.bpp = GetBPP(this.type);
            this.width = (Int16)(BitConverter.ToInt16(headerBytes, 0) / bpp);
            this.height = BitConverter.ToInt16(headerBytes, 4);
            this.raw = new byte[this.width * this.height * this.bpp];
            dataStream.Read(this.raw, 0, this.raw.Length);
        }

        public void Write(Stream outStream)
        {
            outStream.Write(BitConverter.GetBytes(width), 0, sizeof(Int16));
            outStream.Write(BitConverter.GetBytes((Int16)type), 0, sizeof(Int16));
            outStream.Write(BitConverter.GetBytes(height), 0, sizeof(Int16));
            outStream.Write(new byte[] { 0x00, 0x00 }, 0, 4);
            outStream.Write(raw, 0, raw.Length);
        }

        public Color GetPixel(int x, int y)
        {
            byte[] pixel = new byte[bpp];
            Array.Copy(raw, (x + y * width) * bpp, pixel, 0, bpp);

            switch (type)
            {
                case MapType.P8:
                    return Color.FromArgb((int)pixel[0], (int)pixel[0], (int)pixel[0], (int)pixel[0]);
                case MapType.A4R4G4B4:
                    return Color.FromArgb(
                        (int)((pixel[1] & 0xF0) >> 4) + (int)(pixel[1] & 0xF0),
                        (int)((pixel[1] & 0x0F) << 4) + (int)(pixel[1] & 0x0F),
                        (int)((pixel[0] & 0xF0) >> 4) + (int)(pixel[0] & 0xF0), 
                        (int)((pixel[0] & 0x0F) << 4) + (int)(pixel[0] & 0x0F)
                    );
                case MapType.R5G6B5:
                    return Color.FromArgb(
                        255,
                        (int)(pixel[1] & 0xF8),
                        (int)((pixel[1] & 0x07) << 5) + (int)((pixel[0] & 0xE0) >> 3),
                        (int)((pixel[0] & 0x1F) << 3)
                    );
                case MapType.A8R8G8B8:
                    return Color.FromArgb(
                        (int)pixel[3],
                        (int)pixel[2],
                        (int)pixel[1],
                        (int)pixel[0]
                    );
                case MapType.X8R8G8B8:
                    return Color.FromArgb(
                        255,
                        (int)pixel[2],
                        (int)pixel[1],
                        (int)pixel[0]);
            }
            return Color.Black;
        }
    }
}
