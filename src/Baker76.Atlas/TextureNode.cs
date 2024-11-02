using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Baker76.Atlas
{
    public class TextureNode
    {
        private enum XmlElement
        {
            None,
            Texture,
            Image
        }

        public string FileName = String.Empty;
        public string Name = String.Empty;
        public string Path = String.Empty;
        public int Width = 0;
        public int Height = 0;
        public int Format = 0;
        public bool Clamp = true;
        public bool Linear = true;
        public bool ColorKey = false;
        public List<ImageNode> Images = null;
        public bool UseRefCount = true;
        public int RefCount = 0;

        public TextureNode()
        {
            Images = new List<ImageNode>();
        }

        public TextureNode(int width, int height, int format)
        {
            Width = width;
            Height = height;
            Format = format;
            Images = new List<ImageNode>();
        }

        public TextureNode(string fileName, string name, int width, int height)
        {
            FileName = fileName;
            Name = name;
            Width = width;
            Height = height;
            Images = new List<ImageNode>();
        }

        public TextureNode(string fileName, string name, bool clamp, bool linear)
        {
            FileName = fileName;
            Name = name;
            Clamp = clamp;
            Linear = linear;
            Images = new List<ImageNode>();
        }

        public TextureNode(string fileName, string name, int width, int height, bool clamp, bool linear)
        {
            FileName = fileName;
            Name = name;
            Width = width;
            Height = height;
            Clamp = clamp;
            Linear = linear;
            Images = new List<ImageNode>();
        }

        public static TextureNode ReadXml(string fileName)
        {
            XmlElement xmlElement = XmlElement.None;
            Hashtable attribHash = new Hashtable();
            TextureNode textureNode = null;

            using (XmlReader xmlReader = XmlReader.Create(fileName))
            {
                xmlReader.Read();

                while (xmlReader.Read())
                {
                    switch (xmlReader.NodeType)
                    {
                        case XmlNodeType.Element:
                            switch (xmlReader.LocalName.ToLower())
                            {
                                case "texture":
                                    xmlElement = XmlElement.Texture;
                                    textureNode = new TextureNode();
                                    break;
                                case "image":
                                    xmlElement = XmlElement.Image;
                                    break;
                                default:
                                    xmlElement = XmlElement.None;
                                    break;
                            }

                            if (xmlReader.HasAttributes)
                            {
                                attribHash.Clear();
                                while (xmlReader.MoveToNextAttribute())
                                    attribHash.Add(xmlReader.Name.ToLower(), xmlReader.Value);
                            }

                            switch (xmlElement)
                            {
                                case XmlElement.Texture:
                                    if (textureNode != null)
                                    {
                                        textureNode.FileName = System.IO.Path.Combine((string)attribHash["path"], (string)attribHash["name"]);
                                        textureNode.Name = (string)attribHash["name"];
                                        textureNode.Path = (string)attribHash["path"];
                                        textureNode.Width = int.Parse((string)attribHash["width"]);
                                        textureNode.Height = int.Parse((string)attribHash["height"]);
                                        textureNode.Clamp = bool.Parse((string)attribHash["clamp"]);
                                        textureNode.Linear = bool.Parse((string)attribHash["linear"]);
                                        textureNode.ColorKey = bool.Parse((string)attribHash["colorkey"]);
                                    }
                                    break;
                                case XmlElement.Image:
                                    if (textureNode != null)
                                        textureNode.Images.Add(new ImageNode(textureNode, int.Parse((string)attribHash["id"]), int.Parse((string)attribHash["frameindex"]), (string)attribHash["name"], (string)attribHash["label"], int.Parse((string)attribHash["loopdirection"]), int.Parse((string)attribHash["duration"]), int.Parse((string)attribHash["x"]), int.Parse((string)attribHash["y"]), int.Parse((string)attribHash["width"]), int.Parse((string)attribHash["height"]), int.Parse((string)attribHash["framex"]), int.Parse((string)attribHash["framey"]), int.Parse((string)attribHash["framewidth"]), int.Parse((string)attribHash["frameheight"])));
                                    break;
                                default:
                                    break;
                            }

                            xmlReader.MoveToElement();
                            break;

                        case XmlNodeType.Text:
                            string text = xmlReader.Value.Trim();
                            switch (xmlElement)
                            {
                                default:
                                    break;
                            }
                            break;

                        case XmlNodeType.EndElement:
                            switch (xmlElement)
                            {
                                case XmlElement.Texture:
                                    textureNode = null;
                                    break;
                                default:
                                    break;
                            }
                            break;
                    }
                }
            }

            return textureNode;
        }

        public static void WriteTxt(string fileName, List<TextureNode> textureList)
        {
            List<string> textList = new List<string>();

            foreach (TextureNode textureNode in textureList)
            {
                textList.Add(String.Format("tex n={0} w={1} h={2} n={3}", textureNode.Name, textureNode.Width, textureNode.Height, textureNode.Images.Count));

                foreach (ImageNode imageNode in textureNode.Images)
                    textList.Add(String.Format("img fi={0} n={1} l={2} ld={3} d={4} x={5} y={6} w={7} h={8} fx={9} fy={10} fw={11} fh={12} ps={13}", imageNode.FrameIndex, imageNode.Name, imageNode.Label, imageNode.LoopDirection, imageNode.Duration, imageNode.X, imageNode.Y, imageNode.Width, imageNode.Height, imageNode.FrameX, imageNode.FrameY, imageNode.FrameWidth, imageNode.FrameHeight, imageNode.PaletteSlot));
            }

            File.WriteAllLines(fileName, textList.ToArray());
        }

        public static void WriteJson(string fileName, List<TextureNode> textureList)
        {
            using (StreamWriter jsonWriter = new StreamWriter(fileName))
            {
                jsonWriter.WriteLine("{");
                jsonWriter.WriteLine("\t\"trim\":true,");
                jsonWriter.WriteLine("\t\"rotate\":false,");
                jsonWriter.WriteLine("\t\"textures\":[");

                foreach (TextureNode textureNode in textureList)
                {
                    jsonWriter.WriteLine("\t\t{");
                    jsonWriter.WriteLine($"\t\t\t\"name\":\"{textureNode.Name}\",");
                    jsonWriter.WriteLine($"\t\t\t\"width\":{textureNode.Width},");
                    jsonWriter.WriteLine($"\t\t\t\"height\":{textureNode.Height},");
                    jsonWriter.WriteLine($"\t\t\t\"format\":\"{textureNode.Format}\",");
                    jsonWriter.WriteLine("\t\t\t\"images\":[");

                    foreach (ImageNode imageNode in textureNode.Images)
                    {
                        jsonWriter.WriteLine("\t\t\t\t{");
                        jsonWriter.WriteLine($"\t\t\t\t\t\"fi\":{imageNode.FrameIndex},");
                        jsonWriter.WriteLine($"\t\t\t\t\t\"n\":\"{imageNode.Name}\",");
                        jsonWriter.WriteLine($"\t\t\t\t\t\"l\":\"{imageNode.Label}\",");
                        jsonWriter.WriteLine($"\t\t\t\t\t\"ld\":{imageNode.LoopDirection},");
                        jsonWriter.WriteLine($"\t\t\t\t\t\"d\":{imageNode.Duration},");
                        jsonWriter.WriteLine($"\t\t\t\t\t\"x\":{imageNode.X},");
                        jsonWriter.WriteLine($"\t\t\t\t\t\"y\":{imageNode.Y},");
                        jsonWriter.WriteLine($"\t\t\t\t\t\"w\":{imageNode.Width},");
                        jsonWriter.WriteLine($"\t\t\t\t\t\"h\":{imageNode.Height},");
                        jsonWriter.WriteLine($"\t\t\t\t\t\"fx\":{imageNode.FrameX},");
                        jsonWriter.WriteLine($"\t\t\t\t\t\"fy\":{imageNode.FrameY},");
                        jsonWriter.WriteLine($"\t\t\t\t\t\"fw\":{imageNode.FrameWidth},");
                        jsonWriter.WriteLine($"\t\t\t\t\t\"fh\":{imageNode.FrameHeight},");
                        jsonWriter.WriteLine($"\t\t\t\t\t\"ps\":{imageNode.PaletteSlot}");
                        jsonWriter.WriteLine("\t\t\t\t},");
                    }

                    jsonWriter.WriteLine("\t\t\t]");
                    jsonWriter.Write("\t\t}");
                    if (textureNode != textureList.Last())
                    {
                        jsonWriter.Write(",");
                    }
                    jsonWriter.WriteLine();
                }

                jsonWriter.WriteLine("\t]");
                jsonWriter.WriteLine("}");
            }
        }

        public static void WriteXml(string fileName, List<TextureNode> textureList)
        {
            using (StreamWriter xml = new StreamWriter(fileName))
            {
                xml.WriteLine("<atlas>");
                xml.WriteLine("\t<trim>true</trim>");
                xml.WriteLine("\t<rotate>false</rotate>");

                foreach (TextureNode textureNode in textureList)
                {
                    xml.WriteLine("\t<tex n=\"" + textureNode.Name + "\" w=\"" + textureNode.Width + " h=\"" + textureNode.Height + "\" format=\"" + textureNode.Format + "\">");

                    foreach (ImageNode imageNode in textureNode.Images)
                        xml.WriteLine("\t\t<img fi=\"" + imageNode.FrameIndex + "\" n=\"" + imageNode.Name + "\" l=\"" + imageNode.Label + "\" ld=\"" + imageNode.LoopDirection + "\" d=\"" + imageNode.Duration + "\" x=\"" + imageNode.X + "\" y=\"" + imageNode.Y + "\" w=\"" + imageNode.Width + "\" h=\"" + imageNode.Height + "\" fx=\"" + imageNode.FrameX + "\" fy=\"" + imageNode.FrameY + "\" fw=\"" + imageNode.FrameWidth + "\" fh=\"" + imageNode.FrameHeight + "\" ps=\"" + imageNode.PaletteSlot + "\" />");

                    xml.WriteLine("\t</tex>");
                }

                xml.WriteLine("</atlas>");
            }
        }

        private static long UnixTimeNow()
        {
            var timeSpan = (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0));
            return (long)timeSpan.TotalSeconds;
        }

        public static void WriteMeta(List<TextureNode> textureList)
        {
            foreach (TextureNode textureNode in textureList)
            {
                List<string> textList = new List<string>();

                textList.Add("fileFormatVersion: 2");
                textList.Add(String.Format("guid: {0}", Guid.NewGuid().ToString("N")));
                textList.Add(String.Format("timeCreated: {0}", UnixTimeNow()));
                textList.Add("licenseType: Pro");
                textList.Add("TextureImporter:");
                textList.Add("  fileIDToRecycleName:");

                int fileID = 21300000;

                foreach (ImageNode imageNode in textureNode.Images)
                {
                    textList.Add(String.Format("    {0}: {1}", fileID, imageNode.Name));

                    fileID += 2;
                }

                textList.Add("  serializedVersion: 4");
                textList.Add("  mipmaps:");
                textList.Add("    mipMapMode: 0");
                textList.Add("    enableMipMap: 0");
                textList.Add("    sRGBTexture: 1");
                textList.Add("    linearTexture: 0");
                textList.Add("    fadeOut: 0");
                textList.Add("    borderMipMap: 0");
                textList.Add("    mipMapFadeDistanceStart: 1");
                textList.Add("    mipMapFadeDistanceEnd: 3");
                textList.Add("  bumpmap:");
                textList.Add("    convertToNormalMap: 0");
                textList.Add("    externalNormalMap: 0");
                textList.Add("    heightScale: 0.25");
                textList.Add("    normalMapFilter: 0");
                textList.Add("  isReadable: 0");
                textList.Add("  grayScaleToAlpha: 0");
                textList.Add("  generateCubemap: 6");
                textList.Add("  cubemapConvolution: 0");
                textList.Add("  seamlessCubemap: 0");
                textList.Add("  textureFormat: -3");
                textList.Add("  maxTextureSize: 2048");
                textList.Add("  textureSettings:");
                textList.Add("    filterMode: -1");
                textList.Add("    aniso: 16");
                textList.Add("    mipBias: -1");
                textList.Add("    wrapMode: 1");
                textList.Add("  nPOTScale: 0");
                textList.Add("  lightmap: 0");
                textList.Add("  compressionQuality: 50");
                textList.Add("  spriteMode: 2");
                textList.Add("  spriteExtrude: 1");
                textList.Add("  spriteMeshType: 0");
                textList.Add("  alignment: 0");
                textList.Add("  spritePivot: {x: 0.5, y: 0.5}");
                textList.Add("  spriteBorder: {x: 0, y: 0, z: 0, w: 0}");
                textList.Add("  spritePixelsToUnits: 1");
                textList.Add("  alphaUsage: 1");
                textList.Add("  alphaIsTransparency: 1");
                textList.Add("  spriteTessellationDetail: -1");
                textList.Add("  textureType: 8");
                textList.Add("  textureShape: 1");
                textList.Add("  maxTextureSizeSet: 0");
                textList.Add("  compressionQualitySet: 0");
                textList.Add("  textureFormatSet: 0");
                textList.Add("  platformSettings:");
                textList.Add("  - buildTarget: DefaultTexturePlatform");
                textList.Add("    maxTextureSize: 2048");
                textList.Add("    textureFormat: -1");
                textList.Add("    textureCompression: 0");
                textList.Add("    compressionQuality: 50");
                textList.Add("    crunchedCompression: 0");
                textList.Add("    allowsAlphaSplitting: 0");
                textList.Add("    overridden: 0");
                textList.Add("  - buildTarget: Standalone");
                textList.Add("    maxTextureSize: 2048");
                textList.Add("    textureFormat: -1");
                textList.Add("    textureCompression: 0");
                textList.Add("    compressionQuality: 50");
                textList.Add("    crunchedCompression: 0");
                textList.Add("    allowsAlphaSplitting: 0");
                textList.Add("    overridden: 0");
                textList.Add("  spriteSheet:");
                textList.Add("    serializedVersion: 2");
                textList.Add("    sprites:");

                foreach (ImageNode imageNode in textureNode.Images)
                {
                    textList.Add("    - serializedVersion: 2");
                    textList.Add(String.Format("      name: {0}", imageNode.Name));
                    textList.Add("      rect:");
                    textList.Add("        serializedVersion: 2");
                    textList.Add(String.Format("        x: {0}", imageNode.X));
                    textList.Add(String.Format("        y: {0}", textureNode.Height - imageNode.Y - imageNode.Height));
                    textList.Add(String.Format("        width: {0}", imageNode.Width));
                    textList.Add(String.Format("        height: {0}", imageNode.Height));
                    textList.Add("      alignment: 0");
                    textList.Add("      pivot: {x: 0.5, y: 0.5}");
                    textList.Add("      border: {x: 0, y: 0, z: 0, w: 0}");
                    textList.Add("      outline: []");
                    textList.Add("      tessellationDetail: -1");
                }

                textList.Add("    outline: []");
                textList.Add("  spritePackingTag: ");
                textList.Add("  userData: ");
                textList.Add("  assetBundleName: ");
                textList.Add("  assetBundleVariant: ");

                File.WriteAllLines(textureNode.Path + ".meta", textList.ToArray());
            }
        }

        public int ImageCount
        {
            get { return Images.Count; }
        }
    }
}
