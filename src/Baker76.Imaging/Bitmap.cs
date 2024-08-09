using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Baker76.Imaging.Aseprite.Layer;

namespace Baker76.Imaging
{
    public class Bitmap
    {
        private const int BMP_MIN_FILE_SIZE = 54;
        private const int BMP_MIN_DIB_HEADER_SIZE = 40;
        private const int BMP_FILE_HEADER_SIZE = 14;

        private static bool IsValidBmpFile(Stream stream, out int imageWidth, out int imageHeight, out uint paletteOffset, out uint imageOffset, out ushort bpp, out uint colorCount)
        {
            byte[] bmpHeader = new byte[54];
            imageWidth = 0;
            imageHeight = 0;
            paletteOffset = 0;
            imageOffset = 0;
            bpp = 0;
            colorCount = 0;

            // Open the BMP file and validate its header.
            if (stream.Read(bmpHeader, 0, bmpHeader.Length) != bmpHeader.Length)
            {
                Console.WriteLine($"Can't read the BMP header.");
                return false;
            }

            if (bmpHeader[0] != 'B' || bmpHeader[1] != 'M')
            {
                Console.WriteLine("Not a BMP file.");
                return false;
            }

            uint fileSize = BitConverter.ToUInt32(bmpHeader, 2);
            if (fileSize < BMP_MIN_FILE_SIZE)
            {
                Console.WriteLine("Invalid size of BMP file.");
                return false;
            }

            imageOffset = BitConverter.ToUInt32(bmpHeader, 10);
            if (imageOffset >= fileSize)
            {
                Console.WriteLine("Invalid header of BMP file.");
                return false;
            }

            uint dibHeaderSize = BitConverter.ToUInt32(bmpHeader, 14);
            if (dibHeaderSize < BMP_MIN_DIB_HEADER_SIZE)
            {
                Console.WriteLine("Invalid/unsupported header of BMP file.");
                return false;
            }

            paletteOffset = BMP_FILE_HEADER_SIZE + dibHeaderSize;

            imageWidth = BitConverter.ToInt32(bmpHeader, 18);
            if (imageWidth == 0)
            {
                Console.WriteLine("Invalid image width in BMP file.");
                return false;
            }

            imageHeight = BitConverter.ToInt32(bmpHeader, 22);
            if (imageHeight == 0)
            {
                Console.WriteLine("Invalid image height in BMP file.");
                return false;
            }

            bpp = BitConverter.ToUInt16(bmpHeader, 28);
            if (bpp != 4 && bpp != 8)
            {
                Console.WriteLine("Not a 4-bit or 8-bit BMP file.");
                return false;
            }

            colorCount = BitConverter.ToUInt32(bmpHeader, 46);

            uint imageSize = (uint)(imageWidth * Math.Abs(imageHeight));

            if (bpp == 4)
                imageSize >>= 1;

            if (imageSize >= fileSize)
            {
                Console.WriteLine("Invalid image size in BMP file.");
                return false;
            }

            uint compression = BitConverter.ToUInt32(bmpHeader, 30);
            if (compression != 0)
            {
                Console.WriteLine("Not an uncompressed BMP file.");
                return false;
            }

            return true;
        }

        public static async Task<Image> ReadAsync(Stream stream)
        {
            return await Task.Run(() =>
            {
                return Read(stream);
            });
        }

        public static Image Read(string fileName)
        {
            using (FileStream fileStream = File.OpenRead(fileName))
            {
                return Read(fileStream);
            }
        }

        public static Image Read(Stream stream)
        {
            Image pngImage = null;
            byte[] imageBytes;
            int imageWidth;
            int imageHeight;
            bool bottomToTopImage;
            int paddedImageWidth;
            int imageSize;
            byte[] paletteBytes = new byte[1024];
            List<Color> colorList = new List<Color>();
            uint paletteOffset;
            uint imageOffset;
            ushort bpp;
            uint colorCount;

            if (!IsValidBmpFile(stream, out imageWidth,out imageHeight, out paletteOffset, out imageOffset, out bpp, out colorCount))
            {
                Console.WriteLine($"The file is not a valid or supported BMP file.");
                return null;
            }

            // Allocate memory for image data.
            // Note: Image width is padded to a multiple of 4 bytes.
            bottomToTopImage = (imageHeight > 0);
            paddedImageWidth = (imageWidth + 3) & ~0x03;
            imageSize = paddedImageWidth * imageHeight;
            if (bpp == 4)
                imageSize >>= 1;
            imageBytes = new byte[imageSize];

            // Read the palette and image data.
            if (stream.Seek(paletteOffset, SeekOrigin.Begin) != paletteOffset)
            {
                Console.WriteLine($"Can't access the BMP palette in file.");
                return null;
            }

            colorCount = (colorCount == 0 ? (bpp == 4 ? 16U : 256U) : colorCount);

            if (stream.Read(paletteBytes, 0, (int)colorCount * 4) != colorCount * 4)
            {
                Console.WriteLine($"Can't read the BMP palette in file.");
                return null;
            }

            if (stream.Seek(imageOffset, SeekOrigin.Begin) != imageOffset)
            {
                Console.WriteLine($"Can't access the BMP image data in file.");
                return null;
            }

            if (stream.Read(imageBytes, 0, (int)imageSize) != imageSize)
            {
                Console.WriteLine($"Can't read the BMP image data in file.");
                return null;
            }

            for (int i = 0; i < colorCount; i++)
            {
                // BGRA to ARGB
                byte b8 = paletteBytes[i * 4 + 0];
                byte g8 = paletteBytes[i * 4 + 1];
                byte r8 = paletteBytes[i * 4 + 2];
                byte a8 = paletteBytes[i * 4 + 3];

                colorList.Add(Color.FromArgb(0xff, r8, g8, b8));
            }

            // Convert 4-bit to 8-bit data
            if (bpp == 4)
            {
                bpp = 8;
                imageSize <<= 1;

                byte[] newImage = new byte[imageSize];

                for (int i = 0; i < imageBytes.Length; i++)
                {
                    byte value = imageBytes[i];
                    newImage[i * 2] = (byte)(value >> 4);
                    newImage[i * 2 + 1] = (byte)(value & 0xf);
                }

                imageBytes = newImage;
            }

            if (bottomToTopImage)
            {
                int rowSize = paddedImageWidth;
                byte[] tempRow = new byte[rowSize];

                for (int row = 0; row < imageHeight / 2; row++)
                {
                    int topRowIndex = row * rowSize;
                    int bottomRowIndex = (imageHeight - row - 1) * rowSize;

                    Array.Copy(imageBytes, topRowIndex, tempRow, 0, rowSize);
                    Array.Copy(imageBytes, bottomRowIndex, imageBytes, topRowIndex, rowSize);
                    Array.Copy(tempRow, 0, imageBytes, bottomRowIndex, rowSize);
                }
            }

            Palette palette = new Palette(colorList.ToArray());
            return new Image(imageWidth, imageHeight, 8, palette, imageBytes);
        }

        public static bool Write(string fileName, Image pngImage)
        {
            byte[] palette = new byte[pngImage.Palette.Colors.Count * 4];
            byte[] imageBytes = pngImage.PixelData;

            // Convert RGBA to BGRA
            if (pngImage.BitsPerPixel == 8)
            {
                for (int i = 0; i < pngImage.Palette.Colors.Count; i++)
                {
                    Color color = pngImage.Palette.Colors[i];
                    palette[i * 4] = (byte)color.B;
                    palette[i * 4 + 1] = (byte)color.G;
                    palette[i * 4 + 2] = (byte)color.R;
                    palette[i * 4 + 3] = (byte)color.A;
                }
            }
            else if (pngImage.BitsPerPixel == 24)
            {
                // Convert RGBA to BGR
                for (int i = 0; i < imageBytes.Length; i += 3)
                {
                    byte r = imageBytes[i];
                    byte g = imageBytes[i + 1];
                    byte b = imageBytes[i + 2];

                    imageBytes[i] = b;
                    imageBytes[i + 1] = g;
                    imageBytes[i + 2] = r;
                }
            }
            else if (pngImage.BitsPerPixel == 32)
            {
                // Convert RGBA to BGRA
                for (int i = 0; i < imageBytes.Length; i += 4)
                {
                    byte r = imageBytes[i];
                    byte g = imageBytes[i + 1];
                    byte b = imageBytes[i + 2];
                    byte a = imageBytes[i + 3];

                    imageBytes[i] = b;
                    imageBytes[i + 1] = g;
                    imageBytes[i + 2] = r;
                    imageBytes[i + 3] = a;
                }
            }

            return Write(fileName, pngImage.Width, pngImage.Height, imageBytes, palette, pngImage.BitsPerPixel);
        }

        public static bool Write(string fileName, int imageWidth, int imageHeight, byte[] imageBytes, byte[] paletteBytes, int bpp)
        {
            // Calculate file size and padding
            int bytesPerPixel = bpp / 8;
            int bytesPerRow = imageWidth * bytesPerPixel;
            int stride = (bytesPerRow + 3) & ~3;
            int dataSize = stride * imageHeight;
            int paletteSize = (bpp == 8 ? 1024 : 0);
            int fileSize = BMP_MIN_FILE_SIZE + paletteSize + dataSize;

            // Create BMP header
            byte[] bmpBytes = new byte[fileSize];
            bmpBytes[0] = (byte)'B';
            bmpBytes[1] = (byte)'M';
            Array.Copy(BitConverter.GetBytes(fileSize), 0, bmpBytes, 2, 4); // bfSize
            Array.Copy(BitConverter.GetBytes(BMP_MIN_FILE_SIZE + paletteSize), 0, bmpBytes, 10, 4); // bfOffBits
            Array.Copy(BitConverter.GetBytes(BMP_MIN_DIB_HEADER_SIZE), 0, bmpBytes, 14, 4); // biSize
            Array.Copy(BitConverter.GetBytes(imageWidth), 0, bmpBytes, 18, 4); // biWidth
            Array.Copy(BitConverter.GetBytes(imageHeight), 0, bmpBytes, 22, 4); // biHeight
            Array.Copy(BitConverter.GetBytes(1), 0, bmpBytes, 26, 2);  // biPlanes
            Array.Copy(BitConverter.GetBytes((ushort)bpp), 0, bmpBytes, 28, 2); // biBitCount
            Array.Copy(BitConverter.GetBytes(0), 0, bmpBytes, 30, 4); // biCompression
            Array.Copy(BitConverter.GetBytes(dataSize), 0, bmpBytes, 34, 4); // biSizeImage
            Array.Copy(BitConverter.GetBytes(2834), 0, bmpBytes, 38, 4); // biXPelsPerMeter
            Array.Copy(BitConverter.GetBytes(2834), 0, bmpBytes, 42, 4); // biYPelsPerMeter

            // Write palette for 8 bpp
            if (bpp == 8)
            {
                Array.Copy(paletteBytes, 0, bmpBytes, BMP_MIN_FILE_SIZE, paletteBytes.Length);
            }

            // Write image data
            int dataIndex = BMP_MIN_FILE_SIZE + paletteSize;
            int sourceIndex = 0;
            for (int y = imageHeight - 1; y >= 0; y--)
            {
                for (int x = 0; x < imageWidth; x++)
                {
                    int index = dataIndex + (y * stride) + x * bytesPerPixel;
                    Array.Copy(imageBytes, sourceIndex, bmpBytes, index, bytesPerPixel);
                    sourceIndex += bytesPerPixel;
                }
            }

            File.WriteAllBytes(fileName, bmpBytes);

            return true;
        }
    }
}
