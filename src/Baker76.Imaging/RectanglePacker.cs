using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace Baker76.Imaging
{
    public class RectanglePacker
    {
        internal class RectNode
        {
            public bool Used = false;
            public RectNode Left = null;
            public RectNode Right = null;
            public Rectangle Rect = Rectangle.Empty;

            public RectNode(Rectangle rect)
            {
                Rect = rect;
            }
        }

        private Size _usedSize;
        private RectNode _root = null;

        public RectanglePacker(int width, int height)
        {
            _root = new RectNode(new Rectangle(0, 0, width, height));
            _usedSize = Size.Empty;
        }

        private bool RecursiveFindPoint(RectNode rectNode, Size size, ref Point point)
        {
            if (rectNode.Left != null)
            {
                RecursiveFindPoint(rectNode.Left, size, ref point);

                return point != Point.Empty ? true : RecursiveFindPoint(rectNode.Right, size, ref point);
            }
            else
            {
                if (rectNode.Used || size.Width > rectNode.Rect.Width || size.Height > rectNode.Rect.Height)
                    return false;

                if (size.Width == rectNode.Rect.Width && size.Height == rectNode.Rect.Height)
                {
                    rectNode.Used = true;
                    point.X = rectNode.Rect.X;
                    point.Y = rectNode.Rect.Y;

                    return true;
                }

                rectNode.Left = new RectNode(rectNode.Rect);
                rectNode.Right = new RectNode(rectNode.Rect);

                if (rectNode.Rect.Width - size.Width > rectNode.Rect.Height - size.Height)
                {
                    rectNode.Left.Rect.Width = size.Width;
                    rectNode.Right.Rect.X = rectNode.Rect.X + size.Width;
                    rectNode.Right.Rect.Width = rectNode.Rect.Width - size.Width;
                }
                else
                {
                    rectNode.Left.Rect.Height = size.Height;
                    rectNode.Right.Rect.Y = rectNode.Rect.Y + size.Height;
                    rectNode.Right.Rect.Height = rectNode.Rect.Height - size.Height;
                }

                return RecursiveFindPoint(rectNode.Left, size, ref point);
            }
        }
        public bool FindPoint(Size size, ref Point point)
        {
            if (RecursiveFindPoint(_root, size, ref point))
            {
                if (_usedSize.Width < point.X + size.Width)
                    _usedSize.Width = point.X + size.Width;
                if (_usedSize.Height < point.Y + size.Height)
                    _usedSize.Height = point.Y + size.Height;

                return true;
            }

            return false;
        }

        public Size UsedSize
        {
            get { return _usedSize; }
        }
    }
}