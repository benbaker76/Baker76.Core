using Baker76.Pngcs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

namespace Baker76.Pngcs.Chunks
{
    /// <summary>
    /// IHDR chunk: http://www.w3.org/TR/PNG/#11IHDR
    /// </summary>
    public class PngChunkIHDR : PngChunkSingle
    {
        public const String ID = ChunkHelper.IHDR;
        public int Cols { get; set; }
        public int Rows { get; set; }
        public int Bitspc { get; set; }
        public int Colormodel { get; set; }
        public int Compmeth { get; set; }
        public int Filmeth { get; set; }
        public int Interlaced { get; set; }

        public PngChunkIHDR(ImageInfo info)
            : base(ID, info)
        {
        }

        public override ChunkOrderingConstraint GetOrderingConstraint()
        {
            return ChunkOrderingConstraint.NA;
        }

        public override ChunkRaw CreateRawChunk()
        {
            ChunkRaw c = new ChunkRaw(13, ChunkHelper.b_IHDR, true);
            int offset = 0;
            Baker76.Pngcs.PngHelperInternal.WriteInt4tobytes(Cols, c.Data, offset);
            offset += 4;
            Baker76.Pngcs.PngHelperInternal.WriteInt4tobytes(Rows, c.Data, offset);
            offset += 4;
            c.Data[offset++] = (byte)Bitspc;
            c.Data[offset++] = (byte)Colormodel;
            c.Data[offset++] = (byte)Compmeth;
            c.Data[offset++] = (byte)Filmeth;
            c.Data[offset++] = (byte)Interlaced;
            return c;
        }

        public override void ParseFromRaw(ChunkRaw c)
        {
            if (c.Len != 13)
                throw new PngjException("Bad IDHR len " + c.Len);
            MemoryStream st = c.GetAsByteStream();
            Cols = Baker76.Pngcs.PngHelperInternal.ReadInt4(st);
            Rows = Baker76.Pngcs.PngHelperInternal.ReadInt4(st);
            // bit depth: number of bits per channel
            Bitspc = Baker76.Pngcs.PngHelperInternal.ReadByte(st);
            Colormodel = Baker76.Pngcs.PngHelperInternal.ReadByte(st);
            Compmeth = Baker76.Pngcs.PngHelperInternal.ReadByte(st);
            Filmeth = Baker76.Pngcs.PngHelperInternal.ReadByte(st);
            Interlaced = Baker76.Pngcs.PngHelperInternal.ReadByte(st);
        }

        public override void CloneDataFromRead(PngChunk other)
        {
            PngChunkIHDR otherx = (PngChunkIHDR)other;
            Cols = otherx.Cols;
            Rows = otherx.Rows;
            Bitspc = otherx.Bitspc;
            Colormodel = otherx.Colormodel;
            Compmeth = otherx.Compmeth;
            Filmeth = otherx.Filmeth;
            Interlaced = otherx.Interlaced;
        }
    }
}
