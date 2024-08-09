using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http;
using Baker76.Imaging;
using System.Threading.Tasks;

namespace Baker76.Imaging
{
    public enum PaletteFormat
    {
        Act,
        MSPal,
        JASC,
        GIMP,
        PaintNET
    }

    public class PalFile
    {
        // http://www.softhelp.ru/fileformat/pal1/Pal.htm

        private delegate TResult Func<T, TResult>(T arg);
        private const string errorMessage = "Could not recognise image format.";

        private static Dictionary<byte[], Func<BinaryReader, Color[]>> paletteFormatDecoders = new Dictionary<byte[], Func<BinaryReader, Color[]>>()
        {
            { new byte[] { (byte)'R', (byte)'I', (byte)'F', (byte)'F' }, DecodeMSPal },      // MS Palette
            { new byte[] { (byte)'J', (byte)'A', (byte)'S', (byte)'C', (byte)'-', (byte)'P', (byte)'A', (byte)'L' }, DecodeJASCPal },               // JASC Palette
            { new byte[] { (byte)'G', (byte)'I', (byte)'M', (byte)'P', (byte)' ', (byte)'P', (byte)'a', (byte)'l', (byte)'e', (byte)'t', (byte)'t', (byte)'e' }, DecodeGIMPPal },               // GIMP Palette
            { new byte[] { (byte)';', (byte)' ', (byte)'P', (byte)'a', (byte)'i', (byte)'n', (byte)'t', (byte)'.', (byte)'N', (byte)'E', (byte)'T', (byte)' ', (byte)'P', (byte)'a', (byte)'l', (byte)'e', (byte)'t', (byte)'t', (byte)'e' }, DecodePaintNetPal },
            { new byte[] { }, DecodeActPal },
        };

        private static byte[] MsPalRiffSig = { (byte)'R', (byte)'I', (byte)'F', (byte)'F' };
        private static byte[] MsPalRiffType = { (byte)'P', (byte)'A', (byte)'L', (byte)' ' };
        private static byte[] MsPalRiffChunkSig = { (byte)'d', (byte)'a', (byte)'t', (byte)'a' };
        private static byte[] MsPalRiffChunkPalVer = { 0x00, 0x03 };

        private Color[] m_colorPalette = null;

        public PalFile()
        {
            m_colorPalette = new Color[256];
        }

        public static async Task<Color[]> Read(HttpClient httpClient, string fileName)
        {
            using (var stream = await httpClient.GetStreamAsync(fileName))
            {
                return await Read(stream);
            }
        }

        public static async Task<Color[]> Read(string fileName)
        {
            using (var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            {
                return await Read(stream);
            }
        }

        public static async Task<Color[]> Read(Stream stream)
        {
            Color[] palette = null;

            using (var memoryStream = new MemoryStream())
            {
                await stream.CopyToAsync(memoryStream);

                memoryStream.Position = 0;

                using (var binaryReader = new BinaryReader(memoryStream))
                {
                    int maxMagicBytesLength = GetMaxMagicBytesLength();

                    byte[] magicBytes = binaryReader.ReadBytes(maxMagicBytesLength);

                    foreach (var kvPair in paletteFormatDecoders)
                    {
                        if (StartsWith(magicBytes, kvPair.Key))
                        {
                            memoryStream.Position = 0;

                            palette = kvPair.Value(binaryReader);

                            return palette;
                        }
                    }
                }
            }

            return palette;
        }

        private static bool StartsWith(byte[] thisBytes, byte[] thatBytes)
        {
            if (thatBytes.Length > thisBytes.Length)
                return false;

            for (int i = 0; i < thatBytes.Length; i++)
            {
                if (thisBytes[i] != thatBytes[i])
                    return false;
            }

            return true;
        }

        private static int GetMaxMagicBytesLength()
        {
            int maxMagicBytesLength = 0;

            foreach (byte[] magicBytes in paletteFormatDecoders.Keys)
                maxMagicBytesLength = Math.Max(maxMagicBytesLength, magicBytes.Length);

            return maxMagicBytesLength;
        }

        private static Color[] DecodeMSPal(BinaryReader binaryReader)
        {
            Color[] colorPalette = null;
            int fileLength = binaryReader.ReadInt32() - 16;
            binaryReader.ReadBytes(4); // Skip RIFF type
            binaryReader.ReadBytes(4); // Skip RIFF chunk signature
            binaryReader.ReadBytes(4); // Skip Chunk size
            binaryReader.ReadBytes(2); // Skip palette version
            int palCount = binaryReader.ReadInt16();
            colorPalette = new Color[palCount];

            for (int i = 0; i < colorPalette.Length; i++)
            {
                byte[] colorArray = binaryReader.ReadBytes(4);
                colorPalette[i] = new Color(colorArray[0], colorArray[1], colorArray[2]);
            }

            return colorPalette;
        }

        private static Color[] DecodeActPal(BinaryReader binaryReader)
        {
            List<Color> colorPalette = new List<Color>();

            for (int i = 0; i < 256; i++)
            {
                byte[] colorArray = binaryReader.ReadBytes(3);
                colorPalette.Add(new Color(colorArray[0], colorArray[1], colorArray[2]));
            }

            if (binaryReader.BaseStream.Position == binaryReader.BaseStream.Length - 4)
            {
                short palCount = ReadLittleEndianInt16(binaryReader);
                short transparentIndex = ReadLittleEndianInt16(binaryReader);

                colorPalette.RemoveRange(palCount, 256 - palCount);

                if (transparentIndex != -1)
                    colorPalette[transparentIndex] = Color.FromColor(colorPalette[transparentIndex], 0);
            }

            return colorPalette.ToArray();
        }

        private static Color[] DecodeJASCPal(BinaryReader binaryReader)
        {
            Color[] colorPalette = null;

            string tempString = ReadLine(binaryReader);
            string versionString = ReadLine(binaryReader);
            int palCount = Int32.Parse(ReadLine(binaryReader));
            colorPalette = new Color[palCount];

            for (int i = 0; i < colorPalette.Length; i++)
            {
                string colorString = ReadLine(binaryReader);

                if (colorString == null)
                    break;

                string[] colorArray = colorString.Split(new char[] { ' ' }, StringSplitOptions.None);
                colorPalette[i] = new Color(Byte.Parse(colorArray[0]), Byte.Parse(colorArray[1]), Byte.Parse(colorArray[2]));
            }

            return colorPalette;
        }

        private static Color[] DecodeGIMPPal(BinaryReader binaryReader)
        {
            List<Color> colorList = new List<Color>();

            while (true)
            {
                string lineString = ReadLine(binaryReader);

                if (lineString == null)
                    break;

                if (lineString.Equals("") ||
                    lineString.StartsWith("Name:") ||
                    lineString.StartsWith("Columns:") ||
                    lineString.StartsWith("#"))
                    continue;

                string[] colorArray = lineString.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                if (colorArray.Length < 3)
                    continue;

                byte red = 0, green = 0, blue = 0;

                if (!Byte.TryParse(colorArray[0], out red) ||
                    !Byte.TryParse(colorArray[1], out green) ||
                    !Byte.TryParse(colorArray[2], out blue))
                    continue;

                colorList.Add(new Color(red, green, blue));
            }

            return colorList.ToArray();
        }

        private static Color[] DecodePaintNetPal(BinaryReader binaryReader)
        {
            List<Color> colorList = new List<Color>();

            while (true)
            {
                string lineString = ReadLine(binaryReader);

                if (lineString == null)
                    break;

                if (lineString.Equals("") ||
                    lineString.StartsWith(";"))
                    continue;

                int result = 0;

                if (Int32.TryParse(lineString, NumberStyles.HexNumber, null, out result))
                {
                    colorList.Add(Color.FromArgb(result));
                }
            }

            return colorList.ToArray();
        }

        private static string ReadLine(BinaryReader binaryReader)
        {
            StringBuilder stringBuilder = new StringBuilder();
            bool foundEOL = false;
            char ch;

            while (!foundEOL)
            {
                if (binaryReader.BaseStream.Position == binaryReader.BaseStream.Length)
                {
                    if (stringBuilder.Length == 0)
                        return null;
                    else
                        break;
                }

                ch = binaryReader.ReadChar();

                switch (ch)
                {
                    case '\r':
                        if (binaryReader.PeekChar() == '\n')
                            binaryReader.ReadChar();
                        foundEOL = true;
                        break;
                    case '\n':
                        foundEOL = true;
                        break;
                    default:
                        stringBuilder.Append(ch);
                        break;
                }
            }

            return stringBuilder.ToString();
        }

        private static short ReadLittleEndianInt16(BinaryReader binaryReader)
        {
            byte[] byteArray = binaryReader.ReadBytes(sizeof(short));
            Array.Reverse(byteArray, 0, byteArray.Length);

            return BitConverter.ToInt16(byteArray, 0);
        }

        private static ushort ReadLittleEndianUInt16(BinaryReader binaryReader)
        {
            byte[] byteArray = binaryReader.ReadBytes(sizeof(ushort));
            Array.Reverse(byteArray, 0, byteArray.Length);

            return BitConverter.ToUInt16(byteArray, 0);
        }

        private static int ReadLittleEndianInt32(BinaryReader binaryReader)
        {
            byte[] byteArray = binaryReader.ReadBytes(sizeof(int));
            Array.Reverse(byteArray, 0, byteArray.Length);

            return BitConverter.ToInt32(byteArray, 0);
        }

        public static void Write(string fileName, Color[] colorPalette, PaletteFormat paletteFormat, int transparentIndex = -1)
        {
            using (FileStream fileStream = new FileStream(fileName, FileMode.Create, FileAccess.Write))
            {
                Write(fileStream, Path.GetFileNameWithoutExtension(fileName), colorPalette, paletteFormat, transparentIndex);
            }
        }

        public static void Write(Stream stream, string name, Color[] colorPalette, PaletteFormat paletteFormat, int transparentIndex = -1)
        {
            switch (paletteFormat)
            {
                case PaletteFormat.Act:
                    WriteActFile(stream, colorPalette, transparentIndex);
                    break;
                case PaletteFormat.MSPal:
                    WriteMSPalFile(stream, colorPalette);
                    break;
                case PaletteFormat.JASC:
                    WriteJASCPalFile(stream, colorPalette);
                    break;
                case PaletteFormat.GIMP:
                    WriteGIMPPalFile(stream, name, colorPalette);
                    break;
                case PaletteFormat.PaintNET:
                    WritePaintNETPalFile(stream, name, colorPalette);
                    break;
            }
        }

        private static void WriteActFile(Stream stream, Color[] colorPalette, int transparentIndex)
        {
            // ACT file format: R, G, B, repeated 256 times
            for (int i = 0; i < 256; i++)
            {
                byte r = 0, g = 0, b = 0;
                if (i < colorPalette.Length)
                {
                    r = (byte)colorPalette[i].R;
                    g = (byte)colorPalette[i].G;
                    b = (byte)colorPalette[i].B;
                }
                stream.WriteByte(r);
                stream.WriteByte(g);
                stream.WriteByte(b);
            }

            // Write color count and transparency index if necessary
            if (transparentIndex != -1 || colorPalette.Length < 256)
            {
                stream.WriteByte((byte)(colorPalette.Length >> 8));
                stream.WriteByte((byte)colorPalette.Length);

                if (transparentIndex == -1)
                {
                    stream.WriteByte(0xFF);
                    stream.WriteByte(0xFF);
                }
                else
                {
                    stream.WriteByte((byte)(transparentIndex >> 8));
                    stream.WriteByte((byte)transparentIndex);
                }
            }
        }

        private static void WriteMSPalFile(Stream stream, Color[] colorPalette)
        {
            // MSPal file format: R, G, B, 0, repeated for each color
            stream.Write(MsPalRiffSig, 0, MsPalRiffSig.Length);
            int paletteSize = colorPalette.Length * 4 + 16;
            stream.Write(BitConverter.GetBytes(paletteSize), 0, 4);
            stream.Write(MsPalRiffType, 0, MsPalRiffType.Length);
            stream.Write(MsPalRiffChunkSig, 0, MsPalRiffChunkSig.Length);
            int chunkSize = colorPalette.Length * 4 + 4;
            stream.Write(BitConverter.GetBytes(chunkSize), 0, 4);
            stream.Write(MsPalRiffChunkPalVer, 0, MsPalRiffChunkPalVer.Length);
            stream.Write(BitConverter.GetBytes((short)256), 0, 2);

            foreach (Color color in colorPalette)
            {
                stream.WriteByte((byte)color.R);
                stream.WriteByte((byte)color.G);
                stream.WriteByte((byte)color.B);
                stream.WriteByte(0);
            }
        }

        private static void WriteJASCPalFile(Stream stream, Color[] colorPalette)
        {
            using (StreamWriter writer = new StreamWriter(stream))
            {
                writer.WriteLine("JASC-PAL");
                writer.WriteLine("0100");
                writer.WriteLine(colorPalette.Length);

                foreach (Color color in colorPalette)
                {
                    writer.WriteLine($"{color.R} {color.G} {color.B}");
                }
            }
        }

        private static void WriteGIMPPalFile(Stream stream, string name, Color[] colorPalette)
        {
            using (StreamWriter writer = new StreamWriter(stream))
            {
                writer.WriteLine("GIMP Palette");
                writer.WriteLine($"Name: {name}");
                writer.WriteLine("Columns: 0");
                writer.WriteLine("#");

                foreach (Color color in colorPalette)
                {
                    writer.WriteLine($"{color.R,3} {color.G,3} {color.B,3}\tUntitled");
                }
            }
        }

        private static void WritePaintNETPalFile(Stream stream, string name, Color[] colorPalette)
        {
            using (StreamWriter writer = new StreamWriter(stream))
            {
                writer.WriteLine("; Paint.NET Palette");
                writer.WriteLine($"; {name}");

                foreach (Color color in colorPalette)
                {
                    writer.WriteLine(color.ToArgb().ToString("X8"));
                }
            }
        }
    }
}
