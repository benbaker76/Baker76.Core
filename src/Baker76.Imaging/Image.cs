using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using static Baker76.Imaging.Aseprite;

namespace Baker76.Imaging
{
    [Flags]
    public enum ImageAttributes
    {
        None = 0,
        Rotate = (1 << 1),
        MirrorY = (1 << 2),
        MirrorX = (1 << 3),
        MirrorX_Y = MirrorX | MirrorY
    };

    public enum RotationAngle
    {
        Rotate90,
        Rotate180,
        Rotate270
    }

    public enum InterpolationMode
    {
        NearestNeighbor,
        Bilinear
    }

    public class Image
    {
        public readonly int Width;
        public readonly int Height;
        public readonly int BitsPerPixel;
        public readonly int BytesPerPixel;
        public readonly int Scanline;

        public Palette Palette;
        public readonly byte[] PixelData;

        private Color transparentColor;

        /// <summary>
        /// Image uses a transparent background color
        /// </summary>
        public bool IsTransparent
        {
            get
            {
                if (BitsPerPixel <= 8)
                {
                    return Palette.IsTransparent;
                }
                
                return TransparentColor != null;
            }
        }

        /// <summary>
        /// Color used as transparent background
        /// </summary>
        public Color TransparentColor
        {
            get => transparentColor;
            set
            {
                if (BitsPerPixel <= 8)
                {
                    throw new ArgumentException("Transparent color must be set in palette for an indexed image", nameof(TransparentColor));
                }

                transparentColor = value;
            }
        }

        /// <summary>
        /// Create new image
        /// </summary>
        /// <param name="width">Width of image</param>
        /// <param name="height">Height of image</param>
        /// <param name="bitsPerPixel">Bits per pixel used in image</param>
        public Image(int width, int height, int bitsPerPixel)
        {
            if (!IsBitsPerPixelValid(bitsPerPixel))
            {
                throw new ArgumentException("Only 1, 4, 8, 24 and 32 bits per pixel is supported",
                    nameof(bitsPerPixel));
            }
            
            Width = width;
            Height = height;
            BitsPerPixel = bitsPerPixel;
            BytesPerPixel = bitsPerPixel <= 8 ? 1 : bitsPerPixel / 8;
            Scanline = bitsPerPixel <= 8 ? width : width * BytesPerPixel;
            transparentColor = null;
            Palette = bitsPerPixel <= 8 ? new Palette(System.Convert.ToInt32(System.Math.Pow(2, bitsPerPixel))) : new Palette();
            PixelData = new byte[Scanline * height];
        }

        private static bool IsBitsPerPixelValid(int bitsPerPixel)
        {
            return bitsPerPixel == 1 ||
                   bitsPerPixel == 4 || bitsPerPixel == 8 || bitsPerPixel == 24 || bitsPerPixel == 32;
        }
        
        /// <summary>
        /// Create new image without no palette
        /// </summary>
        /// <param name="width">Width of image</param>
        /// <param name="height">Height of image</param>
        /// <param name="bitsPerPixel">Bits per pixel used in image</param>
        /// <param name="pixelData">Pixel data containing palette color index for 1, 4 and 8 bits per pixel images, rgb colors for 24 bits per pixel images or rgba for 32 bits per pixel images</param>
        public Image(int width, int height, int bitsPerPixel, IEnumerable<byte> pixelData)
            : this(width, height, bitsPerPixel, new Palette(), pixelData)
        {
        }
        
        /// <summary>
        /// Create new image with palette and pixel data
        /// </summary>
        /// <param name="width">Width of image</param>
        /// <param name="height">Height of image</param>
        /// <param name="bitsPerPixel">Bits per pixel used in image</param>
        /// <param name="palette">Palette to use for 1, 4 and 8 bits per pixel images. Palette must be empty for 24 and 32 bits per pixel images.</param>
        /// <param name="pixelData">Pixel data containing palette color index for 1, 4 and 8 bits per pixel images, rgb colors for 24 bits per pixel images or rgba for 32 bits per pixel images</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public Image(int width, int height, int bitsPerPixel, Palette palette, IEnumerable<byte> pixelData)
        {
            if (!IsBitsPerPixelValid(bitsPerPixel))
            {
                throw new ArgumentException("Only 1, 4, 8, 24 and 32 bits per pixel is supported",
                    nameof(bitsPerPixel));
            }
            
            if (bitsPerPixel > 8 && palette.Colors.Count > 0)
            {
                throw new ArgumentException("Palette must not have any colors for 24 and 32 bits per pixel images",
                    nameof(palette));
            }

            Width = width;
            Height = height;
            BitsPerPixel = bitsPerPixel;
            BytesPerPixel = bitsPerPixel <= 8 ? 1 : bitsPerPixel / 8;
            Scanline = bitsPerPixel <= 8 ? width : width * BytesPerPixel;
            transparentColor = null;
            Palette = palette;
            PixelData = pixelData.ToArray();

            var pixelDataSize = width * height * ((bitsPerPixel <= 8 ? 8 : bitsPerPixel) / 8);
            if (PixelData.Length != pixelDataSize)
            {
                throw new ArgumentOutOfRangeException(
                    $"Image with dimension {width} x {height} and {BitsPerPixel} bits per pixel must have pixel data size of {pixelDataSize} bytes");
            }
        }

        /// <summary>
        /// Create new image with palette and pixel data
        /// </summary>
        /// <param name="width">Width of image</param>
        /// <param name="height">Height of image</param>
        /// <param name="bitsPerPixel">Bits per pixel used in image</param>
        /// <param name="palette">Palette to use for 1, 4 and 8 bits per pixel images. Palette must be empty for 24 and 32 bits per pixel images.</param>
        /// <param name="pixelData">Pixel data containing palette color index for 1, 4 and 8 bits per pixel images, rgb colors for 24 bits per pixel images or rgba for 32 bits per pixel images</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public Image(int width, int height, int bitsPerPixel, Palette palette)
        {
            if (!IsBitsPerPixelValid(bitsPerPixel))
            {
                throw new ArgumentException("Only 1, 4, 8, 24 and 32 bits per pixel is supported",
                    nameof(bitsPerPixel));
            }

            if (bitsPerPixel > 8 && palette.Colors.Count > 0)
            {
                throw new ArgumentException("Palette must not have any colors for 24 and 32 bits per pixel images",
                    nameof(palette));
            }

            Width = width;
            Height = height;
            BitsPerPixel = bitsPerPixel;
            BytesPerPixel = bitsPerPixel <= 8 ? 1 : bitsPerPixel / 8;
            Scanline = bitsPerPixel <= 8 ? width : width * BytesPerPixel;
            transparentColor = null;
            Palette = palette;
            PixelData = new byte[Scanline * height];
        }

        public void DrawImage(Image pngImage)
        {
            DrawImage(pngImage, new Rectangle(0, 0, pngImage.Width, pngImage.Height), new Rectangle(0, 0, pngImage.Width, pngImage.Height));
        }

        public void DrawImage(Image pngImage, Rectangle destRect)
        {
            DrawImage(pngImage, destRect, new Rectangle(0, 0, pngImage.Width, pngImage.Height));
        }

        public void DrawImage(Image pngImage, Rectangle destRect, Rectangle srcRect)
        {
            Rectangle destIntersect = Rectangle.Intersect(new Rectangle(0, 0, Width, Height), destRect);

            if (destIntersect.IsEmpty)
                return;

            Rectangle srcIntersect = Rectangle.Intersect(new Rectangle(0, 0, pngImage.Width, pngImage.Height), srcRect);

            if (srcIntersect.IsEmpty)
                return;

            Rectangle clippedDestRect = new Rectangle(destIntersect.X - destRect.X, destIntersect.Y - destRect.Y, destIntersect.Width, destIntersect.Height);
            Rectangle clippedSrcRect = new Rectangle(srcIntersect.X - srcRect.X, srcIntersect.Y - srcRect.Y, srcIntersect.Width, srcIntersect.Height);

            for (int destRow = clippedDestRect.Top, srcRow = clippedSrcRect.Top; destRow < clippedDestRect.Bottom; destRow++, srcRow++)
            {
                for (int destCol = clippedDestRect.Left, srcCol = clippedSrcRect.Left; destCol < clippedDestRect.Right; destCol++, srcCol++)
                {
                    int destOffset = (destRect.Y + destRow) * Scanline + (destRect.X + destCol) * BytesPerPixel;
                    int srcOffset = (srcRect.Y + srcRow) * pngImage.Scanline + (srcRect.X + srcCol) * pngImage.BytesPerPixel;

                    switch (BitsPerPixel)
                    {
                        case 1:
                        case 2:
                        case 4:
                        case 8:
                            int destBitIndex = destOffset * 8;
                            int srcBitIndex = srcOffset * 8;

                            for (int bit = 0; bit < BitsPerPixel; bit++)
                            {
                                int srcByteIndex = srcBitIndex / 8;
                                int destByteIndex = destBitIndex / 8;

                                int srcBitOffset = srcBitIndex % 8;
                                int destBitOffset = destBitIndex % 8;

                                byte srcByte = pngImage.PixelData[srcByteIndex];
                                byte destByte = PixelData[destByteIndex];

                                byte srcMask = (byte)(1 << (7 - srcBitOffset));
                                byte destMask = (byte)(1 << (7 - destBitOffset));

                                if ((srcByte & srcMask) != 0)
                                {
                                    destByte |= destMask;
                                }
                                else
                                {
                                    destByte &= (byte)~destMask;
                                }

                                PixelData[destByteIndex] = destByte;

                                srcBitIndex++;
                                destBitIndex++;
                            }
                            break;
                        case 24:
                        case 32:
                            if (pngImage.BitsPerPixel <= 8)
                            {
                                Color color = pngImage.Palette.Colors[pngImage.PixelData[srcOffset]];
                                PixelData[destOffset] = (byte)color.R;
                                PixelData[destOffset + 1] = (byte)color.G;
                                PixelData[destOffset + 2] = (byte)color.B;

                                if (BitsPerPixel == 32)
                                    PixelData[destOffset + 3] = 0xff;
                            }
                            else
                            {
                                if (BitsPerPixel == 32)
                                {
                                    if (pngImage.BitsPerPixel == 32)
                                    {
                                        if (pngImage.PixelData[srcOffset + 3] != 0)
                                        {
                                            PixelData[destOffset] = pngImage.PixelData[srcOffset];
                                            PixelData[destOffset + 1] = pngImage.PixelData[srcOffset + 1];
                                            PixelData[destOffset + 2] = pngImage.PixelData[srcOffset + 2];

                                            if (PixelData[destOffset + 3] == 0)
                                                PixelData[destOffset + 3] = pngImage.PixelData[srcOffset + 3];
                                        }
                                    }
                                    else
                                    {
                                        PixelData[destOffset] = pngImage.PixelData[srcOffset];
                                        PixelData[destOffset + 1] = pngImage.PixelData[srcOffset + 1];
                                        PixelData[destOffset + 2] = pngImage.PixelData[srcOffset + 2];
                                        PixelData[destOffset + 2] = 0xff;
                                    }
                                }
                                else
                                {
                                    if (pngImage.BitsPerPixel == 32)
                                    {
                                        if (pngImage.PixelData[srcOffset + 3] != 0)
                                        {
                                            PixelData[destOffset] = pngImage.PixelData[srcOffset];
                                            PixelData[destOffset + 1] = pngImage.PixelData[srcOffset + 1];
                                            PixelData[destOffset + 2] = pngImage.PixelData[srcOffset + 2];
                                        }
                                    }
                                    else
                                    {
                                        PixelData[destOffset] = pngImage.PixelData[srcOffset];
                                        PixelData[destOffset + 1] = pngImage.PixelData[srcOffset + 1];
                                        PixelData[destOffset + 2] = pngImage.PixelData[srcOffset + 2];
                                    }
                                }
                            }
                            break;
                        default:
                            throw new NotSupportedException("Unsupported bits per pixel format.");
                    }
                }
            }
        }

        public Image Rotate(RotationAngle angle)
        {
            int newWidth, newHeight;
            byte[] newPixelData;

            switch (angle)
            {
                case RotationAngle.Rotate90:
                    newWidth = Height;
                    newHeight = Width;
                    newPixelData = new byte[newWidth * newHeight * (BitsPerPixel / 8)];
                    for (int y = 0; y < Height; y++)
                    {
                        for (int x = 0; x < Width; x++)
                        {
                            int newX = newWidth - 1 - y;
                            int newY = x;
                            CopyPixel(x, y, newX, newY, newPixelData, newWidth);
                        }
                    }
                    break;

                case RotationAngle.Rotate180:
                    newWidth = Width;
                    newHeight = Height;
                    newPixelData = new byte[newWidth * newHeight * (BitsPerPixel / 8)];
                    for (int y = 0; y < Height; y++)
                    {
                        for (int x = 0; x < Width; x++)
                        {
                            int newX = newWidth - 1 - x;
                            int newY = newHeight - 1 - y;
                            CopyPixel(x, y, newX, newY, newPixelData, newWidth);
                        }
                    }
                    break;

                case RotationAngle.Rotate270:
                    newWidth = Height;
                    newHeight = Width;
                    newPixelData = new byte[newWidth * newHeight * (BitsPerPixel / 8)];
                    for (int y = 0; y < Height; y++)
                    {
                        for (int x = 0; x < Width; x++)
                        {
                            int newX = y;
                            int newY = newHeight - 1 - x;
                            CopyPixel(x, y, newX, newY, newPixelData, newWidth);
                        }
                    }
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(angle), angle, null);
            }

            return new Image(newWidth, newHeight, BitsPerPixel, Palette, newPixelData);
        }

        public Image Scale(int newWidth, int newHeight, bool keepAspect = false, InterpolationMode mode = InterpolationMode.Bilinear)
        {
            // Force NearestNeighbor for indexed images
            if (BitsPerPixel <= 8)
            {
                mode = InterpolationMode.NearestNeighbor;
            }

            if (keepAspect)
            {
                float aspectRatio = (float)Width / Height;
                float newAspectRatio = (float)newWidth / newHeight;

                if (aspectRatio > newAspectRatio)
                    newHeight = (int)(newWidth / aspectRatio);
                else
                    newWidth = (int)(newHeight * aspectRatio);
            }

            byte[] newPixelData = new byte[newWidth * newHeight * (BitsPerPixel / 8)];
            float xRatio = (float)Width / newWidth;
            float yRatio = (float)Height / newHeight;

            for (int y = 0; y < newHeight; y++)
            {
                for (int x = 0; x < newWidth; x++)
                {
                    if (mode == InterpolationMode.NearestNeighbor)
                    {
                        int srcX = (int)(x * xRatio);
                        int srcY = (int)(y * yRatio);
                        CopyPixel(srcX, srcY, x, y, newPixelData, newWidth);
                    }
                    else if (mode == InterpolationMode.Bilinear)
                    {
                        float srcX = x * xRatio;
                        float srcY = y * yRatio;
                        PerformBilinearInterpolation(srcX, srcY, x, y, newPixelData, newWidth);
                    }
                }
            }

            return new Image(newWidth, newHeight, BitsPerPixel, Palette, newPixelData);
        }

        private void CopyPixel(int srcX, int srcY, int destX, int destY, byte[] destData, int destWidth)
        {
            int srcOffset = (srcY * Width + srcX) * (BitsPerPixel / 8);
            int destOffset = (destY * destWidth + destX) * (BitsPerPixel / 8);
            Array.Copy(PixelData, srcOffset, destData, destOffset, BitsPerPixel / 8);
        }

        private void PerformBilinearInterpolation(float srcX, float srcY, int destX, int destY, byte[] destData, int destWidth)
        {
            int x1 = (int)srcX;
            int y1 = (int)srcY;
            int x2 = System.Math.Min(x1 + 1, Width - 1);
            int y2 = System.Math.Min(y1 + 1, Height - 1);

            float xLerp = srcX - x1;
            float yLerp = srcY - y1;

            for (int b = 0; b < BitsPerPixel / 8; b++)
            {
                byte c11 = PixelData[(y1 * Width + x1) * (BitsPerPixel / 8) + b];
                byte c12 = PixelData[(y1 * Width + x2) * (BitsPerPixel / 8) + b];
                byte c21 = PixelData[(y2 * Width + x1) * (BitsPerPixel / 8) + b];
                byte c22 = PixelData[(y2 * Width + x2) * (BitsPerPixel / 8) + b];

                float col1 = c11 * (1 - xLerp) + c12 * xLerp;
                float col2 = c21 * (1 - xLerp) + c22 * xLerp;
                byte finalColor = (byte)(col1 * (1 - yLerp) + col2 * yLerp);

                destData[(destY * destWidth + destX) * (BitsPerPixel / 8) + b] = finalColor;
            }
        }

        public Image ToRGBA()
        {
            if (BitsPerPixel > 8)
            {
                throw new InvalidOperationException("ToRGBA method can only be used with <= 8 bits per pixel images.");
            }

            byte[] pixelData = new byte[Width * Height * 4];

            for (int i = 0; i < PixelData.Length; i++)
            {
                Color color = Palette.Colors[PixelData[i]];
                pixelData[i * 4] = (byte)color.R;
                pixelData[i * 4 + 1] = (byte)color.G;
                pixelData[i * 4 + 2] = (byte)color.B;
                pixelData[i * 4 + 3] = (byte)color.A;
            }

            return new Image(Width, Height, 32, new Palette(), pixelData);
        }

        public void Clear(Color color)
        {
            if (BitsPerPixel <= 8)
            {
                int paletteIndex = Palette.GetColorIndex(color);
                if (paletteIndex == -1)
                    return;
                Clear(paletteIndex);
                return;
            }

            byte[] colorBytes = { (byte)color.R, (byte)color.G, (byte)color.B, (byte)color.A };

            for (int i = 0; i < Height; i++)
            {
                int rowOffset = i * Scanline;
                for (int j = 0; j < Width; j++)
                {
                    int pixelOffset = rowOffset + j * BytesPerPixel;
                    for (int k = 0; k < BytesPerPixel; k++)
                    {
                        PixelData[pixelOffset + k] = colorBytes[k];
                    }
                }
            }
        }

        public void Clear(int index)
        {
            if (BitsPerPixel > 8)
            {
                throw new InvalidOperationException("Clear(int index) method can only be used with <= 8 bits per pixel images.");
            }

            for (int i = 0; i < PixelData.Length; i++)
                PixelData[i] = (byte)index;
        }

        public void SwapColors(int indexA, int indexB)
        {
            if (BitsPerPixel > 8)
            {
                throw new InvalidOperationException("SwapColors(int indexA, int indexB) method can only be used with <= 8 bits per pixel images.");
            }

            for (int i = 0; i < PixelData.Length; i++)
            {
                if (PixelData[i] == indexA)
                    PixelData[i] = (byte)indexB;
                else if (PixelData[i] == indexB)
                    PixelData[i] = (byte)indexA;
            }
        }

        public void AddIndexOffset(int offset)
        {
            if (BitsPerPixel > 8)
            {
                throw new InvalidOperationException("AddIndexOffset(int offset) method can only be used with <= 8 bits per pixel images.");
            }

            for (int i = 0; i < PixelData.Length; i++)
            {
                PixelData[i] = (byte)(PixelData[i] + offset);
            }
        }

        public void RemapColors(Color[] paletteColors, int backgroundIndex, DistanceType distanceType)
        {
            Dictionary<int, int> indexRemap = Palette.GetPaletteRemap(paletteColors, backgroundIndex, distanceType);

            RemapColors(indexRemap);
        }

        public void RemapColors(Dictionary<int, int> indexRemap)
        {
            if (BitsPerPixel > 8)
            {
                throw new InvalidOperationException("RemapColors(Dictionary<int, int> indexRemap) method can only be used with <= 8 bits per pixel images.");
            }

            for (int i = 0; i < PixelData.Length; i++)
            {
                int index = PixelData[i];
                PixelData[i] = (byte)indexRemap[index];
            }
        }

        public Color[] GetUsedColors()
        {
            if (BitsPerPixel > 8)
            {
                throw new ArgumentException("Only 1, 2, 4, 8 bits per pixel images can set pixel using palette color");
            }

            List<Color> colorList = new List<Color>();

            for (int i = 0; i < PixelData.Length; i++)
            {
                int index = PixelData[i];
                Color color = Palette.Colors[index];

                if (!colorList.Contains(color))
                    colorList.Add(color);
            }

            return colorList.ToArray();
        }

        public void SortPalette(SortColorMode sortColorMode, HSBSortMode hsbSortMode = HSBSortMode.HSB, int transparentIndex = -1)
        {
            List<ColorNode> colorList = new List<ColorNode>();

            for (int i = 0; i < Palette.Colors.Count; i++)
                colorList.Add(new ColorNode(i, Palette.Colors[i]));

            SortColorList(colorList, sortColorMode, hsbSortMode, transparentIndex);

            Dictionary<int, int> indexRemap = new Dictionary<int, int>();

            for (int i = 0; i < colorList.Count; i++)
                indexRemap[colorList[i].Index] = i;

            RemapColors(indexRemap);
        }

        public static void SortColorList(List<ColorNode> colorList, SortColorMode sortColorMode, HSBSortMode hsbSortMode = HSBSortMode.HSB, int transparentIndex = -1)
        {
            switch (sortColorMode)
            {
                case SortColorMode.Sqrt:
                    colorList.Sort(new SqrtSorter(transparentIndex));
                    break;
                case SortColorMode.HSB:
                    colorList.Sort(new HSBSorter(hsbSortMode, transparentIndex));
                    break;
                case SortColorMode.Lab:
                    colorList.Sort(new LabSorter(transparentIndex));
                    break;
            }
        }

        public Pixel GetPixel(int x, int y)
        {
            if (BitsPerPixel < 8)
            {
                throw new NotSupportedException();
            }

            if (BitsPerPixel == 8)
            {
                var offset = (Scanline * y) + x;
                var paletteColor = PixelData[offset];
                var color = Palette.Colors[paletteColor];
                return new Pixel
                {
                    R = color.R,
                    G = color.G,
                    B = color.B,
                    A = color.A,
                    PaletteColor = paletteColor
                };
            }
            else
            {
                var offset = (Scanline * y) + (x * (BitsPerPixel / 8));

                return new Pixel
                {
                    R = PixelData[offset + 2],
                    G = PixelData[offset + 1],
                    B = PixelData[offset],
                    A = BitsPerPixel == 32 ? PixelData[offset + 3] : 0,
                    PaletteColor = 0
                };
            }
        }

        public void SetPixel(int x, int y, int paletteColor)
        {
            if (BitsPerPixel > 8)
            {
                throw new ArgumentException("Only 1, 2, 4, 8 bits per pixel images can set pixel using palette color", nameof(paletteColor));
            }
            
            var pixelOffset = (Scanline * y) + x;
            PixelData[pixelOffset] = (byte)paletteColor;
        }

        public void SetPixel(int x, int y, Color color)
        {
            SetPixel(x, y, new Pixel
            {
                R = color.R,
                G = color.G,
                B = color.B,
                A = color.A
            });
        }

        public void SetPixel(int x, int y, int r, int g, int b, int a = 255)
        {
            SetPixel(x, y, new Pixel
            {
                R = r,
                G = g,
                B = b,
                A = a
            });
        }
        
        public void SetPixel(int x, int y, Pixel pixel)
        {
            if (BitsPerPixel <= 8)
            {
                SetPixel(x, y, pixel.PaletteColor);
                return;
            }
            
            var pixelOffset = Scanline * y + x * (BitsPerPixel / 8);

            PixelData[pixelOffset] = (byte)pixel.R;
            PixelData[pixelOffset + 1] = (byte)pixel.G;
            PixelData[pixelOffset + 2] = (byte)pixel.B;

            if (BitsPerPixel <= 24)
            {
                return;
            }

            PixelData[pixelOffset + 3] = (byte)pixel.A;
        }

        public UInt64 GetHash()
        {
            return GetHash(PixelData);
        }

        public UInt64 GetHash(ImageAttributes imageAttributes)
        {
            return GetHash(GetPixelData(imageAttributes));
        }

        public UInt64 GetHash(byte[] data)
        {
            const UInt64 fnvOffsetBasis = 0xcbf29ce484222325;
            const UInt64 fnvPrime = 0x100000001b3;

            UInt64 hash = fnvOffsetBasis;

            foreach (var value in data)
            {
                hash ^= value;
                hash *= fnvPrime;
            }

            return hash;
        }

        public byte[] GetPixelData(ImageAttributes imageAttributes)
        {
            bool horizontallyFlipped = (imageAttributes & ImageAttributes.MirrorX) != 0;
            bool verticallyFlipped = (imageAttributes & ImageAttributes.MirrorY) != 0;
            bool rotatedLeft = (imageAttributes & ImageAttributes.Rotate) != 0;
            bool rotatedRight = (imageAttributes & (ImageAttributes.Rotate | ImageAttributes.MirrorX_Y)) == (ImageAttributes.Rotate | ImageAttributes.MirrorX_Y);
            byte[] transformedPixelData = new byte[PixelData.Length];

            int originalWidth = Width;
            int originalHeight = Height;
            int bytesPerPixel = BytesPerPixel;
            int originalScanline = Scanline;

            int newWidth = rotatedLeft || rotatedRight ? originalHeight : originalWidth;
            int newHeight = rotatedLeft || rotatedRight ? originalWidth : originalHeight;
            int newScanline = (newWidth * bytesPerPixel + 3) & ~3; // Padding

            for (int i = 0; i < newHeight; i++)
            {
                int rowIndex = verticallyFlipped ? newHeight - 1 - i : i;
                int rowOffset = rowIndex * newScanline;

                for (int j = 0; j < newWidth; j++)
                {
                    int columnIndex = horizontallyFlipped ? newWidth - 1 - j : j;

                    int oldRowIndex = rotatedLeft ? j : (rotatedRight ? originalHeight - 1 - j : i);
                    int oldColumnIndex = rotatedLeft ? originalWidth - 1 - i : (rotatedRight ? i : j);

                    int pixelOffset = oldRowIndex * originalScanline + oldColumnIndex * bytesPerPixel;

                    for (int k = 0; k < bytesPerPixel; k++)
                    {
                        transformedPixelData[rowOffset + columnIndex * bytesPerPixel + k] = PixelData[pixelOffset + k];
                    }
                }
            }

            return transformedPixelData;
        }

        public static Image Load(string fileName)
        {
            return PngReader.Read(fileName);
        }

        public void Save(string fileName)
        {
            PngWriter.Write(fileName, this);
        }

        public Size Size => new Size(Width, Height);
    }
}