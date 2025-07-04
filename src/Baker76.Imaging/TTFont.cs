﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace Baker76.Imaging
{
    public class GlyphBitmap
    {
        public readonly int Width;
        public readonly int Height;
        public readonly bool IsSvg;
        public readonly byte[] Pixels; // Stores RGBA data

        public GlyphBitmap(int width, int height, bool isSvg, Color backgroundColor)
        {
            Width = width;
            Height = height;
            IsSvg = isSvg;
            Pixels = new byte[width * height * 4]; // 4 bytes per pixel (RGBA)
            for (int i = 0; i < Pixels.Length; i += 4)
            {
                Pixels[i] = (byte)backgroundColor.R;
                Pixels[i + 1] = (byte)backgroundColor.G;
                Pixels[i + 2] = (byte)backgroundColor.B;
                Pixels[i + 3] = (byte)backgroundColor.A;
            }
        }

        public GlyphBitmap(int width, int height, bool isSvg)
        {
            Width = width;
            Height = height;
            Pixels = new byte[width * height * 4];
            IsSvg = isSvg;
        }


        public GlyphBitmap(int width, int height, byte[] pixels, bool isSvg)
        {
            Width = width;
            Height = height;
            Pixels = pixels;
            IsSvg = isSvg;
        }

        public void DrawRectangle(int x, int y, int width, int height, Color color)
        {
            for (int j = y; j < y + height; j++)
            {
                for (int i = x; i < x + width; i++)
                {
                    if (i < 0 || j < 0 || i >= Width || j >= Height)
                        continue;

                    int ofs = (i + j * Width) * 4;
                    Pixels[ofs] = (byte)color.R;
                    Pixels[ofs + 1] = (byte)color.G;
                    Pixels[ofs + 2] = (byte)color.B;
                    Pixels[ofs + 3] = (byte)color.A;
                }
            }
        }

        // Draw another GlyphBitmap onto this one
        public void Draw(GlyphBitmap other, int x, int y, Color backgroundColor)
        {
            for (int j = 0; j < other.Height; j++)
            {
                for (int i = 0; i < other.Width; i++)
                {
                    int destX = x + i;
                    int destY = y + j;

                    if (destX < 0 || destY < 0 || destX >= Width || destY >= Height || i >= other.Width || j >= other.Height)
                        continue;

                    int srcOfs = (i + j * other.Width) * 4;
                    int destOfs = (destX + destY * Width) * 4;

                    // Extract source and destination colors
                    byte srcRed = other.Pixels[srcOfs];
                    byte srcGreen = other.Pixels[srcOfs + 1];
                    byte srcBlue = other.Pixels[srcOfs + 2];
                    byte srcAlpha = other.Pixels[srcOfs + 3];

                    byte destRed = Pixels[destOfs];
                    byte destGreen = Pixels[destOfs + 1];
                    byte destBlue = Pixels[destOfs + 2];
                    byte destAlpha = Pixels[destOfs + 3];

                    // Normalize the alpha values (0-255 to 0-1)
                    float srcAlphaNorm = srcAlpha / 255f;
                    float destAlphaNorm = destAlpha / 255f;

                    // Blend the source and destination colors based on their alpha values
                    float outAlphaNorm = srcAlphaNorm + destAlphaNorm * (1 - srcAlphaNorm);
                    if (outAlphaNorm > 0)
                    {
                        Pixels[destOfs] = (byte)((srcRed * srcAlphaNorm + destRed * destAlphaNorm * (1 - srcAlphaNorm)) / outAlphaNorm);
                        Pixels[destOfs + 1] = (byte)((srcGreen * srcAlphaNorm + destGreen * destAlphaNorm * (1 - srcAlphaNorm)) / outAlphaNorm);
                        Pixels[destOfs + 2] = (byte)((srcBlue * srcAlphaNorm + destBlue * destAlphaNorm * (1 - srcAlphaNorm)) / outAlphaNorm);
                    }

                    // Update the destination alpha value
                    Pixels[destOfs + 3] = (byte)(outAlphaNorm * 255);
                }
            }
        }

        public void DrawFontGuides(int x, int y, FontMetrics metrics, float scale)
        {
            Color topColor = Color.Purple;
            Color ascentColor = Color.Lime;
            Color baseLineColor = Color.Red;
            Color descentColor = Color.Blue;
            Color bottomColor = Color.Orange;

            int width = (int)Math.Round(metrics.AdvanceWidthMax * scale);

            int ascent = (int)Math.Round(metrics.Ascent * scale);
            int descent = (int)Math.Round(metrics.Descent * scale);
            int lineGap = (int)Math.Round(metrics.LineGap * scale);

            int baselineY = y;
            int ascentY = baselineY - ascent;
            int descentY = baselineY - descent; // descent is usually negative
            int topY = ascentY; // in simple fonts top == ascent (unless font bbox says otherwise)
            int bottomY = descentY + lineGap; // bottom after descent + gap

            DrawHorizontalLine(x, topY, width, topColor);         // Top
            DrawHorizontalLine(x, ascentY, width, ascentColor);    // Ascent
            DrawHorizontalLine(x, baselineY, width, baseLineColor); // Baseline
            DrawHorizontalLine(x, descentY, width, descentColor);  // Descent
            DrawHorizontalLine(x, bottomY, width, bottomColor);    // Bottom
        }

        private void DrawHorizontalLine(int x, int y, int width, Color color)
        {
            if (y < 0 || y >= Height)
                return;

            for (int x1 = 0; x1 < width; x1++)
            {
                int xOffset = x + x1;
                int yOffset = y;
                int ofs = (xOffset + yOffset * Width) * 4;

                if (xOffset < 0 || xOffset >= Width)
                    continue;

                if (yOffset < 0 || yOffset >= Height)
                    continue;

                Pixels[ofs + 0] = (byte)color.R;
                Pixels[ofs + 1] = (byte)color.G;
                Pixels[ofs + 2] = (byte)color.B;
                Pixels[ofs + 3] = 255;
            }
        }
    }

    public class FontMetrics
    {
        public int CapHeight;
        public int Ascent;
        public int Descent;
        public int Height;
        public int LineHeight;
        public int LineGap;
        public int BaseLine;
        public int AdvanceWidthMax;
        public int MinLeftSideBearing;
        public int MinRightSideBearing;
        public int xMaxExtent;

        public FontMetrics(TTFont font)
        {
            font.GetFontVMetrics(out Ascent, out Descent, out LineGap);
            CapHeight = font.GetCapHeight();
            Height = Ascent - Descent;
            LineHeight = Height + LineGap;
            BaseLine = Ascent;
            font.GetFontHMetrics(out AdvanceWidthMax, out MinLeftSideBearing, out MinRightSideBearing, out xMaxExtent);
        }
    }

    public class GlyphMetrics
    {
        public RectangleF Bounds { get; set; }
        public int LeftSideBearing { get; set; }
        public int TopSideBearing { get; set; }
        public int AdvanceWidth { get; set; }
        public int AdvanceHeight { get; set; }
    }

    internal struct Edge
    {
        public float x0;
        public float y0;
        public float x1;
        public float y1;
        public bool invert;
    }

    internal struct Vertex
    {
        public short x;
        public short y;
        public short cx;
        public short cy;
        public short cx1;
        public short cy1;
        public byte vertexType;

        public Vertex(byte vertexType, short x, short y, short cx, short cy, short cx1, short cy1)
        {
            this.vertexType = vertexType;
            this.x = x;
            this.y = y;
            this.cx = cx;
            this.cy = cy;
            this.cx1 = cx1;
            this.cy1 = cy1;
        }

        public Vertex(byte vertexType, short x, short y, short cx, short cy)
        {
            this.vertexType = vertexType;
            this.x = x;
            this.y = y;
            this.cx = cx;
            this.cy = cy;
            cx1 = 0;
            cy1 = 0;
        }
    }

    internal class ActiveEdge
    {
        public int x;
        public int dx;
        public float ey;
        public ActiveEdge next;
        public int direction;
    }

    public class TTFont
    {
        private int _glyphCount;
        private byte[] _data;              // pointer to .ttf file

        private uint _loca;
        private uint _head;
        private uint _glyf;
        private uint _hhea;
        private uint _vhea;
        private uint _hmtx;
        private uint _vmtx;
        private uint _kern; // table locations as offset from start of .ttf
        private uint _name;
        private uint _gpos;
        private uint _os2;
        private uint _svg;

        private uint _indexMap;                // a cmap mapping for our chosen character encoding
        private int _indexToLocFormat;         // format needed to map from glyph index to glyph
        private uint _unitsPerEm;

        private string _fontName;
        private string _fontStyle;

        private object _userData;

        private Dictionary<int, string> _svgDocuments = new Dictionary<int, string>();

        private const byte PLATFORM_ID_UNICODE = 0;
        private const byte PLATFORM_ID_MAC = 1;
        private const byte PLATFORM_ID_ISO = 2;
        private const byte PLATFORM_ID_MICROSOFT = 3;

        private const byte MS_EID_SYMBOL = 0;
        private const byte MS_EID_UNICODE_BMP = 1;
        private const byte MS_EID_SHIFTJIS = 2;
        private const byte MS_EID_UNICODE_FULL = 10;

        private const int FIXSHIFT = 10;
        private const int FIX = 1 << FIXSHIFT;
        private const int FIXMASK = FIX - 1;

        private const byte VMOVE = 1;
        private const byte VLINE = 2;
        private const byte VCURVE = 3;
        private const byte VCUBIC = 4;

        public delegate Task<GlyphBitmap> SvgRenderCallback(TTFont font, string svgDoc, char codePoint, int glyph, object userData);

        public SvgRenderCallback SvgRender;

        public TTFont(byte[] bytes, object userData)
        {
            _data = bytes;
            _userData = userData;
            _svgDocuments = new Dictionary<int, string>();

            if (!IsFont())
            {
                throw new Exception("Invalid font file");
            }

            var cmap = FindTable("cmap");
            _loca = FindTable("loca");
            _head = FindTable("head");
            _glyf = FindTable("glyf");
            _hhea = FindTable("hhea");
            _vhea = FindTable("vhea");
            _hmtx = FindTable("hmtx");
            _vmtx = FindTable("vmtx");
            _kern = FindTable("kern");
            _name = FindTable("name");
            _gpos = FindTable("GPOS");
            _os2 = FindTable("OS/2");
            _svg = FindTable("SVG ");

            /* if (cmap == 0 || _loca == 0 || _head == 0 || _glyf == 0 || _hhea == 0 || _hmtx == 0)
            {
                throw new System.Exception("'Invalid font file");
            } */

            ParseNameTable();
            ParseSvgTable();

            var t = FindTable("maxp");

            if (t != 0)
                _glyphCount = ReadU16(t + 4);
            else
                _glyphCount = -1;

            // find a cmap encoding table we understand *now* to avoid searching later. (todo: could make this installable)
            var numTables = (int)ReadU16(cmap + 2);
            _indexMap = 0;

            for (int i = 0; i < numTables; i++)
            {
                uint encodingRecord = (uint)(cmap + 4 + 8 * i);

                // find an encoding we understand:
                switch (ReadU16(encodingRecord))
                {
                    case PLATFORM_ID_MICROSOFT:
                        switch (ReadU16(encodingRecord + 2))
                        {
                            case MS_EID_UNICODE_BMP:
                            case MS_EID_UNICODE_FULL:
                                // MS/Unicode
                                _indexMap = cmap + ReadU32(encodingRecord + 4);
                                break;
                        }
                        break;
                }
            }

            if (_indexMap == 0)
            {
                throw new Exception("Could not find font index map");
            }

            _unitsPerEm = ReadU16(_head + 18);
            _indexToLocFormat = ReadU16(_head + 50);
        }

        private bool TagEqual(byte[] tag, string s)
        {
            return tag[0] == s[0] && tag[1] == s[1] && tag[2] == s[2] && tag[3] == s[3];
        }

        private bool IsFont()
        {
            if (_data.Length < 4)
                return false;

            // check the version number
            if (TagEqual(_data, "1\x00\x00\x00")) return true; // TrueType 1
            if (TagEqual(_data, "typ1")) return true; // TrueType with type 1 font -- we don't support this!
            if (TagEqual(_data, "OTTO")) return true; // OpenType with CFF
            if (TagEqual(_data, "\x00\x01\x00\x00")) return true; // OpenType 1.0
            if (TagEqual(_data, "true")) return true; // Apple specification for TrueType fonts

            return false;
        }

        private int GetFontOffset(uint index)
        {
            // if it's just a font, there's only one valid index
            if (IsFont())
                return index == 0 ? 0 : -1;

            // check if it's a TTC
            if (TagEqual(_data, "ttcf"))
            {
                // version 1?
                if (ReadU32(4) == 0x00010000 || ReadU32(4) == 0x00020000)
                {
                    int n = ReadS32(8);
                    if (index >= n)
                        return -1;
                    return ReadS32(12 + index * 4);
                }
            }
            return -1;
        }

        private byte Read8(uint offset)
        {
            return offset >= _data.Length ? (byte)0 : _data[offset];
        }

        private ushort ReadU16(uint offset)
        {
            return offset >= _data.Length ? (ushort)0 : (ushort)((_data[offset] << 8) + _data[offset + 1]);
        }

        private short ReadS16(uint offset)
        {
            return offset >= _data.Length ? (short)0 : (short)((_data[offset] << 8) + _data[offset + 1]);
        }

        private uint ReadU32(uint offset)
        {
            if (offset >= _data.Length)
                return 0;
            else
                return (uint)((_data[offset] << 24) + (_data[offset + 1] << 16) + (_data[offset + 2] << 8) + _data[offset + 3]);
        }

        private int ReadS32(uint offset)
        {
            if (offset >= _data.Length)
                return 0;
            else
                return (_data[offset] << 24) + (_data[offset + 1] << 16) + (_data[offset + 2] << 8) + _data[offset + 3];
        }

        private bool HasTag(uint offset, string tag)
        {
            if (offset >= _data.Length)
                return false;
            else
            {
                var bytes = Encoding.ASCII.GetBytes(tag);
                return _data[offset + 0] == bytes[0] && _data[offset + 1] == bytes[1] && _data[offset + 2] == bytes[2] && _data[offset + 3] == bytes[3];
            }
        }

        private void ParseNameTable()
        {
            if (_name == 0)
                return;

            uint count = ReadU16(_name + 2);
            uint stringOffset = _name + ReadU16(_name + 4);
            // Get font name and style
            for (uint i = 0; i < count; i++)
            {
                uint loc = _name + 6 + 12 * i;
                uint platformID = ReadU16(loc + 0);
                uint encodingID = ReadU16(loc + 2);
                uint languageID = ReadU16(loc + 4);
                uint nameID = ReadU16(loc + 6);
                uint length = ReadU16(loc + 8);
                uint offset = ReadU16(loc + 10);

                // Full font name
                if (nameID == 1)
                    _fontName = Encoding.BigEndianUnicode.GetString(_data, (int)(stringOffset + offset), (int)length);
                // Font style
                if (nameID == 2)
                    _fontStyle = Encoding.BigEndianUnicode.GetString(_data, (int)(stringOffset + offset), (int)length);
            }
        }

        private void ParseSvgTable()
        {
            if (_svg == 0)
                return;

            uint version = ReadU16(_svg);
            uint indexOffset = ReadU32(_svg + 2);

            ParseSvgDocumentIndex(indexOffset);
        }

        private void ParseSvgDocumentIndex(uint indexOffset)
        {
            uint svgDocIndexOffset = _svg + indexOffset;
            ushort numEntries = ReadU16(svgDocIndexOffset);
            uint entryOffset = svgDocIndexOffset + 2;

            for (int i = 0; i < numEntries; i++)
            {
                uint startGlyphID = ReadU16(entryOffset);
                uint endGlyphID = ReadU16(entryOffset + 2);
                uint svgDocOffset = ReadU32(entryOffset + 4);
                uint svgDocLength = ReadU32(entryOffset + 8);
                entryOffset += 12;

                string svgDoc = ReadSvgDocument(svgDocOffset, svgDocLength);

                if (!string.IsNullOrEmpty(svgDoc))
                {
                    for (int glyphID = (int)startGlyphID; glyphID <= endGlyphID; glyphID++)
                        _svgDocuments[glyphID] = svgDoc;
                }
            }
        }

        private string ReadSvgDocument(uint svgDocOffset, uint svgDocLength)
        {
            uint absoluteOffset = _svg + svgDocOffset + 10;
            if (absoluteOffset >= _data.Length || absoluteOffset + svgDocLength > _data.Length)
                return null;

            byte[] svgBytes = new byte[svgDocLength];
            Array.Copy(_data, absoluteOffset, svgBytes, 0, svgDocLength);

            // Convert to string, assuming the content starts directly at the given offset
            string svgContent = Encoding.UTF8.GetString(svgBytes);

            // Find the start of the <svg> tag
            int startIndex = svgContent.IndexOf("<svg", StringComparison.OrdinalIgnoreCase);
            if (startIndex == -1)
                return null;

            // Extract content from the <svg> tag to the end of the string
            string svgFragment = svgContent.Substring(startIndex);

            return svgFragment;
        }

        // TODO OPTIMIZE: binary search
        private uint FindTable(string tag)
        {
            var tableCount = ReadU16(4);

            if (tableCount <= 0)
            {
                return 0;
            }

            uint tableDir = 12;

            for (uint i = 0; i < tableCount; i++)
            {
                uint loc = tableDir + 16 * i;
                if (HasTag(loc, tag))
                    return ReadU32(loc + 8);
            }
            return 0;
        }

        public void GetGlyphHMetrics(int glyphIndex, out int advanceWidth, out int leftSideBearing)
        {
            advanceWidth = 0;
            leftSideBearing = 0;

            if (_hhea == 0)
                return;

            uint numOfLongHorMetrics = ReadU16(_hhea + 34);
            if (glyphIndex < numOfLongHorMetrics)
            {
                advanceWidth = ReadS16((uint)(_hmtx + 4 * glyphIndex));
                leftSideBearing = ReadS16((uint)(_hmtx + 4 * glyphIndex + 2));
            }
            else
            {
                advanceWidth = ReadS16(_hmtx + 4 * (numOfLongHorMetrics - 1));
                leftSideBearing = ReadS16((uint)(_hmtx + 4 * numOfLongHorMetrics + 2 * (glyphIndex - numOfLongHorMetrics)));
            }
        }

        public void GetGlyphVMetrics(int glyphIndex, out int advanceHeight, out int topSideBearing)
        {
            advanceHeight = 0;
            topSideBearing = 0;

            if (_vhea == 0)
                return;

            uint numOfLongVerMetrics = ReadU16(_vhea + 34);
            if (glyphIndex < numOfLongVerMetrics)
            {
                advanceHeight = ReadS16((uint)(_vmtx + 4 * glyphIndex));
                topSideBearing = ReadS16((uint)(_vmtx + 4 * glyphIndex + 2));
            }
            else
            {
                advanceHeight = ReadS16(_vmtx + 4 * (numOfLongVerMetrics - 1));
                topSideBearing = ReadS16((uint)(_vmtx + 4 * numOfLongVerMetrics + 2 * (glyphIndex - numOfLongVerMetrics)));
            }
        }

        private int GetCoverageIndex(uint coverageTable, int glyph)
        {
            ushort coverageFormat = ReadU16(coverageTable);
            switch (coverageFormat)
            {
                case 1:
                    ushort glyphCount = ReadU16(coverageTable + 2);

                    // Binary search.
                    int l = 0, r = glyphCount - 1, m;
                    int straw, needle = glyph;
                    while (l <= r)
                    {
                        uint glyphArray = coverageTable + 4;
                        ushort glyphID;
                        m = l + r >> 1;
                        glyphID = ReadU16((uint)(glyphArray + 2 * m));
                        straw = glyphID;
                        if (needle < straw)
                            r = m - 1;
                        else if (needle > straw)
                            l = m + 1;
                        else
                            return m;
                    }
                    break;

                case 2:
                    ushort rangeCount = ReadU16(coverageTable + 2);
                    uint rangeArray = coverageTable + 4;

                    // Binary search.
                    l = 0; r = rangeCount - 1;
                    int strawStart, strawEnd;
                    needle = glyph;
                    while (l <= r)
                    {
                        m = l + r >> 1;
                        uint rangeRecord = (uint)(rangeArray + 6 * m);
                        strawStart = ReadU16(rangeRecord);
                        strawEnd = ReadU16(rangeRecord + 2);
                        if (needle < strawStart)
                            r = m - 1;
                        else if (needle > strawEnd)
                            l = m + 1;
                        else
                        {
                            ushort startCoverageIndex = ReadU16(rangeRecord + 4);
                            return startCoverageIndex + glyph - strawStart;
                        }
                    }
                    break;

                default:
                    return -1; // unsupported
            }

            return -1;
        }

        private int GetGlyphClass(uint classDefTable, int glyph)
        {
            ushort classDefFormat = ReadU16(classDefTable);
            switch (classDefFormat)
            {
                case 1:
                    ushort startGlyphID = ReadU16(classDefTable + 2);
                    ushort glyphCount = ReadU16(classDefTable + 4);
                    uint classDef1ValueArray = classDefTable + 6;

                    if (glyph >= startGlyphID && glyph < startGlyphID + glyphCount)
                        return ReadU16((uint)(classDef1ValueArray + 2 * (glyph - startGlyphID)));
                    break;

                case 2:
                    ushort classRangeCount = ReadU16(classDefTable + 2);
                    uint classRangeRecords = classDefTable + 4;

                    // Binary search.
                    int l = 0, r = classRangeCount - 1, m;
                    int strawStart, strawEnd, needle = glyph;
                    while (l <= r)
                    {
                        m = l + r >> 1;
                        uint classRangeRecord = (uint)(classRangeRecords + 6 * m);
                        strawStart = ReadU16(classRangeRecord);
                        strawEnd = ReadU16(classRangeRecord + 2);
                        if (needle < strawStart)
                            r = m - 1;
                        else if (needle > strawEnd)
                            l = m + 1;
                        else
                            return ReadU16(classRangeRecord + 4);
                    }
                    break;

                default:
                    return -1; // Unsupported definition type, return an error.
            }

            // "All glyphs not assigned to a class fall into class 0". (OpenType spec)
            return 0;
        }

        private int GetGlyphGPOSInfoAdvance(int glyph1, int glyph2)
        {
            if (_gpos == 0)
                return 0;

            if (ReadU16(_gpos + 0) != 1)
                return 0; // Major version 1
            if (ReadU16(_gpos + 2) != 0)
                return 0; // Minor version 0

            ushort lookupListOffset = ReadU16(_gpos + 8);
            uint lookupList = _gpos + lookupListOffset;
            ushort lookupCount = ReadU16(lookupList);

            for (int i = 0; i < lookupCount; ++i)
            {
                ushort lookupOffset = ReadU16((uint)(lookupList + 2 + 2 * i));
                uint lookupTable = lookupList + lookupOffset;

                ushort lookupType = ReadU16(lookupTable);
                ushort subTableCount = ReadU16(lookupTable + 4);
                uint subTableOffsets = lookupTable + 6;
                if (lookupType != 2) // Pair Adjustment Positioning Subtable
                    continue;

                for (int sti = 0; sti < subTableCount; sti++)
                {
                    ushort subtableOffset = ReadU16((uint)(subTableOffsets + 2 * sti));
                    uint table = lookupTable + subtableOffset;
                    ushort posFormat = ReadU16(table);
                    ushort coverageOffset = ReadU16(table + 2);
                    int coverageIndex = GetCoverageIndex(table + coverageOffset, glyph1);
                    if (coverageIndex == -1)
                        continue;

                    switch (posFormat)
                    {
                        case 1:
                            ushort valueFormat1 = ReadU16(table + 4);
                            ushort valueFormat2 = ReadU16(table + 6);
                            if (valueFormat1 == 4 && valueFormat2 == 0)
                            {
                                int valueRecordPairSizeInBytes = 2;
                                ushort pairSetCount = ReadU16(table + 8);
                                if (coverageIndex >= pairSetCount)
                                    return 0;

                                ushort pairPosOffset = ReadU16((uint)(table + 10 + 2 * coverageIndex));
                                uint pairValueTable = table + pairPosOffset;
                                ushort pairValueCount = ReadU16(pairValueTable);
                                uint pairValueArray = pairValueTable + 2;

                                int l = 0, r = pairValueCount - 1, m;
                                int needle = glyph2;

                                // Binary search.
                                while (l <= r)
                                {
                                    m = l + r >> 1;
                                    uint pairValue = (uint)(pairValueArray + (2 + valueRecordPairSizeInBytes) * m);
                                    ushort secondGlyph = ReadU16(pairValue);
                                    if (needle < secondGlyph)
                                        r = m - 1;
                                    else if (needle > secondGlyph)
                                        l = m + 1;
                                    else
                                    {
                                        short xAdvance = ReadS16(pairValue + 2);
                                        return xAdvance;
                                    }
                                }
                            }
                            else
                                return 0;
                            break;

                        case 2:
                            valueFormat1 = ReadU16(table + 4);
                            valueFormat2 = ReadU16(table + 6);
                            if (valueFormat1 == 4 && valueFormat2 == 0)
                            {
                                ushort classDef1Offset = ReadU16(table + 8);
                                ushort classDef2Offset = ReadU16(table + 10);
                                int glyph1class = GetGlyphClass(table + classDef1Offset, glyph1);
                                int glyph2class = GetGlyphClass(table + classDef2Offset, glyph2);

                                ushort class1Count = ReadU16(table + 12);
                                ushort class2Count = ReadU16(table + 14);
                                uint class1Records = table + 16;
                                uint class2Records = (uint)(class1Records + 2 * glyph1class * class2Count);
                                short xAdvance = ReadS16((uint)(class2Records + 2 * glyph2class));
                                return xAdvance;
                            }

                            return 0;

                        default:
                            return 0; // Unsupported position format
                    }
                }
            }

            return 0;
        }


        private int GetGlyphKernInfoAdvance(int glyph1, int glyph2)
        {
            if (_kern == 0)
                return 0;

            // we only look at the first table. it must be 'horizontal' and format 0.
            if (ReadU16(_kern + 2) < 1) // number of tables
                return 0;

            if (ReadU16(_kern + 8) != 1) // horizontal flag, format
                return 0;

            int l = 0;
            int r = ReadU16(_kern + 10) - 1;
            uint needle = (uint)(glyph1 << 16 | glyph2);
            while (l <= r)
            {
                var m = l + r >> 1;
                var straw = ReadU32((uint)(_kern + 18 + m * 6)); // note: unaligned read
                if (needle < straw)
                    r = m - 1;
                else if (needle > straw)
                    l = m + 1;
                else
                    return ReadS16((uint)(_kern + 22 + m * 6));
            }

            return 0;
        }

        private int GetGlyphKernAdvance(int glyph1, int glyph2)
        {
            int xAdvance = 0;

            if (glyph1 == 0 || glyph2 == 0)
                return 0;

            if (_gpos != 0)
                xAdvance += GetGlyphGPOSInfoAdvance(glyph1, glyph2);
            else if (_kern != 0)
                xAdvance += GetGlyphKernInfoAdvance(glyph1, glyph2);

            return xAdvance;
        }

        private int GetCodepointKernAdvance(char ch1, char ch2)
        {
            if (_kern == 0 && _gpos == 0) // if no kerning table, don't waste time looking up both codepoint->glyphs
                return 0;

            return GetGlyphKernAdvance(FindGlyphIndex(ch1), FindGlyphIndex(ch2));
        }

        public void GetCodepointHMetrics(char codepoint, out int advanceWidth, out int leftSideBearing)
        {
            GetGlyphHMetrics(FindGlyphIndex(codepoint), out advanceWidth, out leftSideBearing);
        }

        public void GetCodepointVMetrics(char codepoint, out int advanceHeight, out int topSideBearing)
        {
            GetGlyphVMetrics(FindGlyphIndex(codepoint), out advanceHeight, out topSideBearing);
        }

        // advanceWidthMax is the Maximum advance width from the 'hhea' table
        // minLeftSideBearing is the Minimum left side bearing
        // minRightSideBearing is the Minimum right side bearing
        // xMaxExtent is the Maximum x extent (right-most coordinate)
        public void GetFontHMetrics(out int advanceWidthMax, out int minLeftSideBearing, out int minRightSideBearing, out int xMaxExtent)
        {
            advanceWidthMax = ReadS16(_hhea + 10);
            minLeftSideBearing = ReadS16(_hhea + 12);
            minRightSideBearing = ReadS16(_hhea + 14);
            xMaxExtent = ReadS16(_hhea + 16);
        }

        // ascent is the coordinate above the baseline the font extends; 
        // descent is the coordinate below the baseline the font extends (i.e. it is typically negative)
        // lineGap is the spacing between one row's descent and the next row's ascent...
        // you should advance the vertical position by "*ascent - *descent + *lineGap"
        // these are expressed in unscaled coordinates, so you must multiply by the scale factor for a given size
        public void GetFontVMetrics(out int ascent, out int descent, out int lineGap)
        {
            ascent = ReadS16(_hhea + 4);
            descent = ReadS16(_hhea + 6);
            lineGap = ReadS16(_hhea + 8);
        }

        public int GetCapHeight()
        {
            if (_os2 == 0)
            {
                // No OS/2 table: fallback to ascent
                GetFontVMetrics(out int ascent, out _, out _);
                return ascent;
            }

            ushort version = ReadU16(_os2 + 0);
            if (version < 2)
            {
                // sCapHeight only exists in OS/2 version 2 or later
                GetFontVMetrics(out int ascent, out _, out _);
                return ascent;
            }

            // Read sCapHeight from OS/2 table (offset 86)
            return ReadS16(_os2 + 86);
        }

        private async Task<GlyphBitmap> GetCodePointBitmap(float scaleX, float scaleY, float shiftX, float shiftY, char codePoint, Color color, Color backgroundColor)
        {
            int glyph = FindGlyphIndex(codePoint);

            // Check if the glyph is an SVG
            //if (_svgDocuments.ContainsKey(glyph))
            if (_svg != 0)
            {
                string svgDoc = null;

                if (SvgRender != null && _svgDocuments.TryGetValue(glyph, out svgDoc))
                {
                    var ret = await SvgRender(this, svgDoc, codePoint, glyph, _userData);

                    return ret;
                }

                return new GlyphBitmap(0, 0, true, backgroundColor);
            }

            var glyphMetrics = await GetGlyphMetrics(codePoint, scaleX, scaleY, shiftX, shiftY);

            var vertices = GetGlyphShape(glyph);

            // now we get the size
            var glyphBitmap = new GlyphBitmap((int)glyphMetrics.Bounds.Width, (int)glyphMetrics.Bounds.Height, false, backgroundColor);
            Rasterize(glyphBitmap, 0.35f, vertices, scaleX, scaleY, shiftX, shiftY, (int)glyphMetrics.Bounds.X, (int)glyphMetrics.Bounds.Y, true, color);

            return glyphBitmap;
        }

        private ushort FindGlyphIndex(char unicodeCodePoint)
        {
            var format = ReadU16(_indexMap);

            switch (format)
            {
                // apple byte encoding
                case 0:
                    {
                        var bytes = ReadU16(_indexMap + 2);
                        if (unicodeCodePoint < bytes - 6)
                            return _data[_indexMap + 6 + unicodeCodePoint];

                        return 0;
                    }

                case 6:
                    {
                        var first = ReadU16(_indexMap + 6);
                        var count = ReadU16(_indexMap + 8);
                        if (unicodeCodePoint >= first && unicodeCodePoint < first + count)
                            return ReadU16((uint)(_indexMap + 10 + (unicodeCodePoint - first) * 2));

                        return 0;
                    }

                // TODO: high-byte mapping for japanese/chinese/korean
                case 2:
                    return 0;

                // standard mapping for windows fonts: binary search collection of ranges
                case 4:
                    {
                        var segcount = ReadU16(_indexMap + 6) >> 1;
                        var searchRange = ReadU16(_indexMap + 8) >> 1;
                        var entrySelector = ReadU16(_indexMap + 10);
                        var rangeShift = ReadU16(_indexMap + 12) >> 1;

                        // do a binary search of the segments
                        var endCount = _indexMap + 14;
                        var search = endCount;

                        if (unicodeCodePoint > 0xFFFF)
                            return 0;

                        // they lie from endCount .. endCount + segCount
                        // but searchRange is the nearest power of two, so...
                        if (unicodeCodePoint >= ReadU16((uint)(search + rangeShift * 2)))
                            search += (uint)(rangeShift * 2);

                        // now decrement to bias correctly to find smallest
                        search -= 2;
                        while (entrySelector != 0)
                        {
                            searchRange >>= 1;
                            var endValue2 = ReadU16((uint)(search + searchRange * 2));

                            if (unicodeCodePoint > endValue2)
                                search += (uint)searchRange * 2;

                            entrySelector--;
                        }

                        search += 2;

                        var item = (ushort)(search - endCount >> 1);

                        //STBTT_assert(unicode_codepoint <= ttUSHORT(data + endCount + 2*item));
                        var startValue = ReadU16((uint)(_indexMap + 14 + segcount * 2 + 2 + 2 * item));
                        var endValue = ReadU16((uint)(_indexMap + 14 + 2 + 2 * item));
                        if (unicodeCodePoint < startValue || unicodeCodePoint > endValue)
                            return 0;

                        var offset = ReadU16((uint)(_indexMap + 14 + segcount * 6 + 2 + 2 * item));
                        if (offset == 0)
                            return (ushort)(unicodeCodePoint + ReadS16((uint)(_indexMap + 14 + segcount * 4 + 2 + 2 * item)));

                        return ReadU16((uint)(offset + (unicodeCodePoint - startValue) * 2 + _indexMap + 14 + segcount * 6 + 2 + 2 * item));
                    }

                case 12:
                case 13:
                    {
                        int ngroups = ReadU16(_indexMap + 6);
                        int low = 0;
                        int high = ngroups;

                        // Binary search the right group.
                        while (low <= high)
                        {
                            var mid = low + (high - low >> 1); // rounds down, so low <= mid < high
                            var startChar = ReadU32((uint)(_indexMap + 16 + mid * 12));
                            var endChar = ReadU32((uint)(_indexMap + 16 + mid * 12 + 4));
                            if (unicodeCodePoint < startChar)
                                high = mid - 1;
                            else if (unicodeCodePoint > endChar)
                                low = mid + 1;
                            else
                            {
                                uint startGlyph = ReadU32((uint)(_indexMap + 16 + mid * 12 + 8));
                                if (format == 12)
                                    return (ushort)(startGlyph + unicodeCodePoint - startChar);
                                else // format == 13
                                    return (ushort)startGlyph;

                            }
                        }

                        return 0; // not found
                    }

                // TODO
                default:
                    return 0;
            }
        }

        private int GetGlyfOffset(int glyphIndex)
        {
            if (glyphIndex >= _glyphCount)
                return -1; // glyph index out of range

            if (_indexToLocFormat >= 2)
                return -1; // unknown index->glyph map format

            int g1, g2;
            if (_indexToLocFormat == 0)
            {
                g1 = (int)(_glyf + ReadU16((uint)(_loca + glyphIndex * 2)) * 2);
                g2 = (int)(_glyf + ReadU16((uint)(_loca + glyphIndex * 2 + 2)) * 2);
            }
            else
            {
                g1 = (int)(_glyf + ReadU32((uint)(_loca + glyphIndex * 4)));
                g2 = (int)(_glyf + ReadU32((uint)(_loca + glyphIndex * 4 + 4)));
            }

            return g1 == g2 ? -1 : g1; // if length is 0, return -1
        }

        private bool GetGlyphBox(int glyphIndex, out int x0, out int y0, out int x1, out int y1)
        {
            x0 = 0;
            y0 = 0;
            x1 = 0;
            y1 = 0;

            var g = GetGlyfOffset(glyphIndex);

            if (g < 0)
                return false;

            x0 = ReadS16((uint)g + 2);
            y0 = ReadS16((uint)g + 4);
            x1 = ReadS16((uint)g + 6);
            y1 = ReadS16((uint)g + 8);

            return true;
        }

        public bool GetCodepointBox(char codepoint, out int x0, out int y0, out int x1, out int y1)
        {
            return GetGlyphBox(FindGlyphIndex(codepoint), out x0, out y0, out x1, out y1);
        }

        private void CloseShape(List<Vertex> vertices, bool wasOff, bool startOff, short sx, short sy, short scx, short scy, short cx, short cy)
        {
            if (startOff)
            {
                if (wasOff)
                    vertices.Add(new Vertex(VCURVE, (short)(cx + scx >> 1), (short)(cy + scy >> 1), cx, cy));
                vertices.Add(new Vertex(VCURVE, sx, sy, scx, scy));
            }
            else
            {
                if (wasOff)
                    vertices.Add(new Vertex(VCURVE, sx, sy, cx, cy));
                else
                    vertices.Add(new Vertex(VLINE, sx, sy, 0, 0));
            }
        }

        private List<Vertex> GetGlyphShape(int glyphIndex)
        {
            var g = GetGlyfOffset(glyphIndex);

            if (g < 0)
                return new List<Vertex>();

            var result = new List<Vertex>();
            var numberOfContours = ReadS16((uint)g);

            if (numberOfContours > 0)
            {
                byte flags = 0;
                int j = 0;
                bool wasOff = false;
                bool startOff = false;
                uint endPtsOfContours = (uint)(g + 10);
                int ins = ReadU16((uint)(g + 10 + numberOfContours * 2));
                uint pointOffset = (uint)(g + 10 + numberOfContours * 2 + 2 + ins);

                int n = 1 + ReadU16((uint)(endPtsOfContours + numberOfContours * 2 - 2));

                int m = n + 2 * numberOfContours;  // a loose bound on how many vertices we might need

                //Result.Count := M;
                //SetLength(Result.List, Result.Count);
                var vertices = new Vertex[m];

                int nextMove = 0;
                byte flagCount = 0;

                // in first pass, we load uninterpreted data into the allocated array
                // above, shifted to the end of the array so we won't overwrite it when
                // we create our final data starting from the front

                int off = m - n; // starting offset for uninterpreted data, regardless of how m ends up being calculated

                // first load flags

                int pointIndex = 0;
                for (var i = 0; i < n; i++)
                {
                    if (flagCount == 0)
                    {
                        flags = _data[pointOffset + pointIndex++];

                        if ((flags & 8) != 0)
                            flagCount = _data[pointOffset + pointIndex++];
                    }
                    else
                        flagCount--;

                    vertices[off + i].vertexType = flags;
                }

                // now load x coordinates
                short x = 0;
                for (var i = 0; i < n; i++)
                {
                    flags = vertices[off + i].vertexType;
                    if ((flags & 2) != 0)
                    {
                        byte dx = _data[pointOffset + pointIndex++];
                        x += (short)((flags & 16) != 0 ? dx : -dx);
                    }
                    else
                    {
                        if ((flags & 16) == 0)
                        {
                            x += ReadS16((uint)(pointOffset + pointIndex)); // PORT
                            pointIndex += 2;
                        }
                    }
                    vertices[off + i].x = x;
                }

                // now load y coordinates
                short y = 0;
                for (var i = 0; i < n; i++)
                {
                    flags = vertices[off + i].vertexType;
                    if ((flags & 4) != 0)
                    {
                        byte dy = _data[pointOffset + pointIndex++];
                        y += (short)((flags & 32) != 0 ? dy : -dy);
                    }
                    else
                    {
                        if ((flags & 32) == 0)
                        {
                            y += ReadS16((uint)(pointOffset + pointIndex)); // PORT
                            pointIndex += 2;
                        }
                    }
                    vertices[off + i].y = y;
                }

                // now convert them to our format
                short sx = 0, sy = 0, cx = 0, cy = 0, scx = 0, scy = 0;
                for (var i = 0; i < n; i++)
                {
                    flags = vertices[off + i].vertexType;
                    x = vertices[off + i].x;
                    y = vertices[off + i].y;

                    if (nextMove == i)
                    {
                        // when we get to the end, we have to close the shape explicitly
                        if (i != 0)
                            CloseShape(result, wasOff, startOff, sx, sy, scx, scy, cx, cy);

                        // now start the new one
                        startOff = (flags & 1) == 0;
                        if (startOff)
                        {
                            // if we start off with an off-curve point, then when we need to find a point on the curve
                            // where we can start, and we need to save some state for when we wraparound.
                            scx = x;
                            scy = y;
                            if ((vertices[off + i + 1].vertexType & 1) == 0)
                            {
                                // next point is also a curve point, so interpolate an on-point curve
                                sx = (short)(x + vertices[off + i + 1].x >> 1);
                                sy = (short)(y + vertices[off + i + 1].y >> 1);
                            }
                            else
                            {
                                // otherwise just use the next point as our start point
                                sx = vertices[off + i + 1].x;
                                sy = vertices[off + i + 1].y;
                                i++; // we're using point i+1 as the starting point, so skip it
                            }
                        }
                        else
                        {
                            sx = x;
                            sy = y;
                        }

                        result.Add(new Vertex(VMOVE, sx, sy, 0, 0));

                        wasOff = false;
                        nextMove = 1 + ReadU16((uint)(endPtsOfContours + j * 2));
                        j++;
                    }
                    else
                    {
                        if ((flags & 1) == 0) // if it's a curve
                        {
                            if (wasOff) // two off-curve control points in a row means interpolate an on-curve midpoint
                                result.Add(new Vertex(VCURVE, (short)(cx + x >> 1), (short)(cy + y >> 1), cx, cy));
                            cx = x;
                            cy = y;
                            wasOff = true;
                        }
                        else
                        {
                            if (wasOff)
                                result.Add(new Vertex(VCURVE, x, y, cx, cy));
                            else
                                result.Add(new Vertex(VLINE, x, y, 0, 0));

                            wasOff = false;
                        }
                    }
                }

                CloseShape(result, wasOff, startOff, sx, sy, scx, scy, cx, cy);
            }
            else if (numberOfContours < 0)
            {
                // Compound shapes.
                bool more = true;
                var comp2 = (uint)(g + 10);

                var mtx = new float[6];

                while (more)
                {
                    mtx[0] = 1;
                    mtx[1] = 0;
                    mtx[2] = 0;
                    mtx[3] = 1;
                    mtx[4] = 0;
                    mtx[5] = 0;

                    short flags = ReadS16(comp2);
                    comp2 += 2;
                    short gidx = ReadS16(comp2);
                    comp2 += 2;

                    if ((flags & 2) != 0)// XY values
                    {
                        if ((flags & 1) != 0)// shorts
                        {
                            mtx[4] = ReadS16(comp2);
                            comp2 += 2;
                            mtx[5] = ReadS16(comp2);
                            comp2 += 2;
                        }
                        else
                        {
                            mtx[4] = Read8(comp2);
                            comp2++;
                            mtx[5] = Read8(comp2);
                            comp2++;
                        }
                    }
                    else
                    {
                        // TODO handle matching point
                        throw new NotImplementedException("matching point");
                    }

                    if ((flags & 1 << 3) != 0) // WE_HAVE_A_SCALE
                    {
                        mtx[0] = ReadS16(comp2) / 16384f;
                        mtx[1] = 0;
                        mtx[2] = 0;
                        mtx[3] = ReadS16(comp2) / 16384f;
                        comp2 += 2;
                    }
                    else if ((flags & 1 << 6) != 0)// WE_HAVE_AN_X_AND_YSCALE
                    {
                        mtx[0] = ReadS16(comp2) / 16384f;
                        comp2 += 2;
                        mtx[1] = 0;
                        mtx[2] = 0;
                        mtx[3] = ReadS16(comp2) / 16384f;
                        comp2 += 2;
                    }
                    else if ((flags & 1 << 7) != 0) // WE_HAVE_A_TWO_BY_TWO
                    {
                        mtx[0] = ReadS16(comp2) / 16384f;
                        comp2 += 2;
                        mtx[1] = ReadS16(comp2) / 16384f;
                        comp2 += 2;
                        mtx[2] = ReadS16(comp2) / 16384f;
                        comp2 += 2;
                        mtx[3] = ReadS16(comp2) / 16384f;
                        comp2 += 2;
                    }

                    // Find transformation scales.
                    var ms = (float)Math.Sqrt(mtx[0] * mtx[0] + mtx[1] * mtx[1]);
                    var ns = (float)Math.Sqrt(mtx[2] * mtx[2] + mtx[3] * mtx[3]);

                    // Get indexed glyph.
                    var compVerts = GetGlyphShape(gidx);
                    if (compVerts.Count > 0)
                    {
                        // Transform vertices.
                        for (var i = 0; i < compVerts.Count; i++)
                        {
                            var vert = compVerts[i];

                            var xx = vert.x;
                            var yy = vert.y;

                            vert.x = (short)(ms * (mtx[0] * xx + mtx[2] * yy + mtx[4]));
                            vert.y = (short)(ns * (mtx[1] * xx + mtx[3] * yy + mtx[5]));

                            xx = vert.cx;
                            yy = vert.cy;

                            vert.cx = (short)(ms * (mtx[0] * xx + mtx[2] * yy + mtx[4]));
                            vert.cy = (short)(ns * (mtx[1] * xx + mtx[3] * yy + mtx[5]));

                            // Append vertices.
                            result.Add(vert);
                        }
                    }

                    // More components ?
                    more = (flags & 1 << 5) != 0;
                }
            }
            else
            {
                // numberOfCounters == 0, do nothing
            }

            return result;
        }

        // antialiasing software rasterizer
        public void GetGlyphBoundingBox(int glyph, float scaleX, float scaleY, float shiftX, float shiftY, out int ix0, out int iy0, out int ix1, out int iy1)
        {
            ix0 = 0;
            iy0 = 0;
            ix1 = 0;
            iy1 = 0;

            int x0 = 0, y0 = 0, x1 = 0, y1 = 0;

            if (GetGlyphBox(glyph, out x0, out y0, out x1, out y1))
            {
                // now move to integral bboxes (treating pixels as little squares, what pixels get touched)?
                ix0 = (int)Math.Floor(x0 * scaleX + shiftX);
                iy0 = (int)Math.Floor(-y1 * scaleY + shiftY);
                ix1 = (int)Math.Ceiling(x1 * scaleX + shiftX);
                iy1 = (int)Math.Ceiling(-y0 * scaleY + shiftY);
            }
        }

        public void GetGlyphBitmapBox(char codepoint, float scaleX, float scaleY, float shiftX, float shiftY, out int ix0, out int iy0, out int ix1, out int iy1)
        {
            GetGlyphBoundingBox(FindGlyphIndex(codepoint), scaleX, scaleY, shiftX, shiftY, out ix0, out iy0, out ix1, out iy1);
        }

        // tesselate until threshhold p is happy... TODO warped to compensate for non-linear stretching
        private void TesselateCurve(List<PointF> points, ref int numPoints, float x0, float y0, float x1, float y1, float x2, float y2, float objspaceFlatnessSquared, int n)
        //  mx, my, dx, dy: Single;
        {
            // midpoint
            float mx = (x0 + 2f * x1 + x2) / 4f;
            float my = (y0 + 2f * y1 + y2) / 4f;
            // versus directly drawn line
            float dx = (x0 + x2) / 2f - mx;
            float dy = (y0 + y2) / 2f - my;
            if (n > 16)// 65536 segments on one curve better be enough!
                return;

            if (dx * dx + dy * dy > objspaceFlatnessSquared)// half-pixel error allowed... need to be smaller if AA
            {
                TesselateCurve(points, ref numPoints, x0, y0, (x0 + x1) / 2f, (y0 + y1) / 2f, mx, my, objspaceFlatnessSquared, n + 1);
                TesselateCurve(points, ref numPoints, mx, my, (x1 + x2) / 2f, (y1 + y2) / 2f, x2, y2, objspaceFlatnessSquared, n + 1);
            }
            else
            {
                if (points != null)
                    points.Add(new PointF(x2, y2));
                numPoints++;
            }
        }

        static void TesselateCubic(List<PointF> points, ref int numPoints, float x0, float y0, float x1, float y1, float x2, float y2, float x3, float y3, float objspaceFlatnessSquared, int n)
        {
            // @TODO this "flatness" calculation is just made-up nonsense that seems to work well enough
            float dx0 = x1 - x0;
            float dy0 = y1 - y0;
            float dx1 = x2 - x1;
            float dy1 = y2 - y1;
            float dx2 = x3 - x2;
            float dy2 = y3 - y2;
            float dx = x3 - x0;
            float dy = y3 - y0;
            float longlen = (float)(Math.Sqrt(dx0 * dx0 + dy0 * dy0) + Math.Sqrt(dx1 * dx1 + dy1 * dy1) + Math.Sqrt(dx2 * dx2 + dy2 * dy2));
            float shortlen = (float)Math.Sqrt(dx * dx + dy * dy);
            float flatness_squared = longlen * longlen - shortlen * shortlen;

            if (n > 16) // 65536 segments on one curve better be enough!
                return;

            if (flatness_squared > objspaceFlatnessSquared)
            {
                float x01 = (x0 + x1) / 2f;
                float y01 = (y0 + y1) / 2f;
                float x12 = (x1 + x2) / 2f;
                float y12 = (y1 + y2) / 2f;
                float x23 = (x2 + x3) / 2f;
                float y23 = (y2 + y3) / 2f;

                float xa = (x01 + x12) / 2f;
                float ya = (y01 + y12) / 2f;
                float xb = (x12 + x23) / 2f;
                float yb = (y12 + y23) / 2f;

                float mx = (xa + xb) / 2f;
                float my = (ya + yb) / 2f;

                TesselateCubic(points, ref numPoints, x0, y0, x01, y01, xa, ya, mx, my, objspaceFlatnessSquared, n + 1);
                TesselateCubic(points, ref numPoints, mx, my, xb, yb, x23, y23, x3, y3, objspaceFlatnessSquared, n + 1);
            }
            else
            {
                if (points != null)
                    points.Add(new PointF(x3, y3));
                numPoints++;
            }
        }

        // returns number of contours
        private int FlattenCurves(List<Vertex> vertices, float objSpaceFlatness, out int[] contours, out List<PointF> windings)
        {
            float objspaceFlatnessSquared = objSpaceFlatness * objSpaceFlatness;
            int n = 0;

            // count how many "moves" there are to get the contour count
            for (int i = 0; i < vertices.Count; i++)
            {
                if (vertices[i].vertexType == VMOVE)
                    n++;
            }

            int windingCount = n;
            windings = null;
            contours = null;

            if (n == 0)
                return 0;

            int numPoints = 0;
            int start = 0;

            contours = new int[n];

            // make two passes through the points so we don't need to realloc
            for (int pass = 0; pass < 2; pass++)
            {
                float x = 0;
                float y = 0;
                if (pass == 1)
                {
                    contours = new int[numPoints * 2];
                    windings = new List<PointF>(numPoints);
                }

                numPoints = 0;
                n = -1;

                for (int i = 0; i < vertices.Count; i++)
                {
                    switch (vertices[i].vertexType)
                    {
                        case VMOVE:
                            {
                                // start the next contour
                                if (n >= 0)
                                    contours[n] = numPoints - start;
                                n++;
                                start = numPoints;

                                x = vertices[i].x;
                                y = vertices[i].y;
                                if (windings != null)
                                    windings.Add(new PointF(x, y));
                                numPoints++;
                                break;
                            }

                        case VLINE:
                            {
                                x = vertices[i].x;
                                y = vertices[i].y;

                                if (windings != null)
                                    windings.Add(new PointF(x, y));

                                numPoints++;
                                break;
                            }

                        case VCURVE:
                            {
                                TesselateCurve(windings, ref numPoints, x, y,
                                                         vertices[i].cx, vertices[i].cy,
                                                         vertices[i].x, vertices[i].y,
                                                         objspaceFlatnessSquared, 0);
                                x = vertices[i].x;
                                y = vertices[i].y;
                                break;
                            }

                        case VCUBIC:
                            {
                                TesselateCubic(windings, ref numPoints, x, y,
                                                         vertices[i].cx, vertices[i].cy,
                                                         vertices[i].cx1, vertices[i].cy1,
                                                         vertices[i].x, vertices[i].y,
                                                         objspaceFlatnessSquared, 0);
                                x = vertices[i].x;
                                y = vertices[i].y;
                                break;
                            }
                        default:
                            break;
                    }
                }

                contours[n] = numPoints - start;
            }

            return windingCount;
        }

        private ActiveEdge CreateActiveEdge(Edge edge, int offX, float startPoint)
        {
            var z = new ActiveEdge(); // TODO: make a pool of these!!!

            float dxdy = (edge.x1 - edge.x0) / (edge.y1 - edge.y0);
            //STBTT_assert(e->y0 <= start_point);

            // round dx down to avoid going too far
            if (dxdy < 0)
                z.dx = -(int)Math.Floor(FIX * -dxdy);
            else
                z.dx = (int)Math.Floor(FIX * dxdy);

            z.x = (int)Math.Floor(FIX * (edge.x0 + dxdy * (startPoint - edge.y0))); // use z.dx so when we offset later it's by the same amount
            z.x -= offX * FIX;
            z.ey = edge.y1;
            z.next = null;
            z.direction = edge.invert ? 1 : -1;
            return z;
        }

        // note: this routine clips fills that extend off the edges... 
        // ideally this wouldn't happen, but it could happen if the truetype glyph bounding boxes are wrong, or if the user supplies a too-small bitmap
        private void FillActiveEdges(byte[] scanline, int len, ActiveEdge e, int maxWeight, Color color)
        {
            // non-zero winding fill
            int x0 = 0;
            int w = 0;
            int x1;

            while (e != null)
            {
                if (w == 0)
                {
                    // if we're currently at zero, we need to record the edge start point
                    x0 = e.x;
                    w += e.direction;
                }
                else
                {
                    x1 = e.x;
                    w += e.direction;

                    // if we went to zero, we need to draw
                    if (w == 0)
                    {
                        int i = x0 >> FIXSHIFT;
                        int j = x1 >> FIXSHIFT;

                        if (i < len && j >= 0)
                        {
                            if (i == j)
                            {
                                // x0,x1 are the same pixel, so compute combined coverage
                                int coverage = (x1 - x0) * maxWeight >> FIXSHIFT;
                                AddCoverage(scanline, i, coverage, color);
                            }
                            else
                            {
                                if (i >= 0) // add antialiasing for x0
                                    AddCoverage(scanline, i, (FIX - (x0 & FIXMASK)) * maxWeight >> FIXSHIFT, color);
                                else
                                    i = -1; // clip

                                if (j < len) // add antialiasing for x1
                                    AddCoverage(scanline, j, (x1 & FIXMASK) * maxWeight >> FIXSHIFT, color);
                                else
                                    j = len; // clip

                                for (++i; i < j; ++i) // fill pixels between x0 and x1
                                    AddCoverage(scanline, i, maxWeight, color);
                            }
                        }
                    }
                }
                e = e.next;
            }
        }

        private void AddCoverage(byte[] scanline, int index, int coverage, Color color)
        {
            if (index >= 0 && index < scanline.Length / 4)
            {
                int offset = index * 4;
                byte existingAlpha = scanline[offset + 3];

                // Compute the new alpha value with coverage
                byte newAlpha = (byte)Math.Min(255, existingAlpha + coverage);

                // Set the RGB channels to white (255) when adding coverage
                scanline[offset] = (byte)color.R;     // Red
                scanline[offset + 1] = (byte)color.G; // Green
                scanline[offset + 2] = (byte)color.B; // Blue
                scanline[offset + 3] = newAlpha; // Alpha
            }
        }

        private void RasterizeSortedEdges(GlyphBitmap bitmap, List<Edge> e, int vSubSamples, int offX, int offY, Color color)
        {
            int eIndex = 0;

            ActiveEdge active = null;
            int maxWeight = 255 / vSubSamples;  // weight per vertical scanline

            int y = offY * vSubSamples;

            int n = e.Count - 1;
            var tempEdge = e[n];
            tempEdge.y0 = (offY + bitmap.Height) * vSubSamples + 1;
            e[n] = tempEdge;

            var scanline = new byte[bitmap.Width * 4];

            float scanY = 0;

            int j = 0;
            while (j < bitmap.Height)
            {
                for (int i = 0; i < bitmap.Width * 4; i++)
                    scanline[i] = 0;

                for (int s = 0; s < vSubSamples; s++)
                {
                    // find center of pixel for this scanline
                    scanY = y + 0.5f;

                    // update all active edges;
                    // remove all active edges that terminate before the center of this scanline
                    var step = active;
                    ActiveEdge prev = null;
                    while (step != null)
                    {
                        if (step.ey <= scanY)
                        {
                            // delete from list
                            if (prev != null)
                                prev.next = step.next;
                            else
                                active = step.next;

                            Debug.Assert(step.direction != 0);

                            step.direction = 0;
                            step = step.next;
                        }

                        else
                        {
                            step.x += step.dx; // advance to position for current scanline

                            prev = step;
                            step = step.next; // advance through list
                        }
                    }

                    // resort the list if needed
                    while (true)
                    {
                        bool changed = false;

                        step = active;
                        prev = null;
                        while (step != null && step.next != null)
                        {
                            var prox = step.next;
                            if (step.x > prox.x)
                            {
                                if (prev == null)
                                    active = prox;
                                else
                                    prev.next = prox;

                                step.next = prox.next;
                                prox.next = step;

                                changed = true;
                            }

                            prev = step;
                            step = step.next; // advance through list
                        }

                        if (!changed)
                            break;
                    }

                    // insert all edges that start before the center of this scanline -- omit ones that also end on this scanline
                    while (e[eIndex].y0 <= scanY)
                    {
                        if (e[eIndex].y1 > scanY)
                        {
                            var z = CreateActiveEdge(e[eIndex], offX, scanY);
                            // find insertion point
                            if (active == null)
                                active = z;
                            else
                             if (z.x < active.x) // insert at front
                            {
                                z.next = active;
                                active = z;
                            }
                            else
                            {
                                // find thing to insert AFTER
                                var p = active;
                                while (p.next != null && p.next.x < z.x)
                                    p = p.next;

                                // at this point, p->next->x is NOT < z->x
                                z.next = p.next;
                                p.next = z;
                            }
                        }

                        eIndex++;
                    }

                    // now process all active edges in XOR fashion
                    if (active != null)
                        FillActiveEdges(scanline, bitmap.Width, active, maxWeight, color);

                    y++;
                }

                // Update bitmap pixels from scanline
                for (int i = 0; i < bitmap.Width; i++)
                {
                    int ofs = (i + j * bitmap.Width) * 4;
                    byte alpha = scanline[i * 4 + 3];
                    if (alpha > 0) // Optimization: Only update if there's non-zero alpha
                    {
                        bitmap.Pixels[ofs] = scanline[i * 4];       // Red
                        bitmap.Pixels[ofs + 1] = scanline[i * 4 + 1]; // Green
                        bitmap.Pixels[ofs + 2] = scanline[i * 4 + 2]; // Blue
                        bitmap.Pixels[ofs + 3] = alpha;              // Alpha
                    }
                }

                j++;
            }
        }

        private void Rasterize(GlyphBitmap glyphBitmap, float flatnessInPixels, List<Vertex> vertices, float scaleX, float scaleY, float shiftX, float shiftY, int xOff, int yOff, bool invert, Color color)
        {
            float scale = scaleX < scaleY ? scaleX : scaleY;

            int[] windingLengths;
            List<PointF> windings;
            int windingCount = FlattenCurves(vertices, flatnessInPixels / scale, out windingLengths, out windings);
            if (windingCount > 0)
                Rasterize(glyphBitmap, windings, windingLengths, windingCount, scaleX, scaleY, shiftX, shiftY, xOff, yOff, invert, color);
        }

        private void Rasterize(GlyphBitmap glyphBitmap, List<PointF> points, int[] windings, int windingCount, float scaleX, float scaleY, float shiftX, float shiftY, int xOff, int yOff, bool invert, Color color)
        {
            int ptOfs = 0;

            float yScaleInv = invert ? -scaleY : scaleY;

            // this value should divide 255 evenly; otherwise we won't reach full opacity
            int vSubSamples = glyphBitmap.Height < 8 ? 15 : 5;
            var edgeList = new List<Edge>(16);
            int m = 0;

            for (int i = 0; i < windingCount; i++)
            {
                ptOfs = m;

                m += windings[i];
                int j = windings[i] - 1;
                int k = 0;

                for (k = 0; k < windings[i]; j = k++)
                {
                    int a = k;
                    int b = j;

                    var en = new Edge();

                    // skip the edge if horizontal
                    if (points[ptOfs + j].Y == points[ptOfs + k].Y)
                        continue;

                    // add edge from j to k to the list
                    en.invert = false;

                    if (invert ? points[ptOfs + j].Y > points[ptOfs + k].Y : points[ptOfs + j].Y < points[ptOfs + k].Y)
                    {
                        en.invert = true;
                        a = j;
                        b = k;
                    }

                    en.x0 = points[ptOfs + a].X * scaleX + shiftX;
                    en.y0 = (points[ptOfs + a].Y * yScaleInv + shiftY) * vSubSamples;
                    en.x1 = points[ptOfs + b].X * scaleX + shiftX;
                    en.y1 = (points[ptOfs + b].Y * yScaleInv + shiftY) * vSubSamples;

                    edgeList.Add(en);
                }
            }

            points.Clear();

            // now sort the edges by their highest point (should snap to integer, and then by x)
            edgeList.Sort((a, b) => a.y0.CompareTo(b.y0));

            var temp = new Edge();
            temp.y0 = 10000000;
            edgeList.Add(temp);

            // now, traverse the scanlines and find the intersections on each scanline, use xor winding rule
            RasterizeSortedEdges(glyphBitmap, edgeList, vSubSamples, xOff, yOff, color);
        }

        public int GetKerning(char current, char next, float scale)
        {
            return (int)Math.Floor(GetCodepointKernAdvance(current, next) * scale);
        }

        public bool HasGlyph(char codePoint)
        {
            var p = FindGlyphIndex(codePoint);
            return p > 0;
        }

        public async Task<GlyphMetrics> GetGlyphMetrics(char codePoint, float scaleX, float scaleY, float shiftX, float shiftY)
        {
            GlyphMetrics glyphMetrics = new GlyphMetrics();

            if (!HasGlyph(codePoint))
                return glyphMetrics;

            var glyph = FindGlyphIndex(codePoint);

            if (scaleX == 0)
                scaleX = scaleY;

            if (scaleY == 0)
            {
                if (scaleX == 0)
                    throw new Exception("invalid scale");

                scaleY = scaleX;
            }

            int advanceWidth, leftSideBearing;
            GetCodepointHMetrics(codePoint, out advanceWidth, out leftSideBearing);
            glyphMetrics.AdvanceWidth = (int)Math.Ceiling(advanceWidth * scaleX);
            glyphMetrics.LeftSideBearing = (int)Math.Ceiling(leftSideBearing * scaleX);

            int advanceHeight, topSideBearing;
            GetCodepointVMetrics(codePoint, out advanceHeight, out topSideBearing);
            glyphMetrics.AdvanceHeight = (int)Math.Ceiling(advanceHeight * scaleY);
            glyphMetrics.TopSideBearing = (int)Math.Ceiling(topSideBearing * scaleY);

            int ascent, descent, lineGap;
            GetFontVMetrics(out ascent, out descent, out lineGap);

            int ix0 = 0, iy0 = 0, ix1 = 0, iy1 = 0;

            GetGlyphBoundingBox(FindGlyphIndex(codePoint == ' ' ? '_' : codePoint), scaleX, scaleY, shiftX, shiftY, out ix0, out iy0, out ix1, out iy1);

            int x = ix0;
            int y = iy0;
            int w = Math.Max(Math.Abs(ix1 - ix0), 0);
            int h = Math.Max(Math.Abs(iy1 - iy0), 0);

            glyphMetrics.Bounds = IsSVG() ? new RectangleF(0, 0, advanceWidth, ascent - descent) : new RectangleF(x, y, w, h);

            return glyphMetrics;
        }

        public async Task<GlyphBitmap> RenderGlyph(char codePoint, float scale, Color color, Color backgroundColor)
        {
            if (!HasGlyph(codePoint))
                return new GlyphBitmap(0, 0, false, backgroundColor);

            GlyphBitmap glyphBitmap = null;

            if (codePoint == ' ')
            {
                codePoint = '_';
                _ = await GetCodePointBitmap(scale, scale, 0, 0, codePoint, color, backgroundColor);
                glyphBitmap = new GlyphBitmap(4, 4, false, backgroundColor);
            }
            else
            {
                if (char.IsLetter(codePoint))
                    codePoint = char.IsUpper(codePoint) ? char.ToUpperInvariant(codePoint) : char.ToLowerInvariant(codePoint);

                glyphBitmap = await GetCodePointBitmap(scale, scale, 0, 0, codePoint, color, backgroundColor);
            }

            return glyphBitmap;
        }

        public string Name
        {
            get { return _fontName; }
        }

        public string Style
        {
            get { return _fontStyle; }
        }

        public uint UnitsPerEm
        {
            get { return _unitsPerEm; }
        }

        public bool HasSVG()
        {
            return _svgDocuments.Count > 0;
        }

        public bool IsSVG()
        {
            return _svgDocuments.Count > 0;
        }

        public bool IsSVG(char codePoint)
        {
            int glyph = FindGlyphIndex(codePoint);
            return _svgDocuments.ContainsKey(glyph);
        }
    }
}
