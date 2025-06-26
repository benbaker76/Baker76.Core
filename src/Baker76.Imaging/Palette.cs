using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;

namespace Baker76.Imaging
{
    public class Palette : ICloneable
    {
        public bool IsTransparent => TransparentColor >= 0 && TransparentColor < maxColors;
        public int TransparentColor { get; set; }

        private readonly int maxColors;
        public IList<Color> Colors;

        /// <summary>
        /// Create palette without colors
        /// </summary>
        public Palette()
            : this(-1)
        {
        }

        /// <summary>
        /// Create new palette with maximum number of colors allowed to add
        /// </summary>
        public Palette(int maxColors)
        {
            if (maxColors > 256)
            {
                throw new ArgumentOutOfRangeException(nameof(maxColors), "Palette can maximum have 256 colors");
            }
            
            this.maxColors = maxColors;
            Colors = new List<Color>();
        }
        
        /// <summary>
        /// Create new palette with predefined colors
        /// </summary>
        /// <param name="colors"></param>
        public Palette(Color[] colors)
        {
            if (colors.Length > 256)
            {
                throw new ArgumentOutOfRangeException(nameof(maxColors), "Palette can maximum have 256 colors");
            }

            Colors = colors.ToList();
            maxColors = Colors.Count;
        }

        /// <summary>
        /// Create new palette with predefined colors and set transparent color
        /// </summary>
        /// <param name="colors"></param>
        /// <param name="transparentColor"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public Palette(Color[] colors, int transparentColor) : this(colors)
        {
            TransparentColor = transparentColor;
        }
        
        /// <summary>
        /// Add color from rgba
        /// </summary>
        /// <param name="r"></param>
        /// <param name="g"></param>
        /// <param name="b"></param>
        /// <param name="a"></param>
        /// <exception cref="InvalidOperationException"></exception>
        public void AddColor(int r, int g, int b, int a = 255)
        {
            AddColor(new Color(r, g, b, a));
        }

        /// <summary>
        /// Add color
        /// </summary>
        /// <param name="color"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public void AddColor(Color color)
        {
            if (Colors.Count >= maxColors)
            {
                throw new ArgumentOutOfRangeException($"Palette can only have maximum {maxColors} colors");
            }

            Colors.Add(color);
        }

        /// <summary>
        /// Add colors
        /// </summary>
        /// <param name="colors"></param>
        public void AddColors(IList<Color> colors)
        {
            for (int i = 0; i < colors.Count() && i < maxColors; i++)
            {
                if (i < Colors.Count)
                    Colors[i] = colors[i];
                else
                    Colors.Add(colors[i]);
            }
        }

        public static double GetColorDistance(Color color1, Color color2, DistanceType distanceType)
        {
            double minDistance = double.MaxValue;
            double distance = 0;

            if (distanceType == DistanceType.Sqrt)
            {
                distance = Math.Sqrt(
                    Math.Pow(color1.R - color2.R, 2) +
                    Math.Pow(color1.G - color2.G, 2) +
                    Math.Pow(color1.B - color2.B, 2)
                );
            }
            else if (distanceType == DistanceType.CIEDE2000)
            {
                CIELab labColor = Lab.RGBtoLab(color1.R, color1.G, color1.B);
                CIELab paletteLabColor = Lab.RGBtoLab(color2.R, color2.G, color2.B);

                distance = Lab.GetDeltaE_CIEDE2000(labColor, paletteLabColor);
            }

            if (distance == 0)
                return 0;

            if (distance < minDistance)
                minDistance = distance;

            return minDistance;
        }

        public static double GetNearestColor(Color color, Color[] palette, DistanceType distanceType, out Color nearestColor)
        {
            double minDistance = double.MaxValue;
            nearestColor = null;

            foreach (var paletteColor in palette)
            {
                double distance = GetColorDistance(color, paletteColor, distanceType);

                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestColor = paletteColor;
                }
            }

            return minDistance;
        }


        public double GetNearestColor(Color color, DistanceType distanceType, out Color nearestColor)
        {
            return GetNearestColor(color, Colors.ToArray(), distanceType, out nearestColor);
        }

        public double GetPaletteDistance(Color[] palette1, Color[] palette2, DistanceType distanceType)
        {
            double totalDistance = 0;

            foreach (var color in palette1)
            {
                Color nearestColor;
                double distance = GetNearestColor(color, palette2, distanceType, out nearestColor);
                totalDistance += distance;
            }

            return totalDistance;
        }

        public double GetPaletteDistance(Color[] paletteColors, DistanceType distanceType)
        {
            double totalDistance = 0;

            foreach (var color in paletteColors)
            {
                Color nearestColor = null;
                totalDistance += GetNearestColor(color, distanceType, out nearestColor);
            }

            return totalDistance;
        }

        public void SetNearestColorPalette(Color[] nearestColorPalette, DistanceType distanceType)
        {
            if (nearestColorPalette == null)
                return;

            for (int i = 0; i < Colors.Count; i++)
            {
                Color nearestColor = null;

                GetNearestColor(Colors[i], nearestColorPalette, distanceType, out nearestColor);

                Colors[i] = nearestColor;
            }
        }

        public int GetBestPaletteSlot(Color[] paletteColors, DistanceType distanceType)
        {
            if (paletteColors.Length != 256)
                return 0;

            Palette palette = new Palette(paletteColors);
            double minTotalDistance = double.MaxValue;
            int paletteSlot = 0;

            for (int i = 0; i < 16; i++)
            {
                Color[] colorPalette = palette.GetColors(i);
                double totalDistance = 0;

                foreach (Color color1 in Colors)
                {
                    double minDistance = double.MaxValue;

                    foreach (Color color2 in colorPalette)
                    {
                        double distance = GetColorDistance(color1, color2, distanceType);

                        if (distance == 0)
                        {
                            minDistance = 0;
                            break;
                        }

                        if (distance < minDistance)
                        {
                            minDistance = distance;
                        }
                    }

                    totalDistance += minDistance;
                }

                if (totalDistance < minTotalDistance)
                {
                    minTotalDistance = totalDistance;
                    paletteSlot = i;
                }
            }

            return paletteSlot;
        }

        public Dictionary<int, int> GetPaletteRemap(Color[] paletteColors, int backgroundIndex, DistanceType distanceType)
        {
            Dictionary<int, int> indexRemap = new Dictionary<int, int>();

            for (int i = 0; i < Colors.Count; i++)
            {
                Color color = Colors[i];

                if (color.A == 0)
                {
                    indexRemap[i] = backgroundIndex;
                    continue;
                }

                double minDistance = double.MaxValue;
                int nearestColorIndex = 0;

                for (int j = 0; j < paletteColors.Length; j++)
                {
                    Color paletteColor = paletteColors[j];
                    double distance = GetColorDistance(color, paletteColor, distanceType);

                    if (distance == 0)
                    {
                        minDistance = 0;
                        nearestColorIndex = j;
                        break;
                    }

                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        nearestColorIndex = j;
                    }
                }

                indexRemap[i] = nearestColorIndex;
            }

            return indexRemap;
        }

        public void SetColors(Color[] colors, int paletteSlot, int colorCount)
        {
            for (int i = 0; i < Math.Min(colors.Length, colorCount); i++)
                Colors[paletteSlot * 16 + i] = colors[i];
        }

        public Color[] GetColors(int paletteSlot)
        {
            return Colors.Skip(paletteSlot * 16).Take(16).ToArray();
        }

        public Color[] GetColors()
        {
            return Colors.ToArray();
        }

        public byte[] Bytes
        {
            get
            {
                byte[] bytes = new byte[Colors.Count * 4];

                for (int i = 0; i < Colors.Count; i++)
                {
                    Color color = Colors[i];
                    bytes[i * 4] = (byte)color.R;
                    bytes[i * 4 + 1] = (byte)color.G;
                    bytes[i * 4 + 2] = (byte)color.B;
                    bytes[i * 4 + 3] = (byte)color.A;
                }

                return bytes;
            }
        }

        public int GetColorIndex(Color color)
        {
            return Colors.IndexOf(color);
        }

        public object Clone()
        {
            return new Palette(Colors.ToArray(), TransparentColor);
        }
    }
}