using System;
using System.IO;

namespace Baker76.Compression
{
    public class Rle
    {
        private const int EOF = -1;

        public static int Read(byte[] data, byte[] buffer, int width, int bytes)
        {
            using (MemoryStream ms = new MemoryStream(data))
            {
                return Read(ms, buffer, width, bytes);
            }
        }

        public static int Read(Stream fp, byte[] buffer, int width, int bytes)
        {
            int repeat = 0;
            int direct = 0;
            byte[] sample = new byte[4];
            int head;
            int bufferIndex = 0;

            for (int x = 0; x < width; x++)
            {
                if (repeat == 0 && direct == 0)
                {
                    head = fp.ReadByte();
                    if (head == EOF)
                    {
                        return EOF;
                    }
                    else if (head >= 128)
                    {
                        repeat = head - 127;
                        if (fp.Read(sample, 0, bytes) < bytes)
                        {
                            return EOF;
                        }
                    }
                    else
                    {
                        direct = head + 1;
                    }
                }

                if (repeat > 0)
                {
                    Array.Copy(sample, 0, buffer, bufferIndex, bytes);
                    repeat--;
                }
                else // direct > 0
                {
                    if (fp.Read(buffer, bufferIndex, bytes) < bytes)
                    {
                        return EOF;
                    }
                    direct--;
                }

                bufferIndex += bytes;
            }

            return 0;
        }

        public static byte[] Write(byte[] buffer, int width, int bytes)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                Rle.Write(ms, buffer, width, bytes);

                return ms.ToArray();
            }
        }

        public static void Write(Stream fp, byte[] buffer, int width, int bytes)
        {
            int repeat = 0;
            int direct = 0;
            int fromIndex = 0;
            int bufferIndex = 0;

            for (int x = 1; x < width; ++x)
            {
                if (!buffer.AsSpan(bufferIndex + bytes, bytes).SequenceEqual(buffer.AsSpan(bufferIndex, bytes)))
                {
                    // next pixel is different
                    if (repeat > 0)
                    {
                        fp.WriteByte((byte)(128 + repeat));
                        fp.Write(buffer, fromIndex, bytes);
                        fromIndex = bufferIndex + bytes;
                        repeat = 0;
                        direct = 0;
                    }
                    else
                    {
                        direct += 1;
                    }
                }
                else
                {
                    // next pixel is the same
                    if (direct > 0)
                    {
                        fp.WriteByte((byte)(direct - 1));
                        fp.Write(buffer, fromIndex, bytes * direct);
                        fromIndex = bufferIndex; // point to first identical pixel
                        direct = 0;
                        repeat = 1;
                    }
                    else
                    {
                        repeat += 1;
                    }
                }

                if (repeat == 128)
                {
                    fp.WriteByte(255);
                    fp.Write(buffer, fromIndex, bytes);
                    fromIndex = bufferIndex + bytes;
                    direct = 0;
                    repeat = 0;
                }
                else if (direct == 128)
                {
                    fp.WriteByte(127);
                    fp.Write(buffer, fromIndex, bytes * direct);
                    fromIndex = bufferIndex + bytes;
                    direct = 0;
                    repeat = 0;
                }

                bufferIndex += bytes;
            }

            if (repeat > 0)
            {
                fp.WriteByte((byte)(128 + repeat));
                fp.Write(buffer, fromIndex, bytes);
            }
            else
            {
                fp.WriteByte((byte)direct);
                fp.Write(buffer, fromIndex, bytes * (direct + 1));
            }
        }
    }
}