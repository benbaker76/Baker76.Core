using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

// https://gist.github.com/NoelFB/778d190e5d17f1b86ebf39325346fcc5
//
// File Format:
// https://github.com/aseprite/aseprite/blob/master/docs/ase-file-specs.md

namespace Baker76.Imaging
{
    public class Aseprite
    {
        public enum Modes
        {
            Indexed = 1,
            Grayscale = 2,
            RGBA = 4
        }

        private enum Chunks
        {
            OldPaletteA = 0x0004,
            OldPaletteB = 0x0011,
            Layer = 0x2004,
            Cel = 0x2005,
            CelExtra = 0x2006,
            Mask = 0x2016,
            Path = 0x2017,
            FrameTags = 0x2018,
            Palette = 0x2019,
            UserData = 0x2020,
            Slice = 0x2022
        }

        private enum CelTypes
        {
            RawCel = 0,
            LinkedCel = 1,
            CompressedImage = 2
        }


        public Modes Mode;
        public int Width;
        public int Height;
        public int FrameCount;

        public List<Layer> Layers = new List<Layer>();
        public List<Frame> Frames = new List<Frame>();
        public List<Tag> Tags = new List<Tag>();
        public List<Slice> Slices = new List<Slice>();
        public Color[] Palette;
        public int TransparentIndex = 0;

        public Aseprite(Modes mode, int width, int height)
        {
            Mode = mode;
            Width = width;
            Height = height;
        }

        #region .ase Parser


        public Aseprite(string fileName, bool loadImageData = true)
        {
            using (var stream = File.OpenRead(fileName))
                ReadData(stream, loadImageData);
        }

        public Aseprite(Stream stream, bool loadImageData = true)
        {
            ReadData(stream, loadImageData);
        }

        public void ReadData(Stream stream, bool loadImageData = true)
        {
            ushort magic = 0;

            using (var reader = new BinaryReader(stream))
            {
                // wrote these to match the documentation names so it's easier (for me, anyway) to parse
                byte BYTE() { return reader.ReadByte(); }
                ushort WORD() { return reader.ReadUInt16(); }
                short SHORT() { return reader.ReadInt16(); }
                uint DWORD() { return reader.ReadUInt32(); }
                long LONG() { return reader.ReadInt32(); }
                string STRING() { return Encoding.UTF8.GetString(BYTES(WORD())); }
                byte[] BYTES(int number) { return reader.ReadBytes(number); }
                void SEEK(int number) { reader.BaseStream.Position += number; }

                // Header
                {
                    // file size
                    DWORD();

                    // Magic number (0xA5E0)
                    magic = WORD();
                    if (magic != 0xA5E0)
                        throw new Exception("File is not in .ase format");

                    // Frames / Width / Height / Color Mode
                    FrameCount = WORD();
                    Width = WORD();
                    Height = WORD();
                    Mode = (Modes)(WORD() / 8);

                    // Other Info, Ignored
                    DWORD();       // Flags
                    WORD();        // Speed (deprecated)
                    DWORD();       // Set to 0
                    DWORD();       // Set to 0
                    TransparentIndex = BYTE();        // Palette entry 
                    SEEK(3);       // Ignore these bytes
                    WORD();        // Number of colors (0 means 256 for old sprites)
                    BYTE();        // Pixel width
                    BYTE();        // Pixel height
                    SHORT();       // X position of the grid
                    SHORT();       // Y position of the grid
                    WORD();        // Grid width
                    WORD();        // Grid height
                    SEEK(84);      // For Future
                }

                // temporary variables
                var temp = new byte[Width * Height * (int)Mode];
                Palette = new Color[256];
                IUserData last = null;

                // Frames
                for (int i = 0; i < FrameCount; i++)
                {
                    var frame = new Frame(this);
                    if (loadImageData)
                    {
                        frame.Pixels = new Color[Width * Height];
                        frame.Indices = new byte[Width * Height];
                    }
                    Frames.Add(frame);

                    long frameStart, frameEnd;
                    uint oldChunkCount, chunkCount;

                    // frame header
                    {
                        frameStart = reader.BaseStream.Position;
                        frameEnd = frameStart + DWORD();
                        magic = WORD();          // Magic number (always 0xF1FA)
                        if (magic != 0xF1FA)
                            throw new Exception("Error reading file");
                        oldChunkCount = (uint)WORD();     // Number of "chunks" in this frame
                        frame.Duration = WORD(); // Frame duration (in milliseconds)
                        BYTE();                  // Set to 0
                        BYTE();                  // Set to 0
                        chunkCount = DWORD();    // Number of "chunks" in this frame (again)
                        if (chunkCount == 0)
                            chunkCount = oldChunkCount;
                    }

                    // chunks
                    for (int j = 0; j < chunkCount; j++)
                    {
                        long chunkStart, chunkEnd;
                        Chunks chunkType;

                        // chunk header
                        {
                            chunkStart = reader.BaseStream.Position;
                            chunkEnd = chunkStart + DWORD();
                            chunkType = (Chunks)WORD();
                        }

                        // LAYER CHUNK
                        if (chunkType == Chunks.Layer)
                        {
                            // create layer
                            var layer = new Layer();

                            // get layer data
                            layer.Flag = (Layer.Flags)WORD();
                            layer.Type = (Layer.Types)WORD();
                            layer.ChildLevel = WORD();
                            WORD(); // width (unused)
                            WORD(); // height (unused)
                            layer.BlendMode = WORD();
                            layer.Alpha = (BYTE() / 255f);
                            SEEK(3); // for future
                            layer.Name = STRING();

                            last = layer;
                            Layers.Add(layer);
                        }
                        // CEL CHUNK
                        else if (chunkType == Chunks.Cel)
                        {
                            // create cel
                            var cel = new Cel();

                            // get cel data
                            var layerIndex = WORD();
                            cel.Layer = Layers[layerIndex];
                            cel.X = SHORT();
                            cel.Y = SHORT();
                            cel.Alpha = BYTE() / 255f;
                            var celType = (CelTypes)WORD(); // type
                            SEEK(7);

                            if (loadImageData)
                            {
                                // RAW or DEFLATE
                                if (celType == CelTypes.RawCel || celType == CelTypes.CompressedImage)
                                {
                                    cel.Width = WORD();
                                    cel.Height = WORD();

                                    var count = cel.Width * cel.Height * (int)Mode;

                                    // RAW
                                    if (celType == 0)
                                    {
                                        reader.Read(temp, 0, cel.Width * cel.Height * (int)Mode);
                                    }
                                    // DEFLATE
                                    else
                                    {
                                        SEEK(2);

                                        using (var deflate = new DeflateStream(reader.BaseStream, CompressionMode.Decompress, true))
                                        {
                                            int readBytes;
                                            var totalBytesRead = 0;
                                            do
                                            {
                                                readBytes = deflate.Read(temp, totalBytesRead, temp.Length - totalBytesRead);
                                                totalBytesRead += readBytes;
                                            }
                                            while (readBytes > 0);
                                        }
                                    }

                                    cel.Pixels = new Color[cel.Width * cel.Height];
                                    cel.Indices = temp;

                                    BytesToPixels(temp, cel.Pixels, Mode, Palette);
                                    CelToFrame(frame, cel, Mode);
                                }
                                // REFERENCE
                                else if (celType == CelTypes.LinkedCel)
                                {
                                    var targetFrame = WORD(); // Frame position to link with

                                    // Grab the cel from a previous frame
                                    var targetCel = Frames[targetFrame].Cels.Where(c => c.Layer == Layers[layerIndex]).First();
                                    cel.Width = targetCel.Width;
                                    cel.Height = targetCel.Height;
                                    cel.Pixels = targetCel.Pixels;
                                }
                            }

                            last = cel;
                            frame.Cels.Add(cel);
                        }
                        // OLD PALETTE CHUNK
                        else if (chunkType == Chunks.OldPaletteA)
                        {
                            var packetCount = WORD();
                            for (int p = 0; p < packetCount; p++)
                            {
                                var skip = BYTE();
                                var count = BYTE();

                                for (int c = 0; c < (count == 0 ? 256 : count); c++)
                                {
                                    Palette[skip + c] = Color.FromRgbaNonPremultiplied(BYTE(), BYTE(), BYTE(), 255);
                                }
                            }
                        }
                        // PALETTE CHUNK
                        else if (chunkType == Chunks.Palette)
                        {
                            var size = DWORD();
                            var start = DWORD();
                            var end = DWORD();
                            SEEK(8); // for future

                            for (int p = 0; p < (end - start) + 1; p++)
                            {
                                var hasName = WORD();
                                Palette[start + p] = Color.FromRgbaNonPremultiplied(BYTE(), BYTE(), BYTE(), BYTE());
                                if (IsBitSet(hasName, 0))
                                    STRING();
                            }
                        }
                        // USERDATA
                        else if (chunkType == Chunks.UserData)
                        {
                            if (last != null)
                            {
                                var flags = (int)DWORD();

                                // has text
                                if (IsBitSet(flags, 0))
                                    last.UserDataText = STRING();

                                // has color
                                if (IsBitSet(flags, 1))
                                    last.UserDataColor = Color.FromRgbaNonPremultiplied(BYTE(), BYTE(), BYTE(), BYTE());
                            }
                        }
                        // TAG
                        else if (chunkType == Chunks.FrameTags)
                        {
                            var count = WORD();
                            SEEK(8);

                            for (int t = 0; t < count; t++)
                            {
                                var tag = new Tag();
                                tag.From = WORD();
                                tag.To = WORD();
                                tag.LoopDirection = (Tag.LoopDirections)BYTE();
                                SEEK(8);
                                tag.Color = Color.FromRgbaNonPremultiplied(BYTE(), BYTE(), BYTE(), 255);
                                SEEK(1);
                                tag.Name = STRING();
                                Tags.Add(tag);
                            }
                        }
                        // SLICE
                        else if (chunkType == Chunks.Slice)
                        {
                            var count = DWORD();
                            var flags = (int)DWORD();
                            DWORD(); // reserved
                            var name = STRING();

                            for (int s = 0; s < count; s++)
                            {
                                var slice = new Slice();
                                slice.Name = name;
                                slice.Frame = (int)DWORD();
                                slice.OriginX = (int)LONG();
                                slice.OriginY = (int)LONG();
                                slice.Width = (int)DWORD();
                                slice.Height = (int)DWORD();

                                // 9 slice (ignored atm)
                                if (IsBitSet(flags, 0))
                                {
                                    LONG();
                                    LONG();
                                    DWORD();
                                    DWORD();
                                }

                                // pivot point
                                if (IsBitSet(flags, 1))
                                    slice.Pivot = new System.Drawing.Point((int)DWORD(), (int)DWORD());

                                last = slice;
                                Slices.Add(slice);
                            }
                        }

                        reader.BaseStream.Position = chunkEnd;
                    }

                    reader.BaseStream.Position = frameEnd;
                }
            }
        }

        #endregion

        #region Data Structures

        public class Frame
        {
            public Aseprite Sprite;
            public int Duration;
            public Color[] Pixels;
            public byte[] Indices;
            public Image Image;
            public List<Cel> Cels;

            public Frame(Aseprite sprite)
            {
                Sprite = sprite;
                Cels = new List<Cel>();
            }
        }

        public class Tag
        {
            public enum LoopDirections
            {
                Forward = 0,
                Reverse = 1,
                PingPong = 2,
                PingPongReverse = 3
            }

            public string Name;
            public LoopDirections LoopDirection;
            public int From;
            public int To;
            public Color Color;
        }

        public interface IUserData
        {
            string UserDataText { get; set; }
            Color UserDataColor { get; set; }
        }

        public struct Slice : IUserData
        {
            public int Frame;
            public string Name;
            public int OriginX;
            public int OriginY;
            public int Width;
            public int Height;
            public System.Drawing.Point? Pivot;
            public string UserDataText { get; set; }
            public Color UserDataColor { get; set; }
        }

        public class Cel : IUserData
        {
            public Layer Layer;
            public Color[] Pixels;
            public byte[] Indices;

            public int X;
            public int Y;
            public int Width;
            public int Height;
            public float Alpha;

            public string UserDataText { get; set; }
            public Color UserDataColor { get; set; }
        }

        public class Layer : IUserData
        {
            [Flags]
            public enum Flags
            {
                Visible = 1,
                Editable = 2,
                LockMovement = 4,
                Background = 8,
                PreferLinkedCels = 16,
                Collapsed = 32,
                Reference = 64
            }

            public enum Types
            {
                Normal = 0,
                Group = 1
            }

            public Flags Flag;
            public Types Type;
            public string Name;
            public int ChildLevel;
            public int BlendMode;
            public float Alpha;

            public string UserDataText { get; set; }
            public Color UserDataColor { get; set; }
        }

        #endregion

        #region Blend Modes

        // Copied from Aseprite's source code:
        // https://github.com/aseprite/aseprite/blob/master/src/doc/blend_funcs.cpp

        private delegate void Blend(ref Color dest, Color src, byte opacity);

        private static Blend[] BlendModes = new Blend[]
        {
            // 0 - NORMAL
            (ref Color dest, Color src, byte opacity) =>
            {
                int r, g, b, a;

                if (dest.A == 0)
                {
                    r = src.R;
                    g = src.G;
                    b = src.B;
                    a = src.A;
                }
                else if (src.A == 0)
                {
                    r = dest.R;
                    g = dest.G;
                    b = dest.B;
                    a = dest.A;
                }
                else
                {
                    a = (dest.A + MUL_UN8(src.A - dest.A, opacity));

                    if (a != 0)
                    {
                        r = (dest.R + MUL_UN8(src.R - dest.R, opacity));
                        g = (dest.G + MUL_UN8(src.G - dest.G, opacity));
                        b = (dest.B + MUL_UN8(src.B - dest.B, opacity));
                    }
                    else
                    {
                        r = g = b = 0;
                    }
                }

                dest.R = (byte)r;
                dest.G = (byte)g;
                dest.B = (byte)b;
                dest.A = (byte)a;
            }
        };

        private static int MUL_UN8(int a, int b)
        {
            var t = (a * b) + 0x80;
            return (((t >> 8) + t) >> 8);
        }

        #endregion

        #region Utils

        /// <summary>
        /// Converts an array of Bytes to an array of Colors, using the specific Aseprite Mode & Palette
        /// </summary>
        private void BytesToPixels(byte[] bytes, Color[] pixels, Aseprite.Modes mode, Color[] palette)
        {
            int len = pixels.Length;
            if (mode == Modes.RGBA)
            {
                for (int p = 0, b = 0; p < len; p++, b += 4)
                {
                    byte red = (byte)(bytes[b + 0] * bytes[b + 3] / 255);
                    byte green = (byte)(bytes[b + 1] * bytes[b + 3] / 255);
                    byte blue = (byte)(bytes[b + 2] * bytes[b + 3] / 255);
                    byte alpha = bytes[b + 3];
                    pixels[p] = new Color(red, green, blue, alpha);
                }
            }
            else if (mode == Modes.Grayscale)
            {
                for (int p = 0, b = 0; p < len; p++, b += 2)
                {
                    byte gray = (byte)(bytes[b + 0] * bytes[b + 1] / 255);
                    byte alpha = bytes[b + 1];
                    pixels[p] = new Color(gray, gray, gray, alpha);
                }
            }
            else if (mode == Modes.Indexed)
            {
                for (int p = 0, b = 0; p < len; p++, b++)
                {
                    pixels[p] = palette[bytes[b]];
                }
            }
        }

        /// <summary>
        /// Applies a Cel's pixels to the Frame, using its Layer's BlendMode & Alpha
        /// </summary>
        private void CelToFrame(Frame frame, Cel cel, Modes mode)
        {
            bool isVisible = (cel.Layer.Flag & Layer.Flags.Visible) != 0;
            var opacity = (byte)((cel.Alpha * cel.Layer.Alpha) * 255);
            var blend = BlendModes[cel.Layer.BlendMode];

            for (int i = 0; i < frame.Pixels.Length; i++)
                if (frame.Pixels[i] == null)
                    frame.Pixels[i] = Color.Transparent;

            if (isVisible)
            {
                for (int sx = 0; sx < cel.Width; sx++)
                {
                    int dx = cel.X + sx;
                    int dy = cel.Y * frame.Sprite.Width;

                    for (int i = 0, sy = 0; i < cel.Height; i++, sy += cel.Width, dy += frame.Sprite.Width)
                    {
                        if (mode == Modes.RGBA)
                            blend(ref frame.Pixels[dx + dy], cel.Pixels[sx + sy], opacity);
                        else
                            frame.Indices[dx + dy] = cel.Indices[sx + sy];
                    }
                }
            }

            if (mode == Modes.RGBA)
            {
                byte[] pixelData = new byte[frame.Pixels.Length * 4];

                for (int i = 0, j = 0; i < frame.Pixels.Length; i++, j += 4)
                {
                    pixelData[j + 0] = (byte)frame.Pixels[i].R;
                    pixelData[j + 1] = (byte)frame.Pixels[i].G;
                    pixelData[j + 2] = (byte)frame.Pixels[i].B;
                    pixelData[j + 3] = (byte)frame.Pixels[i].A;
                }

                frame.Image = new Image(frame.Sprite.Width, frame.Sprite.Height, 32, pixelData);
            }
            else
            {
                frame.Image = new Image(frame.Sprite.Width, frame.Sprite.Height, 8, new Palette(Palette), frame.Indices);
            }
        }

        private static bool IsBitSet(int b, int pos)
        {
            return (b & (1 << pos)) != 0;
        }

        #endregion
    }
}