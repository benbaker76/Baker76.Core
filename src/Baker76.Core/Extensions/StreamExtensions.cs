using System;
using System.IO;
using System.Threading.Tasks;

namespace Baker76.Core.Extensions
{
    public static class StreamExtensions
    {
        public static async Task<byte[]> ReadBytes(this Stream stream, int count)
        {
            var data = new byte[count];
            var bytesRead = await stream.ReadAsync(data, 0, count);
            if (bytesRead == 0)
            {
                return Array.Empty<byte>();
            }

            if (bytesRead >= count)
            {
                return data;
            }

            var partialData = new byte[bytesRead];
            Array.Copy(data, 0, partialData, 0, bytesRead);
            return partialData;
        }

        public static async Task WriteByte(this Stream stream, byte value)
        {
            var data = new[] { value };
            await stream.WriteAsync(data, 0, data.Length);
        }

        public static async Task WriteBytes(this Stream stream, byte[] data)
        {
            await stream.WriteAsync(data, 0, data.Length);
        }

    
        /// <summary>
        /// Find first occurence of byte pattern in stream
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="pattern"></param>
        /// <returns></returns>
        public static async Task<long> Find(this Stream stream, byte[] pattern)
        {
            var chunkSize = 32768;
            byte[] chunk;
            do
            {
                var position = stream.Position;
                chunk = await stream.ReadBytes(chunkSize);

                if (chunk.Length == 0)
                {
                    break;
                }

                for (var i = 0; i < chunk.Length; i++)
                {
                    // skip, if first byte is not equal
                    if (chunk[i] != pattern[0])
                    {
                        continue;
                    }

                    // found a match on first byte, now try to match rest of the pattern
                    var patternIndex = 1;
                    for (var j = 1; j < pattern.Length; j++, patternIndex++)
                    {
                        if (j + i >= chunk.Length)
                        {
                            chunk = await stream.ReadBytes(chunkSize);
                            i = 0;
                            patternIndex = 0;
                        }

                        if (chunk[i + patternIndex] != pattern[j])
                        {
                            break;
                        }

                        if (j == pattern.Length - 1)
                        {
                            return position + i;
                        }
                    }
                }

            } while (chunk.Length == chunkSize);

            return -1;
        }
    }
}
