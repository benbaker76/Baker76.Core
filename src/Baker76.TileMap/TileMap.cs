using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using static Baker76.Imaging.Aseprite.Layer;
using Baker76.Imaging;
using Baker76.Compression;
using Baker76.Plugin;
using Image=Baker76.Imaging.Image;
using Color=Baker76.Imaging.Color;

namespace Baker76.TileMap
{
    public class TileMapOptions
    {
        public int BlankTileId = 0;
        public bool Extended512 = false;
        public bool CompressZx0 = false;
        public bool QuickMode = false;
        public bool BackwardsMode = false;
        public bool CompressRLE = false;
        public bool Header = false;
        public bool Split = false;
    }

    public class TileMapSliceOptions
    {
        public int TileWidth = 0;
        public int TileHeight = 0;
        public int TileSetWidth = 0;
        public bool NoRepeat = true;
        public bool NoMirror = true;
        public bool NoRotate = false;
        public bool InsertBlankTile = true;
        public int ClearMap = -1;
        public TileLayerAttributes TileLayerAttributes = TileLayerAttributes.Extended512;
    }

    public class TileNode
    {
        public int Id;
        public TileAttributes Attributes;

        public TileNode(int id, TileAttributes attributes)
        {
            Id = id;
            Attributes = attributes;
        }

        public ushort ToUShort()
        {
            return (ushort)(((ushort)Attributes << 8) | (ushort)Id);
        }
    }

    public class TiledNode
    {
        public int Id;
        public TiledAttributes Attributes;

        public TiledNode(int id, TiledAttributes attributes)
        {
            Id = id;
            Attributes = attributes;
        }

        public long ToLong()
        {
            return (long)((long)Attributes << 28) | ((long)Id + 1);
        }
    }

    [Flags]
    public enum TileLayerAttributes : byte
    {
        None = 0,
        Extended512 = (1 << 1),
        CompressedZx0 = (1 << 2),
        QuickMode = (1 << 3),
        BackwardsMode = (1 << 4),
        CompressedRLE = (1 << 5),
        Split = (1 << 6),
    };

    [Flags]
    public enum TileAttributes : byte
    {
        None = 0,
        Rotate = (1 << 1),
        MirrorY = (1 << 2),
        MirrorX = (1 << 3),
        MirrorX_Y = MirrorX | MirrorY,
    };

    [Flags]
    public enum TiledAttributes : byte
    {
        None = 0,
        AntiDiagonal = (1 << 1),
        Vertical = (1 << 2),
        Horizontal = (1 << 3),
        Horizontal_Vertical = Horizontal | Vertical,
    };

    public class TileHeader
    {
        public uint Header;
        public byte Version;
        public byte NumLayers;
    }

    public class TileLayer
    {
        public byte Id;
        public string TileSet;
        public TileLayerAttributes Attributes;
        public ushort Width;
        public ushort Height;
        public byte TileWidth;
        public byte TileHeight;
        public ushort DataLength;
    }

    public class TileMap
    {
        public const uint TILE_HEADER_MAGIC = 0x0070616D;
        public const int TILE_HEADER_SIZE = 6;
        public const int TILE_LAYER_SIZE = 26;

        public string Name;
        public ushort MapWidth;
        public ushort MapHeight;
        public byte TileWidth;
        public byte TileHeight;
        public TileLayerAttributes Attributes;
        public ushort[] TileData;
        public string TileSet;

        public TileMap(string name, ushort mapWidth, ushort mapHeight, byte tileWidth, byte tileHeight, TileLayerAttributes attributes, ushort[] tileData, string tileSet)
        {
            Name = name;
            MapWidth = mapWidth;
            MapHeight = mapHeight;
            TileWidth = tileWidth;
            TileHeight = tileHeight;
            Attributes = attributes;
            TileData = tileData;
            TileSet = tileSet;
        }

        public static TileMap FromTileLayer(string fileName, TileLayer tileLayer, ushort[] tileData)
        {
            string suffix = tileLayer.Id > 1 ? "_" + tileLayer.Id.ToString() : "";
            string name = Path.GetFileNameWithoutExtension(fileName) + suffix;

            return new TileMap(name, tileLayer.Width, tileLayer.Height, tileLayer.TileWidth, tileLayer.TileHeight, tileLayer.Attributes, tileData, tileLayer.TileSet);
        }

        public static TileLayer ToTileLayer(TileMap tileMap, int layerId)
        {
            TileLayer tileLayer = new TileLayer
            {
                Id = (byte)layerId,
                TileSet = tileMap.TileSet,
                Width = tileMap.MapWidth,
                Height = tileMap.MapHeight,
                TileWidth = tileMap.TileWidth,
                TileHeight = tileMap.TileHeight,
                Attributes = tileMap.Attributes,
                DataLength = (ushort)(tileMap.TileData.Length * 2)
            };

            return tileLayer;
        }

        public static async Task<List<TileMap>> LoadTileMap(Stream stream, string fileName)
        {
            List<TileMap> tileMaps = new List<TileMap>();

            // Read TileHeader
            byte[] headerBuffer = new byte[TILE_HEADER_SIZE];
            await stream.ReadAsync(headerBuffer, 0, headerBuffer.Length);

            TileHeader tileHeader = new TileHeader
            {
                Header = BitConverter.ToUInt32(headerBuffer, 0),
                Version = headerBuffer[4],
                NumLayers = headerBuffer[5]
            };

            if (tileHeader.Header != TILE_HEADER_MAGIC)
            {
                Console.WriteLine("Invalid Tile Map Header!");
                return null;
            }

            for (int i = 0; i < tileHeader.NumLayers; i++)
            {
                // Read TileLayer
                byte[] layerBuffer = new byte[TILE_LAYER_SIZE];
                await stream.ReadAsync(layerBuffer, 0, layerBuffer.Length);

                TileLayer tileLayer = new TileLayer
                {
                    Id = layerBuffer[0],
                    TileSet = System.Text.Encoding.ASCII.GetString(layerBuffer, 1, 16).TrimEnd('\0'),
                    Attributes = (TileLayerAttributes)layerBuffer[17],
                    Width = BitConverter.ToUInt16(layerBuffer, 18),
                    Height = BitConverter.ToUInt16(layerBuffer, 20),
                    TileWidth = layerBuffer[22],
                    TileHeight = layerBuffer[23],
                    DataLength = BitConverter.ToUInt16(layerBuffer, 24)
                };

                // Prepare destination data buffer
                byte[] dstData = new byte[tileLayer.Width * tileLayer.Height * 2];

                // Read compressed or uncompressed data based on attributes
                if (tileLayer.Attributes.HasFlag(TileLayerAttributes.CompressedZx0))
                {
                    byte[] srcData = new byte[tileLayer.DataLength];
                    await stream.ReadAsync(srcData, 0, tileLayer.DataLength);
                    Zx0.Decompress(srcData, ref dstData);
                }
                else if (tileLayer.Attributes.HasFlag(TileLayerAttributes.CompressedRLE))
                {
                    byte[] srcData = new byte[tileLayer.DataLength];
                    await stream.ReadAsync(srcData, 0, tileLayer.DataLength);
                    int length = Rle.Read(srcData, dstData, dstData.Length, 1);
                }
                else
                {
                    await stream.ReadAsync(dstData, 0, tileLayer.DataLength);
                }

                // Process tile data
                int halfLength = dstData.Length / 2;
                ushort[] tileData = new ushort[halfLength];

                if (tileLayer.Attributes.HasFlag(TileLayerAttributes.Split))
                {
                    for (int j = 0; j < tileData.Length; j++)
                        tileData[j] = (ushort)((dstData[tileData.Length + j] << 8) | dstData[j]);
                }
                else
                {
                    for (int j = 0; j < tileData.Length; j++)
                        tileData[j] = (ushort)((dstData[j * 2 + 1] << 8) | dstData[j * 2]);
                }

                // Create and add TileMap
                TileMap tileMap = TileMap.FromTileLayer(fileName, tileLayer, tileData);
                tileMaps.Add(tileMap);
            }

            return tileMaps;
        }

        public static async Task ParseTmx(Stream stream, IFileSource file, TileMapOptions tileMapOptions)
        {
            string fileName = file.Name;

            using (var memoryStream = new MemoryStream())
            {
                await file.OpenReadStream(1024 * 1024 * 1024).CopyToAsync(memoryStream);

                memoryStream.Position = 0;

                (List<TileLayer> tileLayers, List<byte[]> byteList) = await ParseTmx(memoryStream, fileName, tileMapOptions);

                await WriteBin(stream, fileName, tileLayers, byteList, tileMapOptions.Header, 0);
            }
        }

        public static async Task<(List<TileLayer>, List<byte[]>)> ParseTmx(Stream stream, string fileName, TileMapOptions options)
        {
            List<TileLayer> tileLayers = new List<TileLayer>();
            List<byte[]> byteList = new List<byte[]>();

            string extension = Path.GetExtension(fileName).ToLower();
            string tileSet = "";
            int firstGid = 1;
            string layerId = "1";
            int tileWidth = 8, tileHeight = 8;
            int width = 0, height = 0;
            bool over256 = false;
            bool over512 = false;
            long[] tileData;

            if (extension == ".tmx")
            {
                XmlDocument xmlDocument = new XmlDocument();
                xmlDocument.Load(stream);

                XmlNodeList tilesetNode = xmlDocument.GetElementsByTagName("tileset");

                firstGid = int.Parse(tilesetNode[0].Attributes["firstgid"].Value);

                if (tilesetNode[0].Attributes["source"] != null)
                {
                    tileSet = tilesetNode[0].Attributes["source"].Value;
                }
                else
                {
                    XmlNode imageNode = tilesetNode[0].ChildNodes[0];

                    tileSet = imageNode.Attributes["source"].Value;
                }

                XmlNodeList mapNode = xmlDocument.GetElementsByTagName("map");

                tileWidth = int.Parse(mapNode[0].Attributes["tilewidth"].Value);
                tileHeight = int.Parse(mapNode[0].Attributes["tileheight"].Value);

                XmlNodeList layerNode = xmlDocument.GetElementsByTagName("layer");

                layerId = layerNode[0].Attributes["id"].Value;
                width = int.Parse(layerNode[0].Attributes["width"].Value);
                height = int.Parse(layerNode[0].Attributes["height"].Value);

                XmlNode dataNode = layerNode[0].ChildNodes[0];

                string encoding = dataNode.Attributes["encoding"].Value.ToLower();
                string dataText = dataNode.InnerText;

                tileData = Array.ConvertAll(dataText.Split(','), long.Parse);
            }
            else if (extension == ".csv")
            {
                string[] linesArray;

                using (StreamReader reader = new StreamReader(stream))
                    linesArray = reader.ReadToEnd().Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

                width = linesArray[0].Split(",").Length;
                height = linesArray.Length;

                string dataString = System.String.Join(",", linesArray);
                tileData = Array.ConvertAll(dataString.Split(','), long.Parse);
            }
            else
                return (tileLayers, byteList);

            List<byte> tileBytes = new List<byte>();
            List<byte> attribBytes = new List<byte>();

            for (int i = 0; i < tileData.Length; i++)
            {
                bool isBlankTile = tileData[i] == -1;
                int tileId = (int)tileData[i] & 0x1FFFFFFF;
                TiledAttributes tiledAttributes = (TiledAttributes)(tileData[i] >> 28);
                TileAttributes tileAttributes = TileAttributes.None;

                if (options.Extended512)
                {
                    tileAttributes = TileMap.TiledToTileAttributes(tiledAttributes);

                    if (tileId - firstGid > 512)
                        over512 = true;

                    if (isBlankTile)
                        tileId = options.BlankTileId;

                    tileId -= firstGid;
                    byte attributesByte = (byte)((tileId >> 8) & 1 | (int)tileAttributes);

                    if (options.Split)
                    {
                        tileBytes.Add((byte)tileId);
                        attribBytes.Add(attributesByte);
                    }
                    else
                    {
                        tileBytes.Add((byte)tileId);
                        tileBytes.Add(attributesByte);
                    }
                }
                else
                {
                    if (tileId - firstGid > 255)
                        over256 = true;

                    if (isBlankTile)
                        tileId = options.BlankTileId;

                    tileId -= firstGid;
                    tileId = (isBlankTile ? options.BlankTileId : tileId & 0xFF);

                    tileBytes.Add((byte)tileId);
                }
            }

            if (options.Split)
                tileBytes.AddRange(attribBytes);

            if (options.CompressRLE)
            {
                int oldLength = tileBytes.Count;
                int bytes = (options.Extended512 && !options.Split ? 2 : 1);
                tileBytes = new List<byte>(Rle.Write(tileBytes.ToArray(), tileBytes.Count / bytes, bytes));

                Console.WriteLine($"Rle: {oldLength} -> {tileBytes.Count}");
            }

            if (options.CompressZx0)
            {
                int size = 0;
                int oldLength = tileBytes.Count;
                tileBytes = new List<byte>(Zx0.Compress(tileBytes.ToArray(), options.QuickMode, options.BackwardsMode, out size));

                Console.WriteLine($"Zx0: {oldLength} -> {tileBytes.Count}");
            }

            TileLayerAttributes layerAttributes = TileLayerAttributes.None;

            if (options.CompressZx0)
                layerAttributes |= TileLayerAttributes.CompressedZx0;

            if (options.Extended512)
                layerAttributes |= TileLayerAttributes.Extended512;

            if (options.QuickMode)
                layerAttributes |= TileLayerAttributes.QuickMode;

            if (options.BackwardsMode)
                layerAttributes |= TileLayerAttributes.BackwardsMode;

            if (options.CompressRLE)
                layerAttributes |= TileLayerAttributes.CompressedRLE;

            if (options.Split)
                layerAttributes |= TileLayerAttributes.Split;

            TileLayer tileLayer = new TileLayer
            {
                Id = byte.Parse(layerId),
                TileSet = Path.GetFileNameWithoutExtension(tileSet),
                Attributes = layerAttributes,
                Width = (ushort)width,
                Height = (ushort)height,
                TileWidth = (byte)tileWidth,
                TileHeight = (byte)tileHeight,
                DataLength = (ushort)tileBytes.Count
            };

            byteList.Add(tileBytes.ToArray());

            tileLayers.Add(tileLayer);

            return (tileLayers, byteList);
        }

        public static async Task WriteBin(Stream stream, TileMap tileMap, int version)
        {
            TileLayer tileLayer = ToTileLayer(tileMap, 1);

            using (BinaryWriter writer = new BinaryWriter(stream, Encoding.Default, true))
            {
                byte[] byteData = new byte[tileMap.TileData.Length * 2];
                Buffer.BlockCopy(tileMap.TileData, 0, byteData, 0, byteData.Length);

                // We don't want to use ZX0 on export as it's too slow
                tileLayer.Attributes &= ~(TileLayerAttributes.CompressedZx0);

                bool packedZx0 = tileLayer.Attributes.HasFlag(TileLayerAttributes.CompressedZx0);
                bool quickMode = tileLayer.Attributes.HasFlag(TileLayerAttributes.QuickMode);
                bool backwardsMode = tileLayer.Attributes.HasFlag(TileLayerAttributes.BackwardsMode);
                bool compressRLE = tileLayer.Attributes.HasFlag(TileLayerAttributes.CompressedRLE);
                bool split = tileLayer.Attributes.HasFlag(TileLayerAttributes.Split);

                if (split)
                {
                    int halfLength = byteData.Length / 2;
                    byte[] splitData = new byte[byteData.Length];

                    // Split interleving data
                    for (int i = 0; i < halfLength; i++)
                    {
                        splitData[i] = byteData[i * 2];
                        splitData[halfLength + i] = byteData[i * 2 + 1];
                    }

                    byteData = splitData;
                }

                if (compressRLE)
                {
                    byteData = Rle.Write(byteData, byteData.Length, 1);
                }

                if (packedZx0)
                {
                    byteData = Zx0.Compress(byteData, quickMode, backwardsMode, out int size);
                }

                await WriteBin(stream, new List<TileLayer> { tileLayer }, new List<byte[]> { byteData }, version);
            }
        }

        public static async Task WriteBin(Stream stream, string fileName, List<TileLayer> tileLayers, List<byte[]> byteList, bool includeHeader, int version)
        {
            if (includeHeader)
            {
                await WriteBin(stream, tileLayers, byteList, version);
            }
            else
            {
                foreach (byte[] byteData in byteList)
                    stream.Write(byteData, 0, byteData.Length);
            }
        }

        public static async Task WriteBin(Stream stream, List<TileLayer> tileLayers, List<byte[]> byteList, int version)
        {
            using (BinaryWriter writer = new BinaryWriter(stream, Encoding.Default, true))
            {
                writer.Write(TILE_HEADER_MAGIC);
                writer.Write((byte)version);
                writer.Write((byte)tileLayers.Count);

                for (int i = 0; i < tileLayers.Count; i++)
                {
                    TileLayer tileLayer = tileLayers[i];
                    writer.Write((byte)tileLayer.Id);
                    writer.Write(Encoding.ASCII.GetBytes(tileLayer.TileSet.PadRight(16, '\0')));
                    writer.Write((byte)tileLayer.Attributes);
                    writer.Write((short)tileLayer.Width);
                    writer.Write((short)tileLayer.Height);
                    writer.Write((byte)tileLayer.TileWidth);
                    writer.Write((byte)tileLayer.TileHeight);
                    writer.Write((short)byteList[i].Length);
                    writer.Write(byteList[i]);
                }
            }
        }

        public static async Task<(TileMap, Image)> CreateTileMap(IFileSource file, TileMapSliceOptions options, Action<object, string, int> progressCallback)
        {
            using (var memoryStream = new MemoryStream())
            {
                await file.OpenReadStream(1024 * 1024 * 1024).CopyToAsync(memoryStream);

                memoryStream.Position = 0;

                string name = Path.GetFileNameWithoutExtension(file.Name);

                return await TileMap.CreateTileMap(null, memoryStream, name, options, progressCallback);
            }
        }

        public static async Task<(TileMap, Image)> CreateTileMap(object sender, Stream stream, string name, TileMapSliceOptions options, Action<object, string, int> progressCallback)
        {
            Image srcImage = await PngReader.ReadAsync(stream);

            if (srcImage == null)
            {
                throw new Exception("Could not read the PNG file.");
            }

            if (srcImage.Width % options.TileWidth != 0 || srcImage.Height % options.TileHeight != 0)
            {
                throw new Exception("The image dimensions are not multiples of the tile size.");
            }

            int tileCols = srcImage.Width / options.TileWidth;
            int tileRows = srcImage.Height / options.TileHeight;

            List<TileNode> tileNodes = new List<TileNode>();
            List<Image> uniqueTiles = new List<Image>();

            Dictionary<UInt64, TileNode> tileDictionary = new Dictionary<UInt64, TileNode>();

            int tileCount = 0;
            int bitsPerPixel = srcImage.BitsPerPixel <= 8 ? 8 : 32;

            if (options.InsertBlankTile)
            {
                Image blankTile = new Image(options.TileWidth, options.TileHeight, bitsPerPixel, srcImage.Palette);
                uniqueTiles.Add(blankTile);

                tileDictionary[blankTile.GetHash()] = new TileNode(tileCount++, TileAttributes.None);
            }

            int totalTiles = tileCols * tileRows;
            int processedTiles = 0;

            for (int y = 0; y < tileRows; y++)
            {
                for (int x = 0; x < tileCols; x++)
                {
                    Rectangle srcRect = new Rectangle(x * options.TileWidth, y * options.TileHeight, options.TileWidth, options.TileHeight);
                    Image tileImage = new Image(options.TileWidth, options.TileHeight, bitsPerPixel, srcImage.Palette);

                    //grab the tile
                    tileImage.DrawImage(srcImage, new Rectangle(0, 0, tileImage.Width, tileImage.Height), srcRect);

                    //get the hash code of the image
                    UInt64 hash = tileImage.GetHash();

                    //if its not in the list already lets add it in all of its rotated orientations
                    if (!tileDictionary.ContainsKey(hash))
                    {
                        int id = tileCount++;

                        if (options.NoRepeat)
                        {
                            tileDictionary.Add(hash, new TileNode(id, TileAttributes.None));
                        }

                        if (options.NoMirror)
                        {
                            UInt64 noMirrorHash = tileImage.GetHash(ImageAttributes.MirrorX);

                            if (!tileDictionary.ContainsKey(noMirrorHash))
                                tileDictionary.Add(noMirrorHash, new TileNode(id, TileAttributes.MirrorX));

                            noMirrorHash = tileImage.GetHash(ImageAttributes.MirrorY);

                            if (!tileDictionary.ContainsKey(noMirrorHash))
                                tileDictionary.Add(noMirrorHash, new TileNode(id, TileAttributes.MirrorY));

                            noMirrorHash = tileImage.GetHash(ImageAttributes.MirrorX_Y);

                            if (!tileDictionary.ContainsKey(noMirrorHash))
                                tileDictionary.Add(noMirrorHash, new TileNode(id, TileAttributes.MirrorX_Y));
                        }

                        if (options.NoRotate)
                        {
                            UInt64 noRotateHash = tileImage.GetHash(ImageAttributes.Rotate | ImageAttributes.MirrorX_Y);

                            if (!tileDictionary.ContainsKey(noRotateHash))
                                tileDictionary.Add(noRotateHash, new TileNode(id, TileAttributes.Rotate | TileAttributes.MirrorX_Y));

                            noRotateHash = tileImage.GetHash(ImageAttributes.Rotate | ImageAttributes.MirrorX);

                            if (!tileDictionary.ContainsKey(noRotateHash))
                                tileDictionary.Add(noRotateHash, new TileNode(id, TileAttributes.Rotate | TileAttributes.MirrorX));

                            noRotateHash = tileImage.GetHash(ImageAttributes.Rotate | ImageAttributes.MirrorY);

                            if (!tileDictionary.ContainsKey(noRotateHash))
                                tileDictionary.Add(noRotateHash, new TileNode(id, TileAttributes.Rotate | TileAttributes.MirrorY));

                            noRotateHash = tileImage.GetHash(ImageAttributes.Rotate);

                            if (!tileDictionary.ContainsKey(noRotateHash))
                                tileDictionary.Add(noRotateHash, new TileNode(id, TileAttributes.Rotate));
                        }

                        uniqueTiles.Add(tileImage);
                    }

                    tileNodes.Add(tileDictionary[hash]);

                    if (++processedTiles % 100 == 0)
                    {
                        progressCallback?.Invoke(sender, "Processing Tiles...", (processedTiles * 100) / totalTiles);

                        await Task.Delay(1);
                    }
                }
            }

            int tileSetWidth = options.TileSetWidth;

            if (tileSetWidth == 0)
                tileSetWidth = Baker76.Imaging.Utility.CalculateTextureSize(options.TileWidth, options.TileHeight, uniqueTiles.Count);

            int destRows = (uniqueTiles.Count + (tileSetWidth / options.TileWidth) - 1) / (tileSetWidth / options.TileWidth);
            int destCols = (tileSetWidth / options.TileWidth);
            Image destImage = new Image(tileSetWidth, destRows * options.TileHeight, bitsPerPixel, srcImage.Palette);

            int tileIndex = 0;

            for (int destRow = 0; destRow < destRows; destRow++)
            {
                for (int destCol = 0; destCol < destCols; destCol++)
                {
                    if (tileIndex >= uniqueTiles.Count)
                        break;

                    Image tile = uniqueTiles[tileIndex];
                    int destX = destCol * options.TileWidth;
                    int destY = destRow * options.TileHeight;
                    Rectangle destRect = new Rectangle(destX, destY, tile.Width, tile.Height);
                    destImage.DrawImage(tile, destRect, new Rectangle(0, 0, tile.Width, tile.Height));
                    tileIndex++;
                }
            }

            ushort[] tileData = new ushort[tileCols * tileRows];

            for (int i = 0; i < tileNodes.Count; i++)
            {
                if (i >= tileData.Length)
                    break;

                tileData[i] = (options.ClearMap == -1 ? tileNodes[i].ToUShort() : (ushort)options.ClearMap);
            }

            TileMap tileMap = new TileMap(name, (ushort)tileCols, (ushort)tileRows, (byte)options.TileWidth, (byte)options.TileHeight, options.TileLayerAttributes, tileData, name);

            return (tileMap, destImage);
        }

        public static TileAttributes TiledToTileAttributes(TiledAttributes tiledAttributes)
        {
            TileAttributes tileAttributes = TileAttributes.None;

            if (tiledAttributes.HasFlag(TiledAttributes.AntiDiagonal))
            {
                if (tiledAttributes.HasFlag(TiledAttributes.Horizontal_Vertical))
                    tileAttributes = TileAttributes.Rotate | TileAttributes.MirrorY;
                else if (tiledAttributes.HasFlag(TiledAttributes.Horizontal))
                    tileAttributes = TileAttributes.Rotate;
                else if (tiledAttributes.HasFlag(TiledAttributes.Vertical))
                    tileAttributes = TileAttributes.Rotate | TileAttributes.MirrorX_Y;
                else
                    tileAttributes = TileAttributes.Rotate | TileAttributes.MirrorX;
            }
            else
            {
                if (tiledAttributes.HasFlag(TiledAttributes.Horizontal_Vertical))
                    tileAttributes = TileAttributes.MirrorX_Y;
                else if (tiledAttributes.HasFlag(TiledAttributes.Horizontal))
                    tileAttributes = TileAttributes.MirrorX;
                else if (tiledAttributes.HasFlag(TiledAttributes.Vertical))
                    tileAttributes = TileAttributes.MirrorY;
            }

            return tileAttributes;
        }
    }
}
