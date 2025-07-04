﻿using System.IO;
using System.Linq;
using Baker76.Pngcs;
using Baker76.Pngcs.Chunks;

namespace Baker76.Imaging.Pngcs
{
    public static class PngWriter
    {
        private const int BitDepth = 8;
        private const bool IsGrayscale = false;

        public static void Write(string fileName, Image image)
        {
            using (var stream = File.OpenWrite(fileName))
                Write(stream, image, true);
        }

        public static void Write(Stream stream, Image image, bool shouldCloseStream)
        {
            var hasAlpha = image.BitsPerPixel == 32;
            var hasPalette = image.BitsPerPixel <= 8;

            var imageInfo = new ImageInfo(image.Width, image.Height, BitDepth, hasAlpha, IsGrayscale, hasPalette);
            var pngWriter = new Baker76.Pngcs.PngWriter(stream, imageInfo);

            pngWriter.ShouldCloseStream = shouldCloseStream;

            if (hasPalette)
            {
                CreatePaletteChunk(pngWriter, image.Palette);
            }
            
            if (image.BitsPerPixel < 32 && image.IsTransparent)
            {
                CreateTransparencyChunk(pngWriter, image);
            }

            pngWriter.GetMetadata().SetDpi(72.0);

            var pixelDataOffset = 0;
            for (var y = 0; y < image.Height; y++)
            {
                var imageLine = new ImageLine(imageInfo);

                var scanlineOffset = 0;
                for (var x = 0; x < image.Width; x++)
                {
                    switch (image.BitsPerPixel)
                    {
                        case 1:
                        case 4:
                        case 8:
                            imageLine.Scanline[scanlineOffset] = image.PixelData[pixelDataOffset];
                            break;
                        case 24:
                        case 32:
                            imageLine.Scanline[scanlineOffset] = image.PixelData[pixelDataOffset];
                            imageLine.Scanline[scanlineOffset + 1] = image.PixelData[pixelDataOffset + 1];
                            imageLine.Scanline[scanlineOffset + 2] = image.PixelData[pixelDataOffset + 2];
                            if (image.BitsPerPixel == 32)
                            {
                                imageLine.Scanline[scanlineOffset + 3] = image.PixelData[pixelDataOffset + 3];
                            }
                            break;
                    }

                    pixelDataOffset += image.BytesPerPixel;
                    scanlineOffset += imageInfo.Channels;
                }
                
                pngWriter.WriteRow(imageLine, y);
            }

            pngWriter.End();
        }

        private static void CreateTransparencyChunk(Baker76.Pngcs.PngWriter pngWriter, Image image)
        {
            var transparencyChunk = new PngChunkTRNS(pngWriter.ImgInfo);

            switch (image.BitsPerPixel)
            {
                case 8:
                    int[] paletteAlpha = new int[image.Palette.TransparentColor + 1];
                    for (int i = 0; i < image.Palette.TransparentColor; i++)
                        paletteAlpha[i] = 255;
                    paletteAlpha[image.Palette.TransparentColor] = 0;
                    transparencyChunk.SetPalletteAlpha(paletteAlpha);
                    //transparencyChunk.setIndexEntryAsTransparent(image.Palette.TransparentColor);
                    transparencyChunk.SetPalletteAlpha(image.Palette.Colors.Select(x => x.A).ToArray());
                    break;
                case 24:
                    transparencyChunk.SetRGB(image.TransparentColor.R, image.TransparentColor.G,
                        image.TransparentColor.B);
                    break;
            }

            pngWriter.GetChunksList().Queue(transparencyChunk);
        }

        private static void CreatePaletteChunk(Baker76.Pngcs.PngWriter pngWriter, Palette palette)
        {
            var paletteChunk = pngWriter.GetMetadata().CreatePLTEChunk();

            paletteChunk.SetNentries(palette.Colors.Count);
            for (var i = 0; i < palette.Colors.Count; i++)
            {
                var color = palette.Colors[i];
                paletteChunk.SetEntry(i, color.R, color.G, color.B);
            }
        }
    }
}