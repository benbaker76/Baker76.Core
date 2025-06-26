// <copyright file="IColorQuantizer.cs" company="Jérémy Ansel">
// Copyright (c) 2014-2019 Jérémy Ansel
// </copyright>
// <license>
// Licensed under the MIT license. See LICENSE.txt
// </license>

using Baker76.Imaging;
using System.Threading.Tasks;

namespace JeremyAnsel.ColorQuant
{
    public class ColorQuantizerOptions
    {
        public Color[] Palette;
        public int ColorCountTarget;
        public int ColorCountResult;
        public int BackgroundIndex;
        public int PaletteSlot;
        public bool AutoPaletteSlot;
        public bool PaletteSlotAddIndex;
        public bool RemapPalette;
        public DistanceType DistanceType;
    }

    /// <summary>
    /// Defines a color quantizer.
    /// </summary>
    public interface IColorQuantizer
    {
        Task<ColorQuantizerResult> QuantizeAsync(byte[] image, ColorQuantizerOptions options);

        /// <summary>
        /// Quantizes an image.
        /// </summary>
        /// <param name="image">The image (XRGB or ARGB).</param>
        /// <param name="colorCount">The color count.</param>
        /// <returns>The result.</returns>
        ColorQuantizerResult Quantize(byte[] image, ColorQuantizerOptions options);
    }
}
