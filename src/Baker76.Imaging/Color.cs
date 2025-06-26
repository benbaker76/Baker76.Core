using System;
using System.Drawing;

namespace Baker76.Imaging
{
    public class Color
    {
        public int R;
        public int G;
        public int B;
        public int A;

        public Color(int r, int g, int b, int a = 255)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        public static Color FromColor(Color color, int a)
        {
            return new Color(color.R, color.G, color.B, a);
        }

        public static Color FromArgb(int argb)
        {
            return new Color(argb >> 16 & 0xFF, argb >> 8 & 0xFF, argb & 0xFF, argb >> 24 & 0xFF);
        }

        public static Color FromArgb(int argb, int alpha)
        {
            return new Color(argb >> 16 & 0xFF, argb >> 8 & 0xFF, argb & 0xFF, alpha);
        }

        public static Color FromArgb(int a, int r, int g, int b)
        {
            return new Color(r, g, b, a);
        }

        public static Color FromArgb(int r, int g, int b)
        {
            return new Color(r, g, b, 255);
        }

        public static Color FromRgba(int rgba)
        {
            return new Color(rgba >> 24 & 0xFF, rgba >> 16 & 0xFF, rgba >> 8 & 0xFF, rgba & 0xFF);
        }

        public static Color FromRgba(int rgba, int alpha)
        {
            return new Color(rgba >> 24 & 0xFF, rgba >> 16 & 0xFF, rgba >> 8 & 0xFF, alpha);
        }

        public static Color FromRgba(int r, int g, int b, int a)
        {
            return new Color(r, g, b, a);
        }

        public static Color FromRgba(int r, int g, int b)
        {
            return new Color(r, g, b, 255);
        }

        public static Color FromRgbaNonPremultiplied(int r, int g, int b, int a)
        {
            return new Color(
                r * a / 255,
                g * a / 255,
                b * a / 255,
                a
            );
        }

        public int ToArgb()
        {
            return A << 24 | R << 16 | G << 8 | B;
        }

        public int ToRgba()
        {
            return R << 24 | G << 16 | B << 8 | A;
        }

        public bool IsEmpty()
        {
            return R == 0 && G == 0 && B == 0;
        }

        /// <summary>
        ///       Returns the Hue-Saturation-Lightness (HSL) lightness
        ///       for this <see cref='System.Drawing.Color'/> .
        /// </summary>
        public float GetBrightness()
        {
            float r = R / 255.0f;
            float g = G / 255.0f;
            float b = B / 255.0f;

            float max, min;

            max = r; min = r;

            if (g > max) max = g;
            if (b > max) max = b;

            if (g < min) min = g;
            if (b < min) min = b;

            return (max + min) / 2;
        }

        /// <summary>
        ///       Returns the Hue-Saturation-Lightness (HSL) hue
        ///       value, in degrees, for this <see cref='System.Drawing.Color'/> .  
        ///       If R == G == B, the hue is meaningless, and the return value is 0.
        /// </summary>
        public float GetHue()
        {
            if (R == G && G == B)
                return 0; // 0 makes as good an UNDEFINED value as any

            float r = R / 255.0f;
            float g = G / 255.0f;
            float b = B / 255.0f;

            float max, min;
            float delta;
            float hue = 0.0f;

            max = r; min = r;

            if (g > max) max = g;
            if (b > max) max = b;

            if (g < min) min = g;
            if (b < min) min = b;

            delta = max - min;

            if (r == max)
            {
                hue = (g - b) / delta;
            }
            else if (g == max)
            {
                hue = 2 + (b - r) / delta;
            }
            else if (b == max)
            {
                hue = 4 + (r - g) / delta;
            }
            hue *= 60;

            if (hue < 0.0f)
            {
                hue += 360.0f;
            }
            return hue;
        }

        /// <summary>
        ///   The Hue-Saturation-Lightness (HSL) saturation for this
        ///    <see cref='System.Drawing.Color'/>
        /// </summary>
        public float GetSaturation()
        {
            float r = R / 255.0f;
            float g = G / 255.0f;
            float b = B / 255.0f;

            float max, min;
            float l, s = 0;

            max = r; min = r;

            if (g > max) max = g;
            if (b > max) max = b;

            if (g < min) min = g;
            if (b < min) min = b;

            // if max == min, then there is no color and
            // the saturation is zero.
            //
            if (max != min)
            {
                l = (max + min) / 2;

                if (l <= .5)
                {
                    s = (max - min) / (max + min);
                }
                else
                {
                    s = (max - min) / (2 - max - min);
                }
            }
            return s;
        }

        public static Color Empty => new Color(0, 0, 0, 0);
        public static Color White => new Color(255, 255, 255);
        public static Color Black => new Color(0, 0, 0);
        public static Color Red => new Color(255, 0, 0);
        public static Color Green => new Color(0, 255, 0);
        public static Color Blue => new Color(0, 0, 255);
        public static Color Yellow => new Color(255, 255, 0);
        public static Color Cyan => new Color(0, 255, 255);
        public static Color Magenta => new Color(255, 0, 255);
        public static Color Gray => new Color(128, 128, 128);
        public static Color LightGray => new Color(211, 211, 211);
        public static Color DarkGray => new Color(169, 169, 169);
        public static Color Orange => new Color(255, 165, 0);
        public static Color Pink => new Color(255, 192, 203);
        public static Color Purple => new Color(128, 0, 128);
        public static Color Brown => new Color(165, 42, 42);
        public static Color Lime => new Color(0, 255, 0);
        public static Color Olive => new Color(128, 128, 0);
        public static Color Navy => new Color(0, 0, 128);
        public static Color Teal => new Color(0, 128, 128);
        public static Color Maroon => new Color(128, 0, 0);
        public static Color Silver => new Color(192, 192, 192);
        public static Color Gold => new Color(255, 215, 0);
        public static Color Transparent => new Color(0, 0, 0, 0);

        public bool Equals(Color other)
        {
            return ToArgb() == other.ToArgb();
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;

            var other = (Color)obj;

            return Equals(other);
        }

        public override int GetHashCode()
        {
            return ToArgb();
        }

        public override string ToString()
        {
            return $"Color [A={A}, R={R}, G={G}, B={B}]";
        }
    }
}