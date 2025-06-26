using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Baker76.Core.IO;
using Baker76.Imaging;
using JeremyAnsel.ColorQuant;
using Color = Baker76.Imaging.Color;

namespace Baker76.Atlas
{
    public class CrunchOptions
    {
        public bool Recursive = false;
        public string Name = "texture";
        public bool MultiTexture = false;
        public int BitDepth = 8;
        public bool AutoSizeTexture = true;
        public Size TextureSize = new Size(256, 256);
        public Size MinTextureSize = new Size(16, 16);
        public Size MaxTextureSize = new Size(4096, 4096);
        public int TextureFormat = 0;
        public bool AlphaBackground = true;
        public bool ColorBackground = false;
        public bool IndexBackground = true;
        public Color BackColor = Color.FromArgb(unchecked((int)0xFFFF00FF));
        public int BackgroundIndex = 0;
        public bool ReplaceBackgroundColor = true;
        public DistanceType ColorDistance = DistanceType.CIEDE2000;
        public int Spacing = 1;
        public bool FillSpacing = false;
        public bool TrimBackground = true;
        public bool Quantize = false;
        public bool AutoPaletteSlot = true;
        public bool RemapPalette = true;
        public int PaletteSlot = 0;
        public int ColorCount = 16;
        public bool PaletteSlotAddIndex = false;
    }

    public class CrunchInfo
    {
        public uint Crch { get; set; }
        public short Version { get; set; }
        public byte TrimEnabled { get; set; }
        public byte RotateEnabled { get; set; }
        public byte StringType { get; set; }
        public short NumTextures { get; set; }
    }

    public class TextureInfo
    {
        public string Name { get; set; } = string.Empty;
        public short Width { get; set; }
        public short Height { get; set; }
        public short Format { get; set; }
        public short NumImages { get; set; }
        public List<ImageNode> Images { get; set; } = new List<ImageNode>();
    }

    public class Crunch
    {
        private const int CRCH_HEADER_MAGIC = 0x68637263;
        private const int CRCH_HEADER_SIZE = 11;
        private const int TEXTURE_INFO_SIZE = 24;
        private const int IMAGE_INFO_SIZE = 54;

        private static bool IsAtlasDone(List<ImageNode> imageList)
        {
            foreach (ImageNode imageNode in imageList)
            {
                if (!imageNode.Processed)
                    return false;
            }

            return true;
        }

        private static void ResetAtlas(List<ImageNode> imageList)
        {
            foreach (ImageNode imageNode in imageList)
                imageNode.Processed = false;
        }

        private static bool IsAtlasTooSmall(List<ImageNode> imageList, int spacing, Size textureSize)
        {
            foreach (ImageNode imageNode in imageList)
            {
                if (imageNode.Width + spacing > textureSize.Width || imageNode.Height + spacing > textureSize.Height)
                    return true;
            }

            return false;
        }

        public static Image CreateTextureImage(Size textureSize, Palette palette, CrunchOptions options)
        {
            bool isIndexed = options.BitDepth <= 8;
            int bitDepth = System.Math.Max(options.BitDepth, 8);

            if (bitDepth == 16)
                bitDepth = 32;

            Image textureImage = new Image(textureSize.Width, textureSize.Height, bitDepth, isIndexed ? palette : new Palette());

            if (options.ColorBackground)
                textureImage.Clear(options.BackColor);

            if (isIndexed && options.IndexBackground)
            {
                palette.TransparentColor = options.BackgroundIndex;
                textureImage.Clear(options.BackgroundIndex);
            }

            return textureImage;
        }

        public static async Task<List<(TextureNode, Image)>> ParseAtlas(object sender, IList<IFileSource> fileList, Palette palette, CrunchOptions options, Action<object, string, int> progressCallback)
        {
            List<(TextureNode, Image)> textures = new List<(TextureNode, Image)>();

            options.Name = String.IsNullOrEmpty(options.Name) ? "texture" : options.Name;

            if (fileList.Count == 0)
                throw new Exception("No Files Found");

            Size textureSize = (options.AutoSizeTexture ? options.MinTextureSize : options.TextureSize);
            List<ImageNode> imageList = new List<ImageNode>();
            Dictionary<string, List<IFileSource>> imageSequence = new Dictionary<string, List<IFileSource>>();
            int textureCount = 0, imageIndex = 0;
            RectanglePacker rectPacker;
            int index = 0;

            // Stage 1 Process files
            for (int i = 0; i < fileList.Count; i++)
            {
                IFileSource file = fileList[i];
                Match match = Regex.Match(file.Name, @"^(?<name>.+)(?<suffix>\d{2})\.(?<extension>\w+)$");

                progressCallback?.Invoke(sender, $"Loading {file}...", (i + 1) * 100 / fileList.Count);

                if (match.Success)
                {
                    string name = match.Groups["name"].Value;
                    string suffix = match.Groups["suffix"].Value;

                    if (!imageSequence.ContainsKey(name))
                        imageSequence[name] = new List<IFileSource>();

                    imageSequence[name].Add(file);
                }
                else
                {
                    (List<ImageNode> processedImageList, index) = await ImageNode.ProcessFile(sender, index, file, progressCallback);

                    imageList.AddRange(processedImageList);
                }
            }

            (List<ImageNode> sequenceList, index) = await ImageNode.ProcessFiles(sender, index, imageSequence, progressCallback);

            imageList.AddRange(sequenceList);

            if (!options.AutoSizeTexture)
            {
                if (IsAtlasTooSmall(imageList, options.Spacing, textureSize))
                    throw new Exception("Texture Size Too Small");
            }

            imageList.Sort(new ImageNode());
            imageList.Sort(new ImageSizeComparer());

            rectPacker = new RectanglePacker(textureSize.Width, textureSize.Height);
            List<TextureNode> textureList = new List<TextureNode>();
            TextureNode textureNode = null;

            textureList.Add(textureNode = new TextureNode(textureSize.Width, textureSize.Height, options.TextureFormat));

            // Stage 2 Replace background color
            if (options.ReplaceBackgroundColor)
            {
                foreach (ImageNode imageNode in imageList)
                {
                    Image image = imageNode.Image;

                    if (image.BitsPerPixel <= 8)
                    {
                        if (image.Palette.IsTransparent)
                            image.Palette.Colors[image.Palette.TransparentColor] = options.BackColor;
                        else
                            image.Palette.Colors[0] = options.BackColor;
                    }
                }
            }

            // Stage 3 Quantize images
            if (options.Quantize)
            {
                for (int i = 0; i < imageList.Count; i++)
                {
                    ImageNode imageNode = imageList[i];
                    Image image = imageNode.Image;
                    int paletteSlot = options.PaletteSlot;

                    if (options.BitDepth <= 8)
                    {
                        progressCallback?.Invoke(sender, $"Quantizing '{imageNode.Name}'...", (i + 1) * 100 / imageList.Count);

                        if (image.BitsPerPixel <= 8)
                            image = image.ToRGBA();

                        ColorQuantizerOptions colorQuantizerOptions = new ColorQuantizerOptions
                        {
                            Palette = palette.Colors.ToArray(),
                            ColorCountTarget = options.ColorCount,
                            BackgroundIndex = options.BackgroundIndex,
                            PaletteSlot = paletteSlot,
                            AutoPaletteSlot = options.AutoPaletteSlot,
                            PaletteSlotAddIndex = options.PaletteSlotAddIndex,
                            RemapPalette = options.RemapPalette,
                            DistanceType = options.ColorDistance,
                        };

                        WuAlphaColorQuantizer quantizer = new WuAlphaColorQuantizer();
                        ColorQuantizerResult result = await quantizer.QuantizeAsync(image.PixelData, colorQuantizerOptions);

                        if (!options.RemapPalette)
                            palette = new Palette(result.ColorPalette);

                        imageNode.PaletteSlot = options.PaletteSlot;
                        imageNode.Image = new Image(image.Width, image.Height, 8, palette, result.Bytes);

                        await Task.Delay(1);
                    }
                }
            }

            // Stage 4 Pack images
            while (!IsAtlasDone(imageList))
            {
                for (int i = 0; i < imageList.Count; i++)
                {
                    ImageNode imageNode = imageList[i];

                    if (imageNode.Processed)
                        continue;

                    try
                    {
                        progressCallback?.Invoke(sender, $"Packing '{imageNode.Name}'...", (i + 1) * 100 / imageList.Count);

                        Point imagePoint = Point.Empty;
                        Size imageSize = new Size(System.Math.Min(imageNode.Width, textureSize.Width - options.Spacing), System.Math.Min(imageNode.Height, textureSize.Height - options.Spacing));
                        int backgroundIndex = (options.PaletteSlotAddIndex ? options.BackgroundIndex + (imageNode.PaletteSlot * 16) : options.BackgroundIndex);
                        Rectangle trimmedRect = (options.TrimBackground && imageNode.ImageType != ImageType.Aseprite && imageNode.ImageType != ImageType.Sequence ? Baker76.Imaging.Utility.GetTrimmedBackgroundRect(imageNode.Image, backgroundIndex) : new Rectangle(0, 0, imageNode.Width, imageNode.Height));
                        Rectangle frameRect = new Rectangle(-trimmedRect.X, -trimmedRect.Y, imageSize.Width, imageSize.Height);
                        imageSize = new Size(trimmedRect.Width, trimmedRect.Height);

                        if (rectPacker.FindPoint(new Size(imageSize.Width + options.Spacing, imageSize.Height + options.Spacing), ref imagePoint))
                        {
                            int halfSpacing = options.Spacing / 2;
                            Point imageOffset = new Point(imagePoint.X + halfSpacing, imagePoint.Y + halfSpacing);

                            imageNode.Index = imageIndex;
                            imageNode.TextureNode = textureNode;
                            imageNode.X = imageOffset.X;
                            imageNode.Y = imageOffset.Y;
                            imageNode.Width = imageSize.Width;
                            imageNode.Height = imageSize.Height;
                            imageNode.FrameX = -frameRect.X;
                            imageNode.FrameY = -frameRect.Y;
                            imageNode.FrameWidth = frameRect.Width;
                            imageNode.FrameHeight = frameRect.Height;
                            imageNode.Processed = true;

                            textureNode.Images.Add(imageNode);

                            imageIndex++;
                        }
                    }
                    catch
                    {
                        imageNode.Processed = true;
                    }
                }

                if (IsAtlasDone(imageList))
                    break;

                if (options.AutoSizeTexture)
                {
                    ResetAtlas(textureNode.Images);

                    textureSize.Width <<= 1;
                    textureSize.Height <<= 1;

                    if (textureSize.Width > options.MaxTextureSize.Width || textureSize.Height > options.MaxTextureSize.Height)
                        throw new Exception("Texture Size Too Large");

                    rectPacker = new RectanglePacker(textureSize.Width, textureSize.Height);

                    textureNode.Width = textureSize.Width;
                    textureNode.Height = textureSize.Height;
                    textureNode.Images.Clear();

                    continue;
                }

                rectPacker = new RectanglePacker(textureSize.Width, textureSize.Height);

                textureList.Add(textureNode = new TextureNode(textureSize.Width, textureSize.Height, options.TextureFormat));

                textureCount++;
            }

            // Stage 5 Write images
            for (int i = 0; i < textureList.Count; i++)
            {
                TextureNode textureNode_ = textureList[i];
                Image textureImage = CreateTextureImage(textureSize, palette, options);

                textureNode.Images.Sort(new ImageNode());

                for (int j = 0; j < textureNode_.Images.Count; j++)
                {
                    ImageNode imageNode = textureNode_.Images[j];
                    Image image = imageNode.Image;
                    Point imagePoint = new Point(imageNode.X, imageNode.Y);
                    Size imageSize = new Size(imageNode.Width, imageNode.Height);
                    Rectangle trimmedRect = new Rectangle(imageNode.FrameX, imageNode.FrameY, imageSize.Width, imageSize.Height);

                    progressCallback?.Invoke(sender, $"Drawing '{imageNode.Name}'...", (j + 1) * 100 / textureNode_.Images.Count);

                    textureImage.DrawImage(image, new Rectangle(imagePoint, imageSize), trimmedRect);

                    if (options.FillSpacing)
                        Baker76.Imaging.Utility.FillSpacing(textureImage, image, imagePoint, imageSize, options.Spacing);
                }

                textureNode.Name = options.Name + (options.MultiTexture && textureList.Count > 1 ? textureList.IndexOf(textureNode_).ToString("00") : String.Empty);

                textures.Add((textureNode, textureImage));
            }

            progressCallback?.Invoke(sender, "Done!", 100);

            return textures;
        }

        public static async Task<List<TextureInfo>> ReadBin(Stream stream, string fileName, Palette palette)
        {
            string? path = Path.GetDirectoryName(fileName) ?? "";
            List<TextureInfo> textures = new List<TextureInfo>();

            // Read TextureAtlas
            byte[] buffer = new byte[11];
            await stream.ReadAsync(buffer, 0, buffer.Length);

            CrunchInfo crunchInfo = new CrunchInfo
            {
                Crch = BitConverter.ToUInt32(buffer, 0),
                Version = BitConverter.ToInt16(buffer, 4),
                TrimEnabled = buffer[6],
                RotateEnabled = buffer[7],
                StringType = buffer[8],
                NumTextures = BitConverter.ToInt16(buffer, 9)
            };

            /* Console.WriteLine("Crch: 0x" + textureAtlas.Crch.ToString("X"));
            Console.WriteLine("Version: " + textureAtlas.Version);
            Console.WriteLine("Trim: " + textureAtlas.TrimEnabled);
            Console.WriteLine("Rotate: " + textureAtlas.RotateEnabled);
            Console.WriteLine("StringType: " + textureAtlas.StringType);
            Console.WriteLine("NumTextures: " + textureAtlas.NumTextures); */

            if (crunchInfo.Crch != CRCH_HEADER_MAGIC)
            {
                Console.WriteLine("Invalid Texture Atlas Header!");
                return null;
            }

            for (int i = 0; i < crunchInfo.NumTextures; i++)
            {
                // Read TextureInfo
                buffer = new byte[24];
                await stream.ReadAsync(buffer, 0, buffer.Length);

                TextureInfo textureInfo = new TextureInfo
                {
                    Name = System.Text.Encoding.ASCII.GetString(buffer, 0, 16).TrimEnd('\0'),
                    Width = BitConverter.ToInt16(buffer, 16),
                    Height = BitConverter.ToInt16(buffer, 18),
                    Format = BitConverter.ToInt16(buffer, 20),
                    NumImages = BitConverter.ToInt16(buffer, 22)
                };

                /* Console.WriteLine("\nTexture " + (i + 1) + ":");
                Console.WriteLine("Name: " + new string(textureInfo.Name));
                Console.WriteLine("Width: " + textureInfo.Width);
                Console.WriteLine("Height: " + textureInfo.Height);
                Console.WriteLine("Format: " + textureInfo.Format);
                Console.WriteLine("NumImages: " + textureInfo.NumImages); */

                for (int j = 0; j < textureInfo.NumImages; j++)
                {
                    // Read ImageInfo
                    buffer = new byte[54];
                    await stream.ReadAsync(buffer, 0, buffer.Length);

                    ImageNode imageNode = new ImageNode
                    {
                        FrameIndex = BitConverter.ToInt16(buffer, 0),
                        Name = System.Text.Encoding.ASCII.GetString(buffer, 2, 16).TrimEnd('\0'),
                        Label = System.Text.Encoding.ASCII.GetString(buffer, 18, 16).TrimEnd('\0'),
                        LoopDirection = buffer[34],
                        Duration = BitConverter.ToInt16(buffer, 35),
                        X = BitConverter.ToInt16(buffer, 37),
                        Y = BitConverter.ToInt16(buffer, 39),
                        Width = BitConverter.ToInt16(buffer, 41),
                        Height = BitConverter.ToInt16(buffer, 43),
                        FrameX = BitConverter.ToInt16(buffer, 45),
                        FrameY = BitConverter.ToInt16(buffer, 47),
                        FrameWidth = BitConverter.ToInt16(buffer, 49),
                        FrameHeight = BitConverter.ToInt16(buffer, 51),
                        PaletteSlot = buffer[53]
                    };

                    /* Console.WriteLine("\nImage " + (j + 1) + ":");
                    Console.WriteLine("Name: " + new string(imageNode.Name));
                    Console.WriteLine("X: " + imageNode.X);
                    Console.WriteLine("Y: " + imageNode.Y);
                    Console.WriteLine("Width: " + imageNode.Width);
                    Console.WriteLine("Height: " + imageNode.Height);
                    Console.WriteLine("FrameX: " + imageNode.FrameX);
                    Console.WriteLine("FrameY: " + imageNode.FrameY);
                    Console.WriteLine("FrameWidth: " + imageNode.FrameWidth);
                    Console.WriteLine("FrameHeight: " + imageNode.FrameHeight);
                    Console.WriteLine("PaletteSlot: " + imageNode.PaletteSlot); */

                    textureInfo.Images.Add(imageNode);
                }

                textures.Add(textureInfo);

                break;
            }

            return textures;
        }

        public static void WriteBin(string fileName, List<TextureNode> textureList, bool trimBackground = true)
        {
            using (FileStream stream = new FileStream(fileName, FileMode.Create, FileAccess.Write))
            {
                WriteBin(stream, textureList, trimBackground);
            }
        }

        public static void WriteBin(Stream stream, List<TextureNode> textureList, bool trimBackground = true)
        {
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                writer.Write(CRCH_HEADER_MAGIC);
                writer.Write((ushort)0);
                writer.Write((byte)(trimBackground ? 1 : 0));
                writer.Write((byte)0);
                writer.Write((byte)3);
                writer.Write((ushort)1);

                foreach (TextureNode textureNode in textureList)
                {
                    writer.Write(Encoding.ASCII.GetBytes(textureNode.Name.PadRight(16, '\0')));
                    writer.Write((ushort)textureNode.Width);
                    writer.Write((ushort)textureNode.Height);
                    writer.Write((ushort)textureNode.Format);
                    writer.Write((ushort)textureNode.Images.Count);

                    foreach (ImageNode imageNode in textureNode.Images)
                    {
                        writer.Write((ushort)imageNode.FrameIndex);
                        writer.Write(Encoding.ASCII.GetBytes(imageNode.Name.PadRight(16, '\0')));
                        writer.Write(Encoding.ASCII.GetBytes(imageNode.Label.PadRight(16, '\0')));
                        writer.Write((byte)imageNode.LoopDirection);
                        writer.Write((ushort)imageNode.Duration);
                        writer.Write((ushort)imageNode.X);
                        writer.Write((ushort)imageNode.Y);
                        writer.Write((ushort)imageNode.Width);
                        writer.Write((ushort)imageNode.Height);
                        if (trimBackground)
                        {
                            writer.Write((ushort)imageNode.FrameX);
                            writer.Write((ushort)imageNode.FrameY);
                            writer.Write((ushort)imageNode.FrameWidth);
                            writer.Write((ushort)imageNode.FrameHeight);
                        }
                        writer.Write((byte)imageNode.PaletteSlot);
                    }
                }
            }
        }

        public static void WriteJson(Stream stream, List<TextureNode> textureList)
        {
            using (StreamWriter writer = new StreamWriter(stream))
            {
                writer.WriteLine("{");
                writer.WriteLine("\t\"trim\":true,");
                writer.WriteLine("\t\"rotate\":false,");
                writer.WriteLine("\t\"textures\":[");

                foreach (TextureNode textureNode in textureList)
                {
                    writer.WriteLine("\t\t{");
                    writer.WriteLine($"\t\t\t\"name\":\"{textureNode.Name}\",");
                    writer.WriteLine($"\t\t\t\"width\":{textureNode.Width},");
                    writer.WriteLine($"\t\t\t\"height\":{textureNode.Height},");
                    writer.WriteLine($"\t\t\t\"format\":\"{textureNode.Format}\",");
                    writer.WriteLine("\t\t\t\"images\":[");

                    foreach (ImageNode imageNode in textureNode.Images)
                    {
                        writer.WriteLine("\t\t\t\t{");
                        writer.WriteLine($"\t\t\t\t\t\"fi\":{imageNode.FrameIndex},");
                        writer.WriteLine($"\t\t\t\t\t\"n\":\"{imageNode.Name}\",");
                        writer.WriteLine($"\t\t\t\t\t\"l\":\"{imageNode.Label}\",");
                        writer.WriteLine($"\t\t\t\t\t\"ld\":{imageNode.LoopDirection},");
                        writer.WriteLine($"\t\t\t\t\t\"d\":{imageNode.Duration},");
                        writer.WriteLine($"\t\t\t\t\t\"x\":{imageNode.X},");
                        writer.WriteLine($"\t\t\t\t\t\"y\":{imageNode.Y},");
                        writer.WriteLine($"\t\t\t\t\t\"w\":{imageNode.Width},");
                        writer.WriteLine($"\t\t\t\t\t\"h\":{imageNode.Height},");
                        writer.WriteLine($"\t\t\t\t\t\"fx\":{imageNode.FrameX},");
                        writer.WriteLine($"\t\t\t\t\t\"fy\":{imageNode.FrameY},");
                        writer.WriteLine($"\t\t\t\t\t\"fw\":{imageNode.FrameWidth},");
                        writer.WriteLine($"\t\t\t\t\t\"fh\":{imageNode.FrameHeight},");
                        writer.WriteLine($"\t\t\t\t\t\"ps\":{imageNode.PaletteSlot}");
                        writer.WriteLine("\t\t\t\t},");
                    }

                    writer.WriteLine("\t\t\t]");
                    writer.Write("\t\t}");
                    if (textureNode != textureList.Last())
                    {
                        writer.Write(",");
                    }
                    writer.WriteLine();
                }

                writer.WriteLine("\t]");
                writer.WriteLine("}");
            }
        }
    }
}
