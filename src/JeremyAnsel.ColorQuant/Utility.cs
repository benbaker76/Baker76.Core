using Baker76.Imaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JeremyAnsel.ColorQuant
{
    public class Utility
    {
        public static Image QuantizeImage(Image image, Palette palette, bool remapColors, bool insertMagenta)
        {
            ImageOptions imageOptions = new ImageOptions
            {
                Palette = palette,
                RemapColors = remapColors,
                InsertMagenta = insertMagenta,
            };

            return QuantizeImage(image, imageOptions);
        }

        public static Image QuantizeImage(Image image, ImageOptions imageOptions)
        {
            WuAlphaColorQuantizer quantizer = new WuAlphaColorQuantizer();
            Color[] colors = null;
            Palette quantizedPalette = imageOptions.Palette;
            int colorCountTarget = imageOptions.ColorCount;

            if (quantizedPalette != null)
            {
                colors = quantizedPalette.Colors.ToArray();
                colorCountTarget = quantizedPalette.Colors.Count;

                if (imageOptions.ColorCount == 16)
                {
                    colors = colors.Skip(imageOptions.PaletteSlot * 16).Take(16).ToArray();
                    quantizedPalette = new Palette(colors);
                }
            }

            ColorQuantizerOptions quantizeOptions = new ColorQuantizerOptions
            {
                Palette = colors,
                ColorCountTarget = (imageOptions.InsertMagenta ? colorCountTarget - 1 : colorCountTarget),
                BackgroundIndex = 0,
                PaletteSlot = imageOptions.PaletteSlot,
                AutoPaletteSlot = false,
                PaletteSlotAddIndex = false,
                RemapPalette = imageOptions.RemapColors,
                DistanceType = imageOptions.DistanceType
            };

            ColorQuantizerResult result = quantizer.Quantize(image.PixelData, quantizeOptions);

            if (!imageOptions.RemapColors)
                quantizedPalette = new Palette(result.ColorPalette);

            if (colors == null)
            {
                if (imageOptions.InsertMagenta)
                    quantizedPalette.Colors.Insert(0, Color.Magenta);
            }

            return new Image(image.Width, image.Height, 8, quantizedPalette, result.Bytes);
        }
    }
}
