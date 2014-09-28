using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;
using System.Drawing;
using System.ComponentModel;

namespace BZSpriteEditor
{
    public class Sprite
    {
        private static int U_OFFSET = 32 + 8;
        private static int V_OFFSET = 32 + 8 + 2;
        private static int WIDTH_OFFSET = 32 + 8 + 2 + 2;
        private static int HEIGHT_OFFSET = 32 + 8 + 2 + 2 + 2;
        private static int FLAGS_OFFSET = 32 + 8 + 2 + 2 + 2 + 2;

        public enum Colorization
        {
            White      = 0x0,
            Tan        = 0x1,
            Brown      = 0x2,
            Black      = 0x3,
            Green      = 0x4,
            Yellow     = 0x5,
            Red        = 0x6,
            Blue       = 0x7,
            Cyan       = 0x8,
            Grey       = 0x9,
            UNUSED     = 0xA,
            PlasmaGree = 0xB,
            PlasmaRed  = 0xC,
            PlasmaBlue = 0xD,
            Explosion1 = 0xE,
            Explosion2 = 0xF
        }

        public char[] name; // Unique name of the sprite
        public char[] texture; // MAP texture file to use for the sprite
        public ushort u; // Position of the left edge of the sprite rectangle in pixels/texels
        public ushort v; // Position of the top edge of the sprite rectangle in pixels/texels
        public ushort width; // Width of the sprite rectangle in pixels/texels
        public ushort height; // Height of the sprite rectangle in pixels/texels
        public uint flags; // Extra data associated with the sprite (lowest 4 bits are colorization)

        public delegate void NewImageGeneratedHandler(object sender);
        //private NewImageGeneratedHandler newImageGeneratedHandler = null;
        public event NewImageGeneratedHandler NewImageGeneratedEvent;
        protected void OnNewImageGeneratedEvent()
        {
            NewImageGeneratedEvent(this);
        }

        public delegate void UpdateGroupsHandler(object sender);
        //private UpdateGroupsHandler updateGroupsHandler = null;
        public event UpdateGroupsHandler UpdateGroupsEvent;
        protected void OnUpdateGroupsEvent()
        {
            UpdateGroupsEvent(this);
        }

        private static Dictionary<string, Bitmap> ImageMemo;

        public override string ToString()
        {
            return ("\"" + Name + "\"").PadRight(34) + " " +
                Texture.PadRight(8) + " " +
                OffsetX.ToString().PadLeft(4) + " " +
                OffsetY.ToString().PadLeft(4) + " " +
                Width.ToString().PadLeft(4) + " " +
                Height.ToString().PadLeft(4) + " " +
                string.Format("0x{0:x8}", flags);
        }

        public static void EnableMemo()
        {
            if (ImageMemo == null) ImageMemo = new Dictionary<string, Bitmap>();
        }

        public static void ClearImageMemo()
        {
            if (ImageMemo != null)
            {
                ImageMemo.Values.AsEnumerable().ToList().ForEach(dr =>
                {
                    dr.Dispose();
                });

                ImageMemo.Clear();
            }
        }

        public static void DisableMemo()
        {
            ClearImageMemo();

            ImageMemo = null;
        }

        [Category("Render")]
        [Description("Hardware rendering texture module color")]
        [DisplayName("Color")]
        public Colorization ColorFlag {
            get { return (Colorization)(flags & 0xf); }
            set {
                flags = (flags & 0xfff0) | ((uint)value & 0xf);
                UpdateImageFromSettings();
            }
        }

        [Category("Name")]
        [Description("Unique name used for reference in game files and code")]
        public string Name {
            get { return new string(name.TakeWhile(dr => dr != '\0').ToArray()).Trim(); }
            set {
                value.Trim().PadRight(32, '\0').Substring(0, 32).ToCharArray().CopyTo(name,0);
                listItem.Text = Name;
            }
        }

        [Category("Render")]
        [Description("Texture file to select from")]
        public string Texture {
            get { return new string(texture.TakeWhile(dr => dr != '\0').ToArray()).Trim(); }
            set {
                value.Trim().PadRight(8, '\0').Substring(0, 8).ToCharArray().CopyTo(texture, 0);
                UpdateImageFromSettings();
            }
        }

        [Category("Clipping")]
        [Description("X coordinate on the image that the sprite selection starts")]
        [DisplayName("X")]
        public ushort OffsetX { get { return u; } set { u = value; UpdateImageFromSettings(); } }
        [Category("Clipping")]
        [Description("Y coordinate on the image that the sprite selection starts")]
        [DisplayName("Y")]
        public ushort OffsetY { get { return v; } set { v = value; UpdateImageFromSettings(); } }
        [Category("Clipping")]
        [Description("Width of the sprite")]
        public ushort Width { get { return width; } set { width = value; UpdateImageFromSettings(); } }
        [Category("Clipping")]
        [Description("Height of the sprite")]
        public ushort Height { get { return height; } set { height = value; UpdateImageFromSettings(); } }

        private ListViewItem listItem;
        [Browsable(false)]
        [ReadOnly(true)]
        public ListViewItem ListItem { get { return listItem; } }

        [Browsable(false)]
        [ReadOnly(true)]
        public Image RenderImage { get; private set; }

        [Browsable(false)]
        [ReadOnly(true)]
        public Image Image { get; private set; }

        private string category;

        [Category("Extra")]
        [Description("Category for easy human use")]
        public string Category {
            get { return category; }
            set
            {
                category = value;
                //UpdateImageFromSettings();
                OnUpdateGroupsEvent();
            }
        }

        public Sprite(char[] name, char[] texture, ushort u, ushort v, ushort width, ushort height, uint flags, SpriteData dataModel, string category)
        {
            this.name = new char[32];
            name.Take(32).ToArray().CopyTo(this.name, 0);

            this.texture = new char[8];
            texture.Take(8).ToArray().CopyTo(this.texture, 0);

            this.u = u;
            this.v = v;
            this.width = width;
            this.height = height;
            this.flags = flags;

            this.category = category;

            SetImage();
            listItem = new ListViewItem(Name, GetImageName());
            listItem.Tag = this;

            listItem.Group = dataModel.GetGroup(category);
        }

        public Sprite()
        {
            this.name = new char[32];
            "NewSprite".ToArray().CopyTo(this.name, 0);

            this.texture = new char[8];
            "newsprt".ToArray().CopyTo(this.texture, 0);

            this.u = 0;
            this.v = 0;
            this.width = 0;
            this.height = 0;
            this.flags = 0;

            this.category = string.Empty;

            SetImage();
            listItem = new ListViewItem(Name, GetImageName());
            listItem.Tag = this;

            //listItem.Group = dataModel.GetGroup(category);
        }

        private void UpdateImageFromSettings()
        {
            SetImage();
            listItem.ImageKey = GetImageName();

            OnNewImageGeneratedEvent();
        }

        ~Sprite()
        {
            if (Image != null) Image.Dispose();
            if (RenderImage != null) RenderImage.Dispose();
        }

        //olor COLOR_White = Color.FromArgb(255, 255, 255, 255);
        Color COLOR_Tan = Color.FromArgb(255, 238, 204, 170);
        Color COLOR_Brown = Color.FromArgb(255, 68, 51, 34);
        Color COLOR_Black = Color.FromArgb(255, 0, 0, 0);
        Color COLOR_Green = Color.FromArgb(255, 0, 255, 0);
        Color COLOR_Yellow = Color.FromArgb(255, 255, 255, 0);
        Color COLOR_Red = Color.FromArgb(255, 255, 0, 0);
        Color COLOR_Blue = Color.FromArgb(255, 0, 127, 255);
        Color COLOR_Cyan = Color.FromArgb(255, 0, 255, 255);
        Color COLOR_Grey = Color.FromArgb(255, 153, 153, 153);
        private static Color mulColor(Color A, Color B)
        {
            return Color.FromArgb(
                (byte)(A.A * B.A / 255.0),
                (byte)(A.R * B.R / 255.0),
                (byte)(A.G * B.G / 255.0),
                (byte)(A.B * B.B / 255.0));
        }

        public string GetImageName()
        {
            if (RenderImage == null) return "nul";
            return Texture + ":" + u + ":" + v + ":" + width + ":" + height + ":" + flags;
        }

        private void SetImage()
        {
            if (width > 0 && height > 0)
            {
                Bitmap bmp = new Bitmap(width, height);

                string filename = Texture + ".png";
                if (!File.Exists(filename)) filename = Texture + ".bmp";
                if (!File.Exists(filename)) filename = Texture + ".tga";
                if (!File.Exists(filename)) filename = Texture + ".jpg";
                if (!File.Exists(filename)) filename = Texture + ".jpeg";
                if (!File.Exists(filename)) filename = Texture + ".gif";

                if (File.Exists(filename))
                {
                    Bitmap source;

                    if (ImageMemo != null && ImageMemo.ContainsKey(filename))
                    {
                        source = ImageMemo[filename];
                    }
                    else
                    {
                        if (filename.ToLowerInvariant().EndsWith(".tga"))
                        {
                            source = Paloma.TargaImage.LoadTargaImage(filename);
                        }
                        else
                        {
                            source = new Bitmap(Image.FromFile(filename));
                        }

                        if (ImageMemo != null)
                        {
                            ImageMemo[filename] = source;
                        }
                    }

                    for (int x = 0; x < width; x++)
                    {
                        for (int y = 0; y < height; y++)
                        {
                            if ((x + u < source.Width) && (y + v < source.Height))
                            {
                                Color baseColor = source.GetPixel(x + u, y + v);
                                switch (ColorFlag)
                                {
                                    case Colorization.White:
                                        break;
                                    case Colorization.Tan:
                                        baseColor = mulColor(baseColor, COLOR_Tan);
                                        break;
                                    case Colorization.Brown:
                                        baseColor = mulColor(baseColor, COLOR_Brown);
                                        break;
                                    case Colorization.Black:
                                        baseColor = mulColor(baseColor, COLOR_Black);
                                        break;
                                    case Colorization.Green:
                                        baseColor = mulColor(baseColor, COLOR_Green);
                                        break;
                                    case Colorization.Yellow:
                                        baseColor = mulColor(baseColor, COLOR_Yellow);
                                        break;
                                    case Colorization.Red:
                                        baseColor = mulColor(baseColor, COLOR_Red);
                                        break;
                                    case Colorization.Blue:
                                        baseColor = mulColor(baseColor, COLOR_Blue);
                                        break;
                                    case Colorization.Cyan:
                                        baseColor = mulColor(baseColor, COLOR_Cyan);
                                        break;
                                    case Colorization.Grey:
                                        baseColor = mulColor(baseColor, COLOR_Grey);
                                        break;
                                }
                                bmp.SetPixel(x, y, baseColor);
                            }
                        }
                    }

                    Image = bmp;

                    Bitmap centeredBmp = new Bitmap(64, 64);
                    Graphics g = Graphics.FromImage(centeredBmp);
                    //g.DrawImageUnscaled(bmp, 256/2 - width / 2, 128/2 - height / 2);




                    // Figure out the ratio
                    double ratioX = (double)64 / (double)width;
                    double ratioY = (double)64 / (double)height;
                    // use whichever multiplier is smaller
                    double ratio = ratioX < ratioY ? ratioX : ratioY;

                    if (ratio > 1) ratio = 1;

                    // now we can get the new height and width
                    int newHeight = Convert.ToInt32(height * ratio);
                    int newWidth = Convert.ToInt32(width * ratio);

                    // Now calculate the X,Y position of the upper-left corner 
                    // (one of these will always be zero)
                    int posX = Convert.ToInt32((64 - (width * ratio)) / 2);
                    int posY = Convert.ToInt32((64 - (height * ratio)) / 2);

                    //g.Clear(Color.White); // white padding
                    g.Clear(Color.Transparent);
                    g.DrawImage(bmp, posX, posY, newWidth, newHeight);




                    RenderImage = centeredBmp; return;
                    //return bmp;
                }
                RenderImage = null; return;
            }
            else if(height > 0 || width > 0)
            {
                Bitmap blankBMP = new Bitmap(Math.Max(1, (int)width), Math.Max(1, (int)height));
                Graphics g = Graphics.FromImage(blankBMP);
                g.Clear(Color.Transparent);

                RenderImage = blankBMP; return;
            }
            RenderImage = null; return;
        }

        public byte[] GetBytes()
        {
            byte[] bytes = new byte[32 + 8 + 2 + 2 + 2 + 2 + 4];

            name.Select(dr => (byte)dr).ToArray().CopyTo(bytes, 0);
            texture.Select(dr => (byte)dr).ToArray().CopyTo(bytes, 32);
            System.BitConverter.GetBytes(u).CopyTo(bytes, U_OFFSET);
            System.BitConverter.GetBytes(v).CopyTo(bytes, V_OFFSET);
            System.BitConverter.GetBytes(width).CopyTo(bytes, WIDTH_OFFSET);
            System.BitConverter.GetBytes(height).CopyTo(bytes, HEIGHT_OFFSET);
            System.BitConverter.GetBytes(flags).CopyTo(bytes, FLAGS_OFFSET);

            return bytes;
        }
    }

    public class SpriteData
    {
        private Dictionary<string, ListViewGroup> GroupMap;
        public ListViewGroup GetGroup(string name)
        {
            if (name == null || name.Length == 0) return null;//name = "[Default]";

            if (!GroupMap.ContainsKey(name))
            {
                GroupMap[name] = new ListViewGroup(name);
            }

            return GroupMap[name];
        }

        public void RemoveItems(List<Sprite> sprites)
        {
            List<Sprite> spritesToDelete = Sprites.Where(dr => sprites.Contains(dr)).ToList();
            List<ListViewItem> listItemsToDelete = spritesToDelete.Select(dr => dr.ListItem).ToList();

            // remove sprites from our internal list now that we are holding references above
            spritesToDelete.ForEach(dr =>
            {
                Sprites.Remove(dr);
            });

            // throw out events here that list these removals, though we might not need these depending on how we do it
            OnSpritesDeletedEvent(listItemsToDelete);
        }

        public void AddNewSprite()
        {
            List<ListViewItem> newsprites = new List<ListViewItem>();
            Sprite newSprite = new Sprite();
            newsprites.Add(newSprite.ListItem);
            Sprites.Add(newSprite);

            OnSpritesAddedEvent(newsprites);
        }

        public delegate void SpritesDeletedHandler(object sender, List<ListViewItem> listItemsToDelete);
        public event SpritesDeletedHandler SpritesDeletedEvent;
        protected void OnSpritesDeletedEvent(List<ListViewItem> listItemsToDelete)
        {
            SpritesDeletedEvent(this, listItemsToDelete);
        }

        public delegate void SpritesAddedHandler(object sender, List<ListViewItem> listItemsToAdd);
        public event SpritesAddedHandler SpritesAddedEvent;
        protected void OnSpritesAddedEvent(List<ListViewItem> listItemsToAdd)
        {
            SpritesAddedEvent(this, listItemsToAdd);
        }


        public ListViewGroup[] GetGroups()
        {
            return GroupMap.Values.ToArray();
        }

        public List<Sprite> Sprites { get; private set; }
        //private ImageList images;

        private static int U_OFFSET      = 32 + 8;
        private static int V_OFFSET      = 32 + 8 + 2;
        private static int WIDTH_OFFSET  = 32 + 8 + 2 + 2;
        private static int HEIGHT_OFFSET = 32 + 8 + 2 + 2 + 2;
        private static int FLAGS_OFFSET  = 32 + 8 + 2 + 2 + 2 + 2;

        public SpriteData()
        {
            Sprites = new List<Sprite>();
            //images = new ImageList();
            GroupMap = new Dictionary<string, ListViewGroup>();
        }

        public void ReadFile(Stream filestream, Dictionary<string, string> Groups = null)
        {
            Sprite.EnableMemo();

            byte[] readBytes = new byte[32 + 8 + 2 + 2 + 2 + 2 + 4];
            while (filestream.Read(readBytes, 0, readBytes.Length) > 0)
            {
                char[] name = readBytes.Take(32).Select(dr => (char)dr).ToArray();
                char[] texture = readBytes.Skip(32).Take(8).Select(dr => (char)dr).ToArray();
                ushort u = System.BitConverter.ToUInt16(readBytes, U_OFFSET);
                ushort v = System.BitConverter.ToUInt16(readBytes, V_OFFSET);
                ushort width = System.BitConverter.ToUInt16(readBytes, WIDTH_OFFSET);
                ushort height = System.BitConverter.ToUInt16(readBytes,HEIGHT_OFFSET);
                uint flags = System.BitConverter.ToUInt32(readBytes, FLAGS_OFFSET);

                string tmpName = new string(name.TakeWhile(dr => dr != '\0').ToArray()).Trim();
                string tmpTexture = new string(texture.TakeWhile(dr => dr != '\0').ToArray()).Trim();
                string cat = (Groups != null && Groups.ContainsKey(tmpName + "\t" + tmpTexture)) ? Groups[tmpName + "\t" + tmpTexture] : null;

                Sprite spr = new Sprite(name, texture, u, v, width, height, flags, this, cat);
                Sprites.Add(spr);

                //images.Images.Add(spr.GetImageKey());
            }

            Sprite.DisableMemo();
        }

        public void WriteFile(FileStream stream)
        {
            for (int i = 0; i < Sprites.Count; i++)
            {
                byte[] data = Sprites[i].GetBytes();
                stream.Write(data, 0, data.Length);
            }
        }

        public void WriteFile(FileStream stream, List<Sprite> SpritesRestricted)
        {
            for (int i = 0; i < Sprites.Count; i++)
            {
                if (SpritesRestricted.Contains(Sprites[i]))
                {
                    byte[] data = Sprites[i].GetBytes();
                    stream.Write(data, 0, data.Length);
                }
            }
        }

        /*public ImageList GetImages()
        {
            ImageList lst = new ImageList();

            Sprites.ForEach(dr =>
            {
                Image tmp = dr.GetImage();
                if (tmp != null)
                {
                    lst.Images.Add(dr.GetImageName(), dr.GetImage());
                }
            });

            return lst;
        }*/
    }
}
