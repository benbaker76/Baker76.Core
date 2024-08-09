/*
 * (c) Copyright 2021 by Einar Saukas. All rights reserved.
 *       C# Port 2024 by Ben Baker.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * The name of its author may not be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
 * ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL <COPYRIGHT HOLDER> BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Baker76.Compression
{
    public class Zx0
    {
        private const int FALSE = 0;
        private const int TRUE = 1;
        private const int INITIAL_OFFSET = 1;
        private const int MAX_OFFSET_ZX0 = 32640;
        private const int MAX_OFFSET_ZX7 = 2176;
        private const int MAX_SCALE = 50;
        private const int QTY_BLOCKS = 10000;
        private const int BUFFER_SIZE = 65536;

        private class BLOCK
        {
            public BLOCK Chain;
            public BLOCK GhostChain;
            public int Bits;
            public int Index;
            public int Offset;
            public int Length;
            public int References;
        }

        private static BLOCK[] _deadArray;
        private static int _deadArraySize;
        private static BLOCK _ghostRoot;
        private static int _bitMask;
        private static int _bitValue;
        private static bool _backTrack;
        private static int _lastByte;
        private static int _lastOffset;

        private static BLOCK Allocate(int bits, int index, int offset, int length, BLOCK chain)
        {
            BLOCK ptr;

            if (_ghostRoot != null)
            {
                ptr = _ghostRoot;
                _ghostRoot = ptr.GhostChain;
                if (ptr.Chain != null)
                {
                    if (--ptr.Chain.References == 0)
                    {
                        ptr.Chain.GhostChain = _ghostRoot;
                        _ghostRoot = ptr.Chain;
                    }
                }
            }
            else
            {
                if (_deadArraySize == 0)
                {
                    _deadArray = new BLOCK[QTY_BLOCKS];
                    for (int i = 0; i < QTY_BLOCKS; i++)
                        _deadArray[i] = new BLOCK();
                    _deadArraySize = QTY_BLOCKS;
                }
                ptr = _deadArray[--_deadArraySize];
            }
            ptr.Bits = bits;
            ptr.Index = index;
            ptr.Offset = offset;
            ptr.Length = length;
            if (chain != null)
                chain.References++;
            ptr.Chain = chain;
            ptr.References = 0;
            return ptr;
        }

        private static void Assign(ref BLOCK ptr, BLOCK chain)
        {
            chain.References++;
            if (ptr != null)
            {
                if (--ptr.References == 0)
                {
                    ptr.GhostChain = _ghostRoot;
                    _ghostRoot = ptr;
                }
            }
            ptr = chain;
        }

        private static int OffsetCeiling(int index, int offsetLimit)
        {
            return index > offsetLimit ? offsetLimit : index < INITIAL_OFFSET ? INITIAL_OFFSET : index;
        }

        private static int EliasGammaBits(int value)
        {
            int bits = 1;
            while (value > 1)
            {
                bits += 2;
                value >>= 1;
            }
            return bits;
        }

        private static BLOCK Optimize(byte[] inputData, int inputSize, int skip, int offsetLimit)
        {
            BLOCK[] lastLiteral;
            BLOCK[] lastMatch;
            BLOCK[] optimal;
            int[] matchLength;
            int[] bestLength;
            int bestLengthSize;
            int bits;
            int index;
            int offset;
            int length;
            int bits2;
            int dots = 2;
            int maxOffset = OffsetCeiling(inputSize - 1, offsetLimit);

            lastLiteral = new BLOCK[maxOffset + 1];
            lastMatch = new BLOCK[maxOffset + 1];
            optimal = new BLOCK[inputSize + 1];
            matchLength = new int[maxOffset + 1];
            bestLength = new int[inputSize + 1];

            bestLength[2] = 2;

            Assign(ref lastMatch[INITIAL_OFFSET], Allocate(-1, skip - 1, INITIAL_OFFSET, 0, null));

            Console.Write("[");

            for (index = skip; index < inputSize; index++)
            {
                bestLengthSize = 2;
                maxOffset = OffsetCeiling(index, offsetLimit);
                for (offset = 1; offset <= maxOffset; offset++)
                {
                    if (index != skip && index >= offset && inputData[index] == inputData[index - offset])
                    {
                        if (lastLiteral[offset] != null)
                        {
                            length = index - lastLiteral[offset].Index;
                            bits = lastLiteral[offset].Bits + 1 + EliasGammaBits(length);
                            Assign(ref lastMatch[offset], Allocate(bits, index, offset, length, lastLiteral[offset]));
                            if (optimal[index] == null || optimal[index].Bits > bits)
                                Assign(ref optimal[index], lastMatch[offset]);
                        }
                        if (++matchLength[offset] > 1)
                        {
                            if (bestLengthSize < matchLength[offset])
                            {
                                bits = optimal[index - bestLength[bestLengthSize]].Bits + EliasGammaBits(bestLength[bestLengthSize] - 1);
                                do
                                {
                                    bestLengthSize++;
                                    bits2 = optimal[index - bestLengthSize].Bits + EliasGammaBits(bestLengthSize - 1);
                                    if (bits2 <= bits)
                                    {
                                        bestLength[bestLengthSize] = bestLengthSize;
                                        bits = bits2;
                                    }
                                    else
                                    {
                                        bestLength[bestLengthSize] = bestLength[bestLengthSize - 1];
                                    }
                                } while (bestLengthSize < matchLength[offset]);
                            }
                            length = bestLength[matchLength[offset]];
                            bits = optimal[index - length].Bits + 8 + EliasGammaBits((offset - 1) / 128 + 1) + EliasGammaBits(length - 1);
                            if (lastMatch[offset] == null || lastMatch[offset].Index != index || lastMatch[offset].Bits > bits)
                            {
                                Assign(ref lastMatch[offset], Allocate(bits, index, offset, length, optimal[index - length]));
                                if (optimal[index] == null || optimal[index].Bits > bits)
                                    Assign(ref optimal[index], lastMatch[offset]);
                            }
                        }
                    }
                    else
                    {
                        matchLength[offset] = 0;
                        if (lastMatch[offset] != null)
                        {
                            length = index - lastMatch[offset].Index;
                            bits = lastMatch[offset].Bits + 1 + EliasGammaBits(length) + length * 8;
                            Assign(ref lastLiteral[offset], Allocate(bits, index, 0, length, lastMatch[offset]));
                            if (optimal[index] == null || optimal[index].Bits > bits)
                                Assign(ref optimal[index], lastLiteral[offset]);
                        }
                    }
                }

                if (index * MAX_SCALE / inputSize > dots)
                {
                    Console.Write(".");
                    dots++;
                }
            }

            Console.WriteLine("]");

            return optimal[inputSize - 1];
        }

        private static void Reverse(byte[] data, int first, int last)
        {
            byte temp;
            while (first < last)
            {
                temp = data[first];
                data[first++] = data[last];
                data[last--] = temp;
            }
        }

        private static void ReadBytes(int n, ref int delta)
        {
            delta += n;
        }

        private static void WriteByte(byte[] outputData, ref int outputIndex, int value)
        {
            outputData[outputIndex++] = (byte)value;
        }

        private static void WriteBytes(byte[] outputData, int outputSize, int offset, int length, ref int outputIndex)
        {
            int i;
            if (offset > outputSize + outputIndex)
            {
                throw new Exception("Error: Invalid data");
            }
            while (length-- > 0)
            {
                i = outputIndex - offset;
                WriteByte(outputData, ref outputIndex, outputData[i >= 0 ? i : BUFFER_SIZE + i]);
            }
        }

        private static void WriteBit(byte[] outputData, ref int outputIndex, ref int bitMask, ref int bitIndex, int value)
        {
            if (_backTrack)
            {
                if (value != 0)
                    outputData[outputIndex - 1] |= 1;
                _backTrack = false;
            }
            else
            {
                if (bitMask == 0)
                {
                    bitMask = 0x80;
                    bitIndex = outputIndex;
                    WriteByte(outputData, ref outputIndex, 0);
                }
                if (value != 0)
                    outputData[bitIndex] |= (byte)bitMask;
                bitMask >>= 1;
            }
        }

        private static void WriteInterlacedEliasGamma(byte[] outputData, ref int outputIndex, int value, bool backwardsMode)
        {
            int i;
            for (i = 2; i <= value; i <<= 1) ;
            i >>= 1;
            while ((i >>= 1) > 0)
            {
                WriteBit(outputData, ref outputIndex, ref _bitMask, ref _bitValue, backwardsMode ? TRUE : FALSE);
                WriteBit(outputData, ref outputIndex, ref _bitMask, ref _bitValue, value & i);
            }
            WriteBit(outputData, ref outputIndex, ref _bitMask, ref _bitValue, !backwardsMode ? TRUE : FALSE);
        }

        private static int ReadByte(byte[] inputData, ref int inputIndex)
        {
            _lastByte = inputData[inputIndex++];
            return _lastByte;
        }

        private static int ReadBit(byte[] inputData, ref int inputIndex)
        {
            if (_backTrack)
            {
                _backTrack = false;
                return _lastByte & 1;
            }
            _bitMask >>= 1;
            if (_bitMask == 0)
            {
                _bitMask = 0x80;
                _bitValue = ReadByte(inputData, ref inputIndex);
            }
            return (_bitValue & _bitMask) != 0 ? TRUE : FALSE;
        }

        private static int ReadInterlacedEliasGamma(byte[] inputData, ref int inputIndex)
        {
            int value = 1;

            while (ReadBit(inputData, ref inputIndex) != TRUE)
                value = (value << 1) | ReadBit(inputData, ref inputIndex);

            return value;
        }

        public static byte[] Compress(byte[] inputData, bool quickMode, bool backwardsMode, out int outSize)
        {
            int skip = 0;

            if (backwardsMode)
                Reverse(inputData, 0, inputData.Length - 1);

            BLOCK optimal = Optimize(inputData, inputData.Length, 0, quickMode ? MAX_OFFSET_ZX7 : MAX_OFFSET_ZX0);
            BLOCK next = null, prev = null;
            int lastOffset = INITIAL_OFFSET;
            bool first = true;
            int i;

            outSize = (optimal.Bits + 18 + 7) / 8;
            byte[] outputData = new byte[outSize];

            int delta = 0;

            while (optimal != null)
            {
                prev = optimal.Chain;
                optimal.Chain = next;
                next = optimal;
                optimal = prev;
            }

            int inputIndex = skip;
            int outputIndex = 0;
            _bitMask = 0;

            for (optimal = next.Chain; optimal != null; optimal = optimal.Chain)
            {
                if (optimal.Offset == 0)
                {
                    if (first)
                        first = false;
                    else
                        WriteBit(outputData, ref outputIndex, ref _bitMask, ref _bitValue, FALSE);

                    WriteInterlacedEliasGamma(outputData, ref outputIndex, optimal.Length, backwardsMode);

                    for (i = 0; i < optimal.Length; i++)
                    {
                        WriteByte(outputData, ref outputIndex, inputData[inputIndex]);
                        ReadBytes(1, ref delta);
                        inputIndex++;
                    }
                }
                else if (optimal.Offset == lastOffset)
                {
                    WriteBit(outputData, ref outputIndex, ref _bitMask, ref _bitValue, FALSE);

                    WriteInterlacedEliasGamma(outputData, ref outputIndex, optimal.Length, backwardsMode);
                    ReadBytes(optimal.Length, ref delta);
                    inputIndex += optimal.Length;
                }
                else
                {
                    WriteBit(outputData, ref outputIndex, ref _bitMask, ref _bitValue, TRUE);

                    WriteInterlacedEliasGamma(outputData, ref outputIndex, (optimal.Offset - 1) / 128 + 1, backwardsMode);

                    if (backwardsMode)
                        WriteByte(outputData, ref outputIndex, ((optimal.Offset - 1) % 128) << 1);
                    else
                        WriteByte(outputData, ref outputIndex, (255 - ((optimal.Offset - 1) % 128)) << 1);

                    _backTrack = true;

                    WriteInterlacedEliasGamma(outputData, ref outputIndex, optimal.Length - 1, backwardsMode);
                    ReadBytes(optimal.Length, ref delta);
                    inputIndex += optimal.Length;

                    lastOffset = optimal.Offset;
                }
            }

            WriteBit(outputData, ref outputIndex, ref _bitMask, ref _bitValue, TRUE);
            WriteInterlacedEliasGamma(outputData, ref outputIndex, 256, backwardsMode);

            if (backwardsMode)
                Reverse(outputData, 0, outputData.Length - 1);

            return outputData;
        }

        public static void Decompress(byte[] inData, ref byte[] outData)
        {
            int length;

            int inputIndex = 0;
            int outputIndex = 0;

            _bitMask = 0;
            _backTrack = false;
            _lastOffset = INITIAL_OFFSET;

        COPY_LITERALS:
            length = ReadInterlacedEliasGamma(inData, ref inputIndex);
            for (int i = 0; i < length; i++)
            {
                outData[outputIndex++] = (byte)ReadByte(inData, ref inputIndex);
            }
            if (ReadBit(inData, ref inputIndex) == TRUE)
                goto COPY_FROM_NEW_OFFSET;

        COPY_FROM_LAST_OFFSET:
            length = ReadInterlacedEliasGamma(inData, ref inputIndex);
            WriteBytes(outData, outData.Length, _lastOffset, length, ref outputIndex);
            if (ReadBit(inData, ref inputIndex) == FALSE)
                goto COPY_LITERALS;

        COPY_FROM_NEW_OFFSET:
            _lastOffset = ReadInterlacedEliasGamma(inData, ref inputIndex);
            if (_lastOffset == 256)
                return;
            _lastOffset = ((_lastOffset - 1) << 7) + 128 - (ReadByte(inData, ref inputIndex) >> 1);
            _backTrack = true;
            length = ReadInterlacedEliasGamma(inData, ref inputIndex) + 1;
            WriteBytes(outData, outData.Length, _lastOffset, length, ref outputIndex);
            if (ReadBit(inData, ref inputIndex) == TRUE)
                goto COPY_FROM_NEW_OFFSET;
            else
                goto COPY_LITERALS;
        }
    }
}
