using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Baker76.Core.IO;
using Baker76.Imaging;
using Image = Baker76.Imaging.Image;
using Color = Baker76.Imaging.Color;
using Baker76.Imaging.Pngcs;
using Baker76.Imaging;

namespace Baker76.Atlas
{
    public class ImageNode : IComparer<ImageNode>
    {
        public int Index = -1;
        public string FileName = String.Empty;
        public int FrameIndex = 0;
        public string Name = String.Empty;
        public string Label = String.Empty;
        public int LoopDirection = 0;
        public int Duration = 0;
        public int X = 0;
        public int Y = 0;
        public int Width = 0;
        public int Height = 0;
        public int FrameX = 0;
        public int FrameY = 0;
        public int FrameWidth = 0;
        public int FrameHeight = 0;
        public int PaletteSlot = -1;
        public int ClosestPaletteSlot = -1;
        public int PaletteSlotCount = 0;
        public TextureNode TextureNode = null;
        public Image Image = null;
        public Color[] Palette = null;
        public ImageType ImageType = ImageType.Png;
        public bool Processed = false;
        public bool NoFit = false;

        public Rectangle Rect { get { return new Rectangle(X, Y, Width, Height); } }
        public RectangleF RectF { get { return new RectangleF((float)X / TextureNode.Width, (float)Y / TextureNode.Height, (float)Width / TextureNode.Width, (float)Height / TextureNode.Height); } }

        public ImageNode()
        {
        }

        public ImageNode(int index, string fileName)
        {
            Index = index;
            FileName = fileName;

            Size size = Baker76.Imaging.Utility.GetImageSize(fileName);
            Width = size.Width;
            Height = size.Height;
        }

        public ImageNode(int index, string fileName, int x, int y, int paletteSlot)
        {
            Index = index;
            FileName = fileName;
            X = x;
            Y = y;
            PaletteSlot = paletteSlot;

            Size size = Baker76.Imaging.Utility.GetImageSize(fileName);
            Width = size.Width;
            Height = size.Height;
        }

        public ImageNode(string name, int x, int y, int width, int height)
        {
            Name = name;
            X = x;
            Y = y;
            Width = width;
            Height = height;
            FrameX = 0;
            FrameY = 0;
            FrameWidth = width;
            FrameHeight = height;
        }

        public ImageNode(TextureNode textureNode)
        {
            TextureNode = textureNode;
            Name = textureNode.Name;
            Width = textureNode.Width;
            Height = textureNode.Height;
        }

        public ImageNode(int index, string name, Image image, ImageType imageType)
        {
            Index = index;
            Name = name;
            Width = image.Width;
            Height = image.Height;
            Image = image;
            ImageType = imageType;
        }

        public ImageNode(TextureNode textureNode, int index, int frameIndex, string name, string label, int loopDirection, int duration, int x, int y, int width, int height, int frameX, int frameY, int frameWidth, int frameHeight)
        {
            TextureNode = textureNode;
            Index = index;
            FrameIndex = frameIndex;
            Name = name;
            Label = label;
            LoopDirection = loopDirection;
            Duration = duration;
            X = x;
            Y = y;
            Width = width;
            Height = height;
            FrameX = frameX;
            FrameY = frameY;
            FrameWidth = frameWidth;
            FrameHeight = frameHeight;
        }

        public ImageNode(int index, int frameIndex, string name, string label, int loopDirection, int duration, Image image, ImageType imageType)
        {
            Index = index;
            FrameIndex = frameIndex;
            Name = name;
            Width = image.Width;
            Height = image.Height;
            Label = label;
            LoopDirection = loopDirection;
            Duration = duration;
            Image = image;
            ImageType = imageType;
        }

        public static async Task<(List<ImageNode>, int)> ProcessFile(object sender, int index, IFileSource file, Action<object, string, int> progressCallback)
        {
            List<ImageNode> imageList = new List<ImageNode>();
            string name = Path.GetFileNameWithoutExtension(file.Name);
            string extension = Path.GetExtension(file.Name).ToLower();

            progressCallback?.Invoke(sender, $"Loading '{name}'", 0);

            using (var memoryStream = new MemoryStream())
            {
                await file.OpenReadStream(1024 * 1024 * 1024).CopyToAsync(memoryStream);

                memoryStream.Position = 0;

                if (extension == ".ase" || extension == ".aseprite")
                {
                    //Aseprite sprite = await Aseprite.LoadAsepriteAsync(memoryStream);
                    Aseprite sprite = new Aseprite(memoryStream);

                    for (int i = 0; i < sprite.Frames.Count; i++)
                    {
                        Aseprite.Frame frame = sprite.Frames[i];

                        progressCallback?.Invoke(sender, $"Loading '{name}' ({(i + 1)})", (i + 1) * 100 / sprite.Frames.Count);

                        var firstTagForFrame = sprite.Tags.FirstOrDefault(tag => tag.From <= i && tag.To >= i);
                        string label = String.Empty;
                        int loopDirection = 0;

                        if (firstTagForFrame != null)
                        {
                            label = firstTagForFrame.Name ?? "";
                            loopDirection = (int)firstTagForFrame.LoopDirection;
                        }

                        imageList.Add(new ImageNode(index++, i + 1, name, label, loopDirection, frame.Duration, frame.Image, ImageType.Aseprite));
                    }
                }
                else if (extension == ".png" || extension == ".bmp")
                {
                    Image image = (extension == ".png" ? await PngReader.ReadAsync(memoryStream) : await Bitmap.ReadAsync(memoryStream));

                    imageList.Add(new ImageNode(index++, name, image, ImageType.Png));
                }
            }

            return (imageList, index);
        }

        public static async Task<(List<ImageNode>, int)> ProcessFiles(object sender, int index, Dictionary<string, List<IFileSource>> imageSequence, Action<object, string, int> progressCallback)
        {
            List<ImageNode> imageList = new List<ImageNode>();

            foreach (var sequence in imageSequence)
            {
                string name = sequence.Key;
                List<IFileSource> sequenceFiles = sequence.Value;

                //sequenceFiles.Sort();

                progressCallback?.Invoke(sender, $"Loading '{name}'...", 0);

                for (int i = 0; i < sequenceFiles.Count; i++)
                {
                    IFileSource file = sequenceFiles[i];

                    progressCallback?.Invoke(sender, $"Loading '{file.Name}'...", (i + 1) * 100 / sequenceFiles.Count);

                    using (var memoryStream = new MemoryStream())
                    {
                        await file.OpenReadStream(1024 * 1024 * 1024).CopyToAsync(memoryStream);

                        memoryStream.Position = 0;

                        string extension = Path.GetExtension(file.Name).ToLower();
                        Image image = (extension == ".png" ? await PngReader.ReadAsync(memoryStream) : await Bitmap.ReadAsync(memoryStream));

                        imageList.Add(new ImageNode(index++, i + 1, name, "Frame " + (i + 1), 0, 100, image, ImageType.Sequence));
                    }
                }
            }

            return (imageList, index);
        }

        #region IComparer<ImageNode> Members

        public int Compare(ImageNode x, ImageNode y)
        {
            if (x.Name != y.Name)
                return x.Name.CompareTo(y.Name);

            if (x.Label != y.Label)
                return x.Label.CompareTo(y.Label);

            return x.FrameIndex.CompareTo(y.FrameIndex);
        }

        #endregion
    }

    public enum ImageType
    {
        Png,
        Aseprite,
        Sequence
    };

    public class ImageSizeComparer : IComparer<ImageNode>
    {
        public int Compare(ImageNode x, ImageNode y)
        {
            int areaX = x.Width * x.Height;
            int areaY = y.Width * y.Height;

            return -areaX.CompareTo(areaY);
        }
    }

    public class PaletteLengthComparer : IComparer<ImageNode>
    {
        public int Compare(ImageNode x, ImageNode y)
        {
            return x.Image.Palette.Colors.Count.CompareTo(y.Image.Palette.Colors.Count);
        }
    }

    public class ClosestPaletteSlotComparer : IComparer<ImageNode>
    {
        public int Compare(ImageNode x, ImageNode y)
        {
            return x.ClosestPaletteSlot.CompareTo(y.ClosestPaletteSlot);
        }
    }

    public class PaletteSlotCountComparer : IComparer<ImageNode>
    {
        public int Compare(ImageNode x, ImageNode y)
        {
            return x.PaletteSlotCount.CompareTo(y.PaletteSlotCount);
        }
    }
}
