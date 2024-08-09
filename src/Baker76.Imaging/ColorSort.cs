using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Baker76.Imaging
{
    public enum DistanceType
    {
        Sqrt,
        CIEDE2000
    }

    public enum SortColorMode
    {
        Sqrt,
        HSB,
        Lab
    }

    public enum HSBSortMode
    {
        HSB,
        HBS,
        SHB,
        SBH,
        BHS,
        BSH,
    }

    public class SqrtSorter : IComparer<ColorNode>
    {
        private int _transparentIndex = -1;

        public SqrtSorter()
        {
        }

        public SqrtSorter(int transparentIndex)
        {
            _transparentIndex = transparentIndex;
        }

        #region IComparer Members

        public int Compare(ColorNode cnx, ColorNode cny)
        {
            if (_transparentIndex != -1)
            {
                if (cnx.Index == _transparentIndex || cny.Index == _transparentIndex)
                {
                    if (cnx.Index > cny.Index)
                        return cny.Index.CompareTo(cnx.Index);
                    else
                        return cnx.Index.CompareTo(cny.Index);
                }
            }

            double vx = cnx.Value;
            double vy = cny.Value;

            if (vx.Equals(vy))
            {
                if (cnx.Index > cny.Index)
                    return cny.Index.CompareTo(cnx.Index);
                else
                    return cnx.Index.CompareTo(cny.Index);
            }

            return vx.CompareTo(vy);
        }

        #endregion
    }

    public class HSBSorter : IComparer<ColorNode>
    {
        private int _transparentIndex = -1;
        private HSBSortMode _sortMode = HSBSortMode.HSB;

        public HSBSorter()
        {
        }

        public HSBSorter(HSBSortMode sortMode)
        {
            _sortMode = sortMode;
        }

        public HSBSorter(HSBSortMode sortMode, int transparentIndex)
        {
            _sortMode = sortMode;
            _transparentIndex = transparentIndex;
        }

        public int Compare(ColorNode cnx, ColorNode cny)
        {
            if (_transparentIndex != -1)
            {
                if (cnx.Index == _transparentIndex || cny.Index == _transparentIndex)
                {
                    if (cnx.Index > cny.Index)
                        return cny.Index.CompareTo(cnx.Index);
                    else
                        return cnx.Index.CompareTo(cny.Index);
                }
            }

            Hsb hsbX = Hsb.FromRGB((byte)cnx.Color.R, (byte)cnx.Color.G, (byte)cnx.Color.B);
            Hsb hsbY = Hsb.FromRGB((byte)cny.Color.R, (byte)cny.Color.G, (byte)cny.Color.B);

            int result = 0;

            switch (_sortMode)
            {
                case HSBSortMode.HSB:
                    result = hsbX.Hue.CompareTo(hsbY.Hue);
                    if (result == 0)
                        result = hsbX.Saturation.CompareTo(hsbY.Saturation);
                    if (result == 0)
                        result = hsbX.Brightness.CompareTo(hsbY.Brightness);
                    return result;

                case HSBSortMode.HBS:
                    result = hsbX.Hue.CompareTo(hsbY.Hue);
                    if (result == 0)
                        result = hsbX.Brightness.CompareTo(hsbY.Brightness);
                    if (result == 0)
                        result = hsbX.Saturation.CompareTo(hsbY.Saturation);
                    return result;

                case HSBSortMode.SHB:
                    result = hsbX.Saturation.CompareTo(hsbY.Saturation);
                    if (result == 0)
                        result = hsbX.Hue.CompareTo(hsbY.Hue);
                    if (result == 0)
                        result = hsbX.Brightness.CompareTo(hsbY.Brightness);
                    return result;

                case HSBSortMode.SBH:
                    result = hsbX.Saturation.CompareTo(hsbY.Saturation);
                    if (result == 0)
                        result = hsbX.Brightness.CompareTo(hsbY.Brightness);
                    if (result == 0)
                        result = hsbX.Hue.CompareTo(hsbY.Hue);
                    return result;

                case HSBSortMode.BHS:
                    result = hsbX.Brightness.CompareTo(hsbY.Brightness);
                    if (result == 0)
                        result = hsbX.Hue.CompareTo(hsbY.Hue);
                    if (result == 0)
                        result = hsbX.Saturation.CompareTo(hsbY.Saturation);
                    return result;

                case HSBSortMode.BSH:
                    result = hsbX.Brightness.CompareTo(hsbY.Brightness);
                    if (result == 0)
                        result = hsbX.Saturation.CompareTo(hsbY.Saturation);
                    if (result == 0)
                        result = hsbX.Hue.CompareTo(hsbY.Hue);
                    return result;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    public class LabSorter : IComparer<ColorNode>
    {
        private int _transparentIndex = -1;

        public LabSorter()
        {
        }

        public LabSorter(int transparentIndex)
        {
            _transparentIndex = transparentIndex;
        }

        #region IComparer Members

        public int Compare(ColorNode cnx, ColorNode cny)
        {
            if (_transparentIndex != -1)
            {
                if (cnx.Index == _transparentIndex || cny.Index == _transparentIndex)
                {
                    if (cnx.Index > cny.Index)
                        return cny.Index.CompareTo(cnx.Index);
                    else
                        return cnx.Index.CompareTo(cny.Index);
                }
            }

            CIELab vx = Lab.RGBtoLab(cnx.Color);
            CIELab vy = Lab.RGBtoLab(cny.Color);
            CIELab vz = Lab.RGBtoLab(Color.Empty);

            double vxd = Lab.GetDeltaE_CIEDE2000(vx, vz);
            double vyd = Lab.GetDeltaE_CIEDE2000(vy, vz);

            if (vxd.Equals(vyd))
            {
                if (cnx.Index > cny.Index)
                    return cny.Index.CompareTo(cnx.Index);
                else
                    return cnx.Index.CompareTo(cny.Index);
            }

            return vxd.CompareTo(vyd);
        }

        #endregion
    }

    public class ColorNode
    {
        public int Index;
        public Color Color;

        public ColorNode()
        {
            Index = -1;
            Color = Color.Empty;
        }

        public ColorNode(int index, Color color)
        {
            Index = index;
            Color = color;
        }

        public double Value
        {
            get { return Math.Sqrt(Math.Pow(Color.A, 2) + Math.Pow(Color.R, 2) + Math.Pow(Color.G, 2) + Math.Pow(Color.B, 2)); }
        }
    }

    public class ColorSorter : IComparer<Color>
    {
        #region IComparer Members

        public int Compare(Color cx, Color cy)
        {
            double dx = Math.Pow(cx.A, 2) + Math.Pow(cx.R, 2) + Math.Pow(cx.G, 2) + Math.Pow(cx.B, 2);
            double dy = Math.Pow(cy.A, 2) + Math.Pow(cy.R, 2) + Math.Pow(cy.G, 2) + Math.Pow(cy.B, 2);

            return dx.CompareTo(dy);
        }

        #endregion
    }
}
