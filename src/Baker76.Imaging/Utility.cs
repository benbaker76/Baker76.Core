using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace Baker76.Imaging
{
    public class ImageOptions
    {
        public bool InsertMagenta = true;
        public bool RemapColors = true;
        public Palette Palette = null;
        public int PaletteSlot = 0;
        public int ColorCount = 256;
        public DistanceType DistanceType = DistanceType.CIEDE2000;
        public int FrameWidth = 0;
        public int FrameHeight = 0;
        public int FrameCount = 0;
        public int FrameDuration = 100;
    }

    public class Utility
    {
        public static Size GetImageSize(string fileName)
        {
            using (BinaryReader binaryReader = new BinaryReader(File.Open(fileName, FileMode.Open)))
            {
                if (binaryReader.ReadByte() == 0x42 && binaryReader.ReadByte() == 0x4D)
                    return DecodeBmp(binaryReader);
                else if (binaryReader.ReadByte() == 0x89 && binaryReader.ReadByte() == 0x50 && binaryReader.ReadByte() == 0x4E && binaryReader.ReadByte() == 0x47)
                    return DecodePng(binaryReader);
                else
                    throw new Exception("Unsupported file format");
            }
        }

        private static Size DecodeBmp(BinaryReader binaryReader)
        {
            binaryReader.ReadBytes(16);
            int width = binaryReader.ReadInt32();
            int height = binaryReader.ReadInt32();
            return new Size(width, height);
        }

        private static Size DecodePng(BinaryReader binaryReader)
        {
            binaryReader.ReadBytes(8);
            int width = BitConverter.ToInt32(binaryReader.ReadBytes(4), 0);
            int height = BitConverter.ToInt32(binaryReader.ReadBytes(4), 0);
            return new Size(width, height);
        }

        public static Rectangle GetTrimmedBackgroundRect(Image image, int transparentColor)
        {
            int minWidth = image.Width;
            int minHeight = image.Height;
            int maxWidth = 0;
            int maxHeight = 0;

            if (image.BitsPerPixel <= 8)
            {
                for (int y = 0; y < image.Height; y++)
                {
                    for (int x = 0; x < image.Width; x++)
                    {
                        int pixelOffset = y * image.Scanline + (x * image.BitsPerPixel / 8);
                        byte pixelIndex = image.PixelData[pixelOffset];
                        //bool isTransparent = (image.Palette.IsTransparent ? pixelIndex == image.Palette.TransparentColor : pixelIndex == transparentColor);
                        bool isTransparent = (pixelIndex == transparentColor);

                        if (!isTransparent)
                        {
                            minWidth = System.Math.Min(minWidth, x);
                            minHeight = System.Math.Min(minHeight, y);
                            maxWidth = System.Math.Max(maxWidth, x + 1);
                            maxHeight = System.Math.Max(maxHeight, y + 1);
                        }
                    }
                }
            }
            else
            {
                for (int y = 0; y < image.Height; y++)
                {
                    for (int x = 0; x < image.Width; x++)
                    {
                        Pixel pixel = image.GetPixel(x, y);

                        if (pixel.A != 0)
                        {
                            minWidth = System.Math.Min(minWidth, x);
                            minHeight = System.Math.Min(minHeight, y);
                            maxWidth = System.Math.Max(maxWidth, x + 1);
                            maxHeight = System.Math.Max(maxHeight, y + 1);
                        }
                    }
                }
            }

            if (minWidth == image.Width && minHeight == image.Height && maxWidth == 0 && maxHeight == 0)
                return Rectangle.Empty;

            return new Rectangle(minWidth, minHeight, maxWidth - minWidth, maxHeight - minHeight);
        }

        public static void FillSpacing(Image pngDst, Image pngSrc, Point imagePoint, Size imageSize, int spacing)
        {
            if (spacing < 2)
                return;

            int halfSpacing = spacing / 2;

            for (int i = 0; i < halfSpacing; i++)
            {
                pngDst.DrawImage(pngSrc, new Rectangle(imagePoint.X + i, imagePoint.Y + halfSpacing, 1, imageSize.Height), new Rectangle(0, 0, 1, pngSrc.Height));
                pngDst.DrawImage(pngSrc, new Rectangle(imagePoint.X + halfSpacing + imageSize.Width + i, imagePoint.Y + halfSpacing, 1, imageSize.Height), new Rectangle(pngSrc.Width - 1, 0, 1, pngSrc.Height));
                pngDst.DrawImage(pngSrc, new Rectangle(imagePoint.X + halfSpacing, imagePoint.Y + i, imageSize.Width, 1), new Rectangle(0, 0, pngSrc.Width, 1));
                pngDst.DrawImage(pngSrc, new Rectangle(imagePoint.X + halfSpacing, imagePoint.Y + halfSpacing + imageSize.Height + i, imageSize.Width, 1), new Rectangle(0, pngSrc.Height - 1, pngSrc.Width, 1));
            }

            for (int i = 0; i < halfSpacing; i++)
            {
                for (int j = 0; j < halfSpacing; j++)
                {
                    pngDst.DrawImage(pngSrc, new Rectangle(imagePoint.X + i, imagePoint.Y + j, 1, 1), new Rectangle(0, 0, 1, 1));
                    pngDst.DrawImage(pngSrc, new Rectangle(imagePoint.X + halfSpacing + imageSize.Width + i, imagePoint.Y + j, 1, 1), new Rectangle(pngSrc.Width - 1, 0, 1, 1));
                    pngDst.DrawImage(pngSrc, new Rectangle(imagePoint.X + i, imagePoint.Y + halfSpacing + imageSize.Height + j, 1, 1), new Rectangle(0, pngSrc.Height - 1, 1, 1));
                    pngDst.DrawImage(pngSrc, new Rectangle(imagePoint.X + halfSpacing + imageSize.Width + i, imagePoint.Y + halfSpacing + imageSize.Height + j, 1, 1), new Rectangle(pngSrc.Width - 1, pngSrc.Height - 1, 1, 1));
                }
            }
        }

        public static Image CreateImageFromColors(Color[] colors)
        {
            byte[] pixelData = new byte[256 * 4];

            for (int i = 0; i < colors.Length; i++)
            {
                pixelData[i * 4] = (byte)colors[i].R;
                pixelData[i * 4 + 1] = (byte)colors[i].G;
                pixelData[i * 4 + 2] = (byte)colors[i].B;
                pixelData[i * 4 + 3] = (byte)colors[i].A;
            }

            return new Image(256, 1, 32, new Palette(), pixelData);
        }

        public static Size ResizeKeepAspect(Size sourceSize, Size destSize)
        {
            Size retSize = new Size();

            float sourceAspect = sourceSize.Width / sourceSize.Height;
            float destAspect = destSize.Width / destSize.Height;

            if (sourceAspect > destAspect)
            {
                retSize.Width = destSize.Width;
                retSize.Height = (int)System.Math.Floor(destSize.Width / sourceAspect);
            }
            else
            {
                retSize.Height = destSize.Height;
                retSize.Width = (int)System.Math.Floor(destSize.Height * sourceAspect);
            }

            return retSize;
        }

        public static Image[] SpriteSheetSplicer(Image srcImage, Size inputSize, Size marginSize, Size spacingSize, Size outputSize, bool autoTrim, bool useTrimSize, int paletteSlot, bool paletteSlotAddIndex, int backgroundIndex)
        {
            List<Image> outputImages = new List<Image>();
            Rectangle trimRect = new Rectangle(0, 0, inputSize.Width, inputSize.Height);
            int cols = 0, rows = 0;

            if (autoTrim)
            {
                trimRect = new Rectangle(inputSize.Width, inputSize.Height, 0, 0);

                cols = (srcImage.Width - marginSize.Width) / (inputSize.Width + spacingSize.Width);
                rows = (srcImage.Height - marginSize.Height) / (inputSize.Height + spacingSize.Height);

                for (int y = 0; y < rows; y++)
                {
                    for (int x = 0; x < cols; x++)
                    {
                        Rectangle srcRect = new Rectangle(marginSize.Width + x * (inputSize.Width + spacingSize.Width), marginSize.Height + y * (inputSize.Height + spacingSize.Height), inputSize.Width, inputSize.Height);
                        Image dstImage = new Image(inputSize.Width, inputSize.Height, srcImage.BitsPerPixel, srcImage.Palette);

                        dstImage.Clear(Color.Empty);
                        dstImage.DrawImage(srcImage, new Rectangle(0, 0, inputSize.Width, inputSize.Height), srcRect);

                        int backIndex = (paletteSlotAddIndex ? backgroundIndex + paletteSlot * 16 : backgroundIndex);
                        Rectangle rect = Utility.GetTrimmedBackgroundRect(dstImage, backIndex);

                        trimRect.X = System.Math.Min(trimRect.X, rect.X);
                        trimRect.Y = System.Math.Min(trimRect.Y, rect.Y);
                        trimRect.Width = System.Math.Max(trimRect.Width, rect.Width);
                        trimRect.Height = System.Math.Max(trimRect.Height, rect.Height);

                        if (useTrimSize)
                            outputSize = new Size(trimRect.Width, trimRect.Height);
                    }
                }
            }

            cols = (srcImage.Width - marginSize.Width) / (inputSize.Width + spacingSize.Width);
            rows = (srcImage.Height - marginSize.Height) / (inputSize.Height + spacingSize.Height);

            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < cols; x++)
                {
                    Rectangle srcRect = new Rectangle(marginSize.Width + x * (inputSize.Width + spacingSize.Width), marginSize.Height + y * (inputSize.Height + spacingSize.Height), inputSize.Width, inputSize.Height);
                    Image dstImage = new Image(outputSize.Width, outputSize.Height, srcImage.BitsPerPixel, srcImage.Palette);

                    dstImage.Clear(Color.Empty);
                    dstImage.DrawImage(srcImage, new Rectangle(0, 0, outputSize.Width, outputSize.Height), srcRect);

                    outputImages.Add(dstImage);
                }
            }

            return outputImages.ToArray();
        }

        public static int NearestPowerOfTwo(double n)
        {
            return (int)System.Math.Pow(2, System.Math.Ceiling(System.Math.Log(n, 2)));
        }

        public static int CalculateTextureSize(int tileWidth, int tileHeight, int tileCount)
        {
            double totalArea = tileWidth * tileHeight * tileCount;
            double textureSide = System.Math.Sqrt(totalArea);
            int textureSize = NearestPowerOfTwo(textureSide);

            while (true)
            {
                int tilesPerRow = textureSize / tileWidth;
                int tilesPerColumn = textureSize / tileHeight;
                if (tilesPerRow * tilesPerColumn >= tileCount)
                {
                    return textureSize;
                }
                textureSize *= 2;
            }
        }
    }
}
