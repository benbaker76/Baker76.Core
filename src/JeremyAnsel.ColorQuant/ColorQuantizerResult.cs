// <copyright file="ColorQuantizerResult.cs" company="Jérémy Ansel">
// Copyright (c) 2014-2019 Jérémy Ansel
// </copyright>
// <license>
// Licensed under the MIT license. See LICENSE.txt
// </license>

using Baker76.Imaging;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace JeremyAnsel.ColorQuant
{
    /// <summary>
    /// A result of color quantization.
    /// </summary>
    public sealed class ColorQuantizerResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ColorQuantizerResult"/> class.
        /// </summary>
        /// <param name="size">The size of the result.</param>
        /// <param name="colorCount">The color count.</param>
        public ColorQuantizerResult(int size, int colorCount)
        {
            if (size < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(size));
            }

            if (colorCount < 1 || colorCount > 256)
            {
                throw new ArgumentOutOfRangeException(nameof(colorCount));
            }

            this.Palette = new byte[colorCount * 4];
            this.Bytes = new byte[size];
        }

        public void RemapColors(ColorQuantizerOptions options)
        {
            if (options.ColorCountTarget == 16)
            {
                Palette thisPalette = new Palette(ColorPalette);
                Palette thatPalette = new Palette(options.Palette);
                if (options.AutoPaletteSlot)
                    options.PaletteSlot = thisPalette.GetBestPaletteSlot(thatPalette.GetColors(), options.DistanceType);
                Color[] paletteColors = thatPalette.GetColors(options.PaletteSlot);
                Dictionary<int, int> indexRemap = thisPalette.GetPaletteRemap(paletteColors, options.BackgroundIndex, options.DistanceType);

                if (options.PaletteSlotAddIndex)
                {
                    for (int i = 0; i < Bytes.Length; i++)
                        Bytes[i] = (byte)(indexRemap[Bytes[i]] + options.PaletteSlot * 16);
                }
                else
                {
                    for (int i = 0; i < Bytes.Length; i++)
                        Bytes[i] = (byte)indexRemap[Bytes[i]];
                }
            }
            else
            {
                Palette palette = new Palette(ColorPalette);
                Dictionary<int, int> indexRemap = palette.GetPaletteRemap(options.Palette, options.BackgroundIndex, options.DistanceType);

                for (int i = 0; i < Bytes.Length; i++)
                    Bytes[i] = (byte)indexRemap[Bytes[i]];
            }
        }

        /// <summary>
        /// Gets the palette (XRGB or ARGB).
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Justification = "Reviewed")]
        public byte[] Palette { get; private set; }

        /// <summary>
        /// Gets the bytes.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Justification = "Reviewed")]
        public byte[] Bytes { get; private set; }


        public Color[] ColorPalette
        {
            get
            {
                Color[] colors = new Color[this.Palette.Length / 4];

                for (int i = 0; i < colors.Length; i++)
                    colors[i] = Color.FromRgba(this.Palette[i * 4], this.Palette[i * 4 + 1], this.Palette[i * 4 + 2], this.Palette[i * 4 + 3]);

                return colors;
            }
        }
    }
}
