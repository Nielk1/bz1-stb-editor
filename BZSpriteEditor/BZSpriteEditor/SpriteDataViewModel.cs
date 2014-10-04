using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.IO;

namespace BZSpriteEditor
{
    public interface ImageHandler
    {
        void GetImage(Sprite sprite, out Image ThumbImage, out Image LargeImage);
    }

    public class HardwareImageHandler : ImageHandler
    {
        Color COLOR_White = Color.FromArgb(255, 255, 255);
        Color COLOR_Tan = Color.FromArgb(238, 204, 170);
        Color COLOR_Brown = Color.FromArgb(68, 51, 34);
        Color COLOR_Black = Color.FromArgb(0, 0, 0);
        Color COLOR_Green = Color.FromArgb(0, 255, 0);
        Color COLOR_Yellow = Color.FromArgb(255, 255, 0);
        Color COLOR_Red = Color.FromArgb(255, 0, 0);
        Color COLOR_Blue = Color.FromArgb(0, 127, 255);
        Color COLOR_Cyan = Color.FromArgb(0, 255, 255);
        Color COLOR_Grey = Color.FromArgb(153, 153, 153);
        private Color mulColor(Color A, Sprite.Colorization ColorMode)
        {
            Color B = COLOR_White;
            switch (ColorMode)
            {
                case Sprite.Colorization.Tan: B = COLOR_Tan; break;
                case Sprite.Colorization.Brown: B = COLOR_Brown; break;
                case Sprite.Colorization.Black: B = COLOR_Black; break;
                case Sprite.Colorization.Green: B = COLOR_Green; break;
                case Sprite.Colorization.Yellow: B = COLOR_Yellow; break;
                case Sprite.Colorization.Red: B = COLOR_Red; break;
                case Sprite.Colorization.Blue: B = COLOR_Blue; break;
                case Sprite.Colorization.Cyan: B = COLOR_Cyan; break;
                case Sprite.Colorization.Grey: B = COLOR_Grey; break;
            }
            return Color.FromArgb(
                A.A,
                (byte)(A.R * B.R / 255.0),
                (byte)(A.G * B.G / 255.0),
                (byte)(A.B * B.B / 255.0));
        }

        public void GetImage(Sprite sprite, out Image ThumbImage, out Image LargeImage)
        {
            if (sprite.Width > 0 && sprite.Height > 0)
            {
                Bitmap bmp = new Bitmap(sprite.Width, sprite.Height);

                string filename = sprite.Texture + ".map";

                if (File.Exists(filename))
                {
                    MapFile source = null;

                    //if (ImageMemo != null && ImageMemo.ContainsKey(filename))
                    //{
                    //    source = ImageMemo[filename];
                    //}
                    //else
                    {
                        source = MapFile.FromFile(filename);

                        //    if (ImageMemo != null && source != null)
                        //    {
                        //        ImageMemo[filename] = source;
                        //    }
                    }

                    int fileWidth = source.Width;
                    int fileHeight = source.Height;

                    for (int x = 0; x < sprite.Width; x++)
                    {
                        for (int y = 0; y < sprite.Height; y++)
                        {
                            if ((x + sprite.OffsetX < fileWidth) && (y + sprite.OffsetY < fileHeight))
                            {
                                Color baseColor = mulColor(source.GetPixel(x + sprite.OffsetX, y + sprite.OffsetY), sprite.ColorFlag);
                                bmp.SetPixel(x, y, baseColor);
                            }
                        }
                    }

                    //Image = bmp;

                    Bitmap centeredBmp = new Bitmap(64, 64);
                    Graphics g = Graphics.FromImage(centeredBmp);


                    // Figure out the ratio
                    double ratioX = (double)64 / (double)sprite.Width;
                    double ratioY = (double)64 / (double)sprite.Height;
                    // use whichever multiplier is smaller
                    double ratio = ratioX < ratioY ? ratioX : ratioY;

                    if (ratio > 1) ratio = 1;

                    // now we can get the new height and width
                    int newHeight = Convert.ToInt32(sprite.Height * ratio);
                    int newWidth = Convert.ToInt32(sprite.Width * ratio);

                    // Now calculate the X,Y position of the upper-left corner 
                    // (one of these will always be zero)
                    int posX = Convert.ToInt32((64 - (sprite.Width * ratio)) / 2);
                    int posY = Convert.ToInt32((64 - (sprite.Height * ratio)) / 2);

                    //g.Clear(Color.White); // white padding
                    g.Clear(Color.Transparent);
                    g.DrawImage(bmp, posX, posY, newWidth, newHeight);

                    LargeImage = bmp;
                    ThumbImage = centeredBmp; return;
                    //return bmp;
                }
                LargeImage = null;
                ThumbImage = null; return;
            }
            else if (sprite.Height > 0 || sprite.Width > 0)
            {
                Bitmap blankBMP = new Bitmap(Math.Max(1, (int)sprite.Width), Math.Max(1, (int)sprite.Height));
                Graphics g = Graphics.FromImage(blankBMP);
                g.Clear(Color.Transparent);

                LargeImage = blankBMP;
                ThumbImage = blankBMP; return;
                //return blankBMP;
            }
            LargeImage = null;
            ThumbImage = null; return;
            //return null;
        }
    }

    public class SoftwareImageHandler : ImageHandler
    {
        //byte[] pallet;
        Color[][] pallet;
        Color[][] interfacePallet;

        string[] palletNames = new string[16] {
            "interface.act",//"hwhite.act",
            "htan.act",
            "hbrown.act",
            "hblack.act",
            "hgreen.act",
            "hyellow.act",
            "hred.act",
            "hblue.act",
            "hcyan.act",
            "hgrey.act",
            null,
            "hplasgrn.act",
            "hplasred.act",
            "hplasblu.act",
            "explode.act",
            "explode.act" };

        public class SoftwareUnrenderableException : Exception
        {

        }

        public SoftwareImageHandler()
        {
            byte[] palletData = File.ReadAllBytes("alphapal.abgr");
            pallet = new Color[16][];
            for (int x = 0; x < 16; x++)
            {
                pallet[x] = new Color[16];
            }

            interfacePallet = new Color[16][];
            for (int palletSubIDX = 0; palletSubIDX < 16; palletSubIDX++)
            {
                if (palletNames[palletSubIDX] != null)
                {
                    interfacePallet[palletSubIDX] = new Color[256];
                    {
                        byte[] palletInterfaceData = File.ReadAllBytes(palletNames[palletSubIDX]);
                        for (int x = 0; x < 256; x++)
                        {
                            interfacePallet[palletSubIDX][x] = Color.FromArgb(
                                palletInterfaceData[(x * 3) + 0],
                                palletInterfaceData[(x * 3) + 1],
                                palletInterfaceData[(x * 3) + 2]
                            );
                            if (
                                interfacePallet[palletSubIDX][x].R == 255 &&
                                interfacePallet[palletSubIDX][x].G == 0 &&
                                interfacePallet[palletSubIDX][x].B == 255
                            ) interfacePallet[palletSubIDX][x] = Color.Transparent;
                        }
                    }
                }
            }


            for (int palletID = 0; palletID < 16; palletID++)
            {
                for (int idx = 0; idx < 16; idx++)
                {
                    pallet[palletID][idx] =
                    Color.FromArgb(
                        255 - palletData[(((16 * palletID) + idx) * 4) + 3],
                        palletData[(((16 * palletID) + idx) * 4) + 0],
                        palletData[(((16 * palletID) + idx) * 4) + 1],
                        palletData[(((16 * palletID) + idx) * 4) + 2]
                    );
                }
            }
        }

        public void GetImage(Sprite sprite, out Image ThumbImage, out Image LargeImage)
        {
            if (sprite.Width > 0 && sprite.Height > 0)
            {
                Bitmap bmp = new Bitmap(sprite.Width, sprite.Height);

                string filename = sprite.Texture + ".map";

                if (File.Exists(filename))
                {
                    MapFile source = null;

                    //if (ImageMemo != null && ImageMemo.ContainsKey(filename))
                    //{
                    //    source = ImageMemo[filename];
                    //}
                    //else
                    {
                        source = MapFile.FromFile(filename);

                        //    if (ImageMemo != null && source != null)
                        //    {
                        //        ImageMemo[filename] = source;
                        //    }
                    }

                    if (!source.IsPalletized) throw new SoftwareUnrenderableException();

                    int fileWidth = source.Width;
                    int fileHeight = source.Height;

                    for (int x = 0; x < sprite.Width; x++)
                    {
                        for (int y = 0; y < sprite.Height; y++)
                        {
                            if ((x + sprite.OffsetX < fileWidth) && (y + sprite.OffsetY < fileHeight))
                            {
                                if (interfacePallet[(int)sprite.ColorFlag] != null)
                                {
                                    //Color tmpColor = source.GetPixel(x + sprite.OffsetX, y + sprite.OffsetY, pallet[(int)sprite.ColorFlag]);
                                    //Color baseColor = source.GetPixel(x + sprite.OffsetX, y + sprite.OffsetY, interfacePallet[(int)sprite.ColorFlag]);
                                    //baseColor = Color.FromArgb(tmpColor.A, baseColor.R, baseColor.G, baseColor.B);

                                    Color baseColor = source.GetPixel(x + sprite.OffsetX, y + sprite.OffsetY, interfacePallet[(int)sprite.ColorFlag]);
                                    bmp.SetPixel(x, y, baseColor);
                                }
                                else
                                {
                                    Color baseColor = source.GetPixel(x + sprite.OffsetX, y + sprite.OffsetY, pallet[(int)sprite.ColorFlag]);
                                    bmp.SetPixel(x, y, baseColor);
                                }
                            }
                        }
                    }

                    //Image = bmp;

                    Bitmap centeredBmp = new Bitmap(64, 64);
                    Graphics g = Graphics.FromImage(centeredBmp);


                    // Figure out the ratio
                    double ratioX = (double)64 / (double)sprite.Width;
                    double ratioY = (double)64 / (double)sprite.Height;
                    // use whichever multiplier is smaller
                    double ratio = ratioX < ratioY ? ratioX : ratioY;

                    if (ratio > 1) ratio = 1;

                    // now we can get the new height and width
                    int newHeight = Convert.ToInt32(sprite.Height * ratio);
                    int newWidth = Convert.ToInt32(sprite.Width * ratio);

                    // Now calculate the X,Y position of the upper-left corner 
                    // (one of these will always be zero)
                    int posX = Convert.ToInt32((64 - (sprite.Width * ratio)) / 2);
                    int posY = Convert.ToInt32((64 - (sprite.Height * ratio)) / 2);

                    //g.Clear(Color.White); // white padding
                    g.Clear(Color.Transparent);
                    g.DrawImage(bmp, posX, posY, newWidth, newHeight);



                    LargeImage = bmp;
                    ThumbImage = centeredBmp; return;
                    //return centeredBmp;
                    //return bmp;
                }
                LargeImage = null;
                ThumbImage = null; return;
                //return null;
            }
            else if (sprite.Height > 0 || sprite.Width > 0)
            {
                Bitmap blankBMP = new Bitmap(Math.Max(1, (int)sprite.Width), Math.Max(1, (int)sprite.Height));
                Graphics g = Graphics.FromImage(blankBMP);
                g.Clear(Color.Transparent);

                LargeImage = blankBMP;
                ThumbImage = blankBMP; return;
                //return blankBMP;
            }
            LargeImage = null;
            ThumbImage = null; return;
            //return null;
        }
    }

    public class SpriteDataViewModel
    {
        SpriteTableBinary data;

        public delegate void RefreshEntireListHandler(object sender/*, RefreshEntireListEventArgs args*/);
        public event RefreshEntireListHandler RefreshEntireListEvent;
        protected void OnRefreshEntireListEvent()
        {
            RefreshEntireListHandler evt = RefreshEntireListEvent;
            if (evt != null)
            {
                evt(this);
            }
        }

        public delegate void RefreshEntireImageListHandler(object sender/*, RefreshEntireImageListEventArgs args*/);
        public event RefreshEntireImageListHandler RefreshEntireImageListEvent;
        protected void OnRefreshEntireImageListEvent()
        {
            RefreshEntireImageListHandler evt = RefreshEntireImageListEvent;
            if (evt != null)
            {
                evt(this);
            }
        }

        public class NewListItemEventArgs : EventArgs
        {
            public List<ListViewItem> NewSprites;
        }
        public delegate void NewListItemHandler(object sender, NewListItemEventArgs args);
        public event NewListItemHandler NewListItemEvent;
        protected void OnNewListItemEvent(List<ListViewItem> NewSprites)
        {
            NewListItemHandler evt = NewListItemEvent;
            if (evt != null)
            {
                evt(this, new NewListItemEventArgs() { NewSprites = NewSprites });
            }
        }

        public class ImageUpdatedEventArgs : EventArgs
        {
            public string oldKey;
            public string newKey;
            public Image listImage;
            public ListViewItem listitem;
        }
        public delegate void ImageUpdatedHandler(object sender, ImageUpdatedEventArgs args);
        public event ImageUpdatedHandler ImageUpdatedEvent;
        protected void OnImageUpdatedEvent(ListViewItem listitem, Image listImage, string newKey, string oldKey)
        {
            ImageUpdatedHandler evt = ImageUpdatedEvent;
            if (evt != null)
            {
                evt(this, new ImageUpdatedEventArgs() { listImage = listImage, newKey = newKey, oldKey = oldKey, listitem = listitem });
            }
        }

        /*public delegate void ListItemUpdatedHandler(object sender, ListViewItem listitem);
        public event ListItemUpdatedHandler ListItemUpdatedEvent;
        protected void OnListItemUpdatedEvent(ListViewItem listitem)
        {
            ListItemUpdatedHandler evt = ListItemUpdatedEvent;
            if (evt != null)
            {
                evt(this, listitem);
            }
        }*/

        Dictionary<Sprite, ListViewItem> SpriteToItemMap;
        Dictionary<ListViewItem, Sprite> ItemToSpriteMap;
        Dictionary<string, Image> Images;
        Dictionary<string, int> ImageKeyCount;
        Dictionary<Sprite, Image> PreviewImages;
        ImageHandler ImageHandler;

        public SpriteDataViewModel()
        {
            data = new SpriteTableBinary();
            SpriteToItemMap = new Dictionary<Sprite, ListViewItem>();
            ImageKeyCount = new Dictionary<string, int>();
            ItemToSpriteMap = new Dictionary<ListViewItem, Sprite>();
            Images = new Dictionary<string, Image>();
            PreviewImages = new Dictionary<Sprite, Image>();
            ImageHandler = new HardwareImageHandler();
            //ImageHandler = new SoftwareImageHandler();
        }

        private void ProcessImage(Sprite dr, ref string imageKey)
        {
            if (!Images.ContainsKey(imageKey) || !PreviewImages.ContainsKey(dr))
            {
                try
                {
                    Image thumbImage;
                    Image largeImage;
                    ImageHandler.GetImage(dr, out thumbImage, out largeImage);
                    if (thumbImage != null)
                    {
                        if (!Images.ContainsKey(imageKey)) Images.Add(imageKey, thumbImage);
                        if (!PreviewImages.ContainsKey(dr)) PreviewImages.Add(dr, largeImage);

                        if (!ImageKeyCount.ContainsKey(imageKey)) ImageKeyCount[imageKey] = 0;
                        ImageKeyCount[imageKey]++;
                    }
                    else
                    {
                        imageKey = "nul";
                    }
                }
                catch (SoftwareImageHandler.SoftwareUnrenderableException)
                {
                    imageKey = "err";
                }
            }
        }

        /// <summary>
        /// Create a new SpriteTableBinary and load file into it
        /// </summary>
        /// <param name="filename">Full path to STB file</param>
        public void OpenSpriteData(string filename)
        {
            data = new SpriteTableBinary(filename);

            Images.Clear();
            PreviewImages.Clear();
            SpriteToItemMap.Clear();
            ItemToSpriteMap.Clear();
            data.Sprites.ForEach(dr =>
            {
                string imageKey = dr.Texture + ":" + dr.OffsetX + ":" + dr.OffsetY + ":" + dr.Width + ":" + dr.Height + ":" + dr.ColorFlag;

                ProcessImage(dr, ref imageKey);

                ListViewItem listItem = new ListViewItem(dr.Name, imageKey);
                listItem.Tag = dr;

                //dr.NewImageGeneratedEvent += new Sprite.NewImageGeneratedHandler(dr_NewImageGeneratedEvent);
                dr.ColorFlagChangedEvent += new Sprite.ColorFlagChangedHandler(dr_ColorFlagChangedEvent);
                dr.TextureChangedEvent += new Sprite.TextureChangedHandler(dr_TextureChangedEvent);
                dr.OffsetXChangedEvent += new Sprite.OffsetXChangedHandler(dr_OffsetXChangedEvent);
                dr.OffsetYChangedEvent += new Sprite.OffsetYChangedHandler(dr_OffsetYChangedEvent);
                dr.WidthChangedEvent += new Sprite.WidthChangedHandler(dr_WidthChangedEvent);
                dr.HeightChangedEvent += new Sprite.HeightChangedHandler(dr_HeightChangedEvent);
                dr.UpdateNameEvent += new Sprite.UpdateNameHandler(dr_UpdateNameEvent);

                SpriteToItemMap.Add(dr, listItem);
                ItemToSpriteMap.Add(listItem, dr);
            });

            OnRefreshEntireListEvent();
            OnRefreshEntireImageListEvent();
        }

        public void SaveSpriteData(string filename)
        {
            data.WriteFile(filename);
        }

        private void dr_UpdateNameEvent(object sender)
        {
            Sprite dr = (Sprite)sender;
            SpriteToItemMap[dr].Text = dr.Name;
            //OnListItemUpdatedEvent(SpriteToItemMap[dr]);
        }

        private void dr_ColorFlagChangedEvent(object sender, Sprite.Colorization oldColor, Sprite.Colorization newColor)
        {
            Sprite dr = (Sprite)sender;
            string oldImageKey = dr.Texture + ":" + dr.OffsetX + ":" + dr.OffsetY + ":" + dr.Width + ":" + dr.Height + ":" + oldColor;
            string newImageKey = dr.Texture + ":" + dr.OffsetX + ":" + dr.OffsetY + ":" + dr.Width + ":" + dr.Height + ":" + newColor;

            HandleTextureChangeCommon(ref dr, ref oldImageKey, ref newImageKey);
        }

        private void dr_TextureChangedEvent(object sender, string oldTexture, string newTexture)
        {
            Sprite dr = (Sprite)sender;
            string oldImageKey = oldTexture + ":" + dr.OffsetX + ":" + dr.OffsetY + ":" + dr.Width + ":" + dr.Height + ":" + dr.ColorFlag;
            string newImageKey = newTexture + ":" + dr.OffsetX + ":" + dr.OffsetY + ":" + dr.Width + ":" + dr.Height + ":" + dr.ColorFlag;

            HandleTextureChangeCommon(ref dr, ref oldImageKey, ref newImageKey);
        }

        private void dr_OffsetXChangedEvent(object sender, ushort oldOffsetX, ushort newOffsetX)
        {
            Sprite dr = (Sprite)sender;
            string oldImageKey = dr.Texture + ":" + oldOffsetX + ":" + dr.OffsetY + ":" + dr.Width + ":" + dr.Height + ":" + dr.ColorFlag;
            string newImageKey = dr.Texture + ":" + newOffsetX + ":" + dr.OffsetY + ":" + dr.Width + ":" + dr.Height + ":" + dr.ColorFlag;

            HandleTextureChangeCommon(ref dr, ref oldImageKey, ref newImageKey);
        }

        private void dr_OffsetYChangedEvent(object sender, ushort oldOffsetY, ushort newOffsetY)
        {
            Sprite dr = (Sprite)sender;
            string oldImageKey = dr.Texture + ":" + dr.OffsetX + ":" + oldOffsetY + ":" + dr.Width + ":" + dr.Height + ":" + dr.ColorFlag;
            string newImageKey = dr.Texture + ":" + dr.OffsetX + ":" + newOffsetY + ":" + dr.Width + ":" + dr.Height + ":" + dr.ColorFlag;

            HandleTextureChangeCommon(ref dr, ref oldImageKey, ref newImageKey);
        }

        private void dr_WidthChangedEvent(object sender, ushort oldWidth, ushort newWidth)
        {
            Sprite dr = (Sprite)sender;
            string oldImageKey = dr.Texture + ":" + dr.OffsetX + ":" + dr.OffsetY + ":" + oldWidth + ":" + dr.Height + ":" + dr.ColorFlag;
            string newImageKey = dr.Texture + ":" + dr.OffsetX + ":" + dr.OffsetY + ":" + newWidth + ":" + dr.Height + ":" + dr.ColorFlag;

            HandleTextureChangeCommon(ref dr, ref oldImageKey, ref newImageKey);
        }

        private void dr_HeightChangedEvent(object sender, ushort oldHeight, ushort newHeight)
        {
            Sprite dr = (Sprite)sender;
            string oldImageKey = dr.Texture + ":" + dr.OffsetX + ":" + dr.OffsetY + ":" + dr.Width + ":" + oldHeight + ":" + dr.ColorFlag;
            string newImageKey = dr.Texture + ":" + dr.OffsetX + ":" + dr.OffsetY + ":" + dr.Width + ":" + newHeight + ":" + dr.ColorFlag;

            HandleTextureChangeCommon(ref dr, ref oldImageKey, ref newImageKey);
        }

        private void HandleTextureChangeCommon(ref Sprite dr, ref string oldImageKey, ref string newImageKey)
        {
            if (PreviewImages.ContainsKey(dr)) PreviewImages.Remove(dr);

            if (ImageKeyCount.ContainsKey(oldImageKey))
            {
                ImageKeyCount[oldImageKey]--;
                if (ImageKeyCount[oldImageKey] == 0)
                {
                    ImageKeyCount.Remove(oldImageKey);
                    if (Images.ContainsKey(oldImageKey)) Images.Remove(oldImageKey);
                    oldImageKey = null;
                }
            }
            else
            {
                oldImageKey = null;
            }

            //if (!ImageKeyCount.ContainsKey(newImageKey)) ImageKeyCount[newImageKey] = 0;
            //ImageKeyCount[newImageKey]++;

            ProcessImage(dr, ref newImageKey);
            ListViewItem itr = SpriteToItemMap[dr];
            itr.ImageKey = newImageKey;
            OnImageUpdatedEvent(itr, Images.ContainsKey(newImageKey) ? Images[newImageKey] : null, newImageKey, oldImageKey);
        }

        /// <summary>
        /// Load a file into an the existing SpriteTableBinary
        /// </summary>
        /// <param name="filename">Full path to the STB file</param>
        public void ImportSpriteData(string filename)
        {
            Images.Clear();
            PreviewImages.Clear();
            SpriteToItemMap.Clear();
            ItemToSpriteMap.Clear();
            data.Sprites.ForEach(dr =>
            {
                string imageKey = dr.Texture + ":" + dr.OffsetX + ":" + dr.OffsetY + ":" + dr.Width + ":" + dr.Height + ":" + dr.ColorFlag;

                ProcessImage(dr, ref imageKey);

                ListViewItem listItem = new ListViewItem(dr.Name, imageKey);
                listItem.Tag = dr;

                //dr.NewImageGeneratedEvent += new Sprite.NewImageGeneratedHandler(dr_NewImageGeneratedEvent);
                dr.ColorFlagChangedEvent += new Sprite.ColorFlagChangedHandler(dr_ColorFlagChangedEvent);
                dr.TextureChangedEvent += new Sprite.TextureChangedHandler(dr_TextureChangedEvent);
                dr.OffsetXChangedEvent += new Sprite.OffsetXChangedHandler(dr_OffsetXChangedEvent);
                dr.OffsetYChangedEvent += new Sprite.OffsetYChangedHandler(dr_OffsetYChangedEvent);
                dr.WidthChangedEvent += new Sprite.WidthChangedHandler(dr_WidthChangedEvent);
                dr.HeightChangedEvent += new Sprite.HeightChangedHandler(dr_HeightChangedEvent);

                SpriteToItemMap.Add(dr, listItem);
                ItemToSpriteMap.Add(listItem, dr);
            });

            OnRefreshEntireListEvent();
            OnRefreshEntireImageListEvent();
        }

        /// <summary>
        /// Get an array of all ListViewItems for refreshing the entire listview
        /// </summary>
        /// <returns>Array of ListViewItems</returns>
        public ListViewItem[] GetListViewItems()
        {
            return SpriteToItemMap.Values.ToArray();
        }

        public List<KeyValuePair<string, Image>> GetImageListItems()
        {
            return Images.ToList();
        }

        /// <summary>
        /// Add a new sprite to the SpriteTableBinary
        /// Triggers OnNewListItemEvent
        /// </summary>
        public void AddNewSprite()
        {
            Sprite dr = data.AddNewSprite();

            string imageKey = "nul";

            ProcessImage(dr, ref imageKey);

            ListViewItem listItem = new ListViewItem(dr.Name, imageKey);
            listItem.Tag = dr;

            //dr.NewImageGeneratedEvent += new Sprite.NewImageGeneratedHandler(dr_NewImageGeneratedEvent);
            dr.ColorFlagChangedEvent += new Sprite.ColorFlagChangedHandler(dr_ColorFlagChangedEvent);
            dr.TextureChangedEvent += new Sprite.TextureChangedHandler(dr_TextureChangedEvent);
            dr.OffsetXChangedEvent += new Sprite.OffsetXChangedHandler(dr_OffsetXChangedEvent);
            dr.OffsetYChangedEvent += new Sprite.OffsetYChangedHandler(dr_OffsetYChangedEvent);
            dr.WidthChangedEvent += new Sprite.WidthChangedHandler(dr_WidthChangedEvent);
            dr.HeightChangedEvent += new Sprite.HeightChangedHandler(dr_HeightChangedEvent);

            SpriteToItemMap.Add(dr, listItem);
            ItemToSpriteMap.Add(listItem, dr);

            OnNewListItemEvent(new List<ListViewItem>() { listItem });
        }

        public Image GetFullImage(Sprite sprite)
        {
            return PreviewImages.ContainsKey(sprite) ? PreviewImages[sprite] : null;
        }

        //private void dr_NewImageGeneratedEvent(object sender)
        //{
        //    OnRefreshEntireImageListEvent();
        //}
    }
}
