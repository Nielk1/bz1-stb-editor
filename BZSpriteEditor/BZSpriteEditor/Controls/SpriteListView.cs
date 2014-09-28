using System;
using System.Collections.Generic;
using System.ComponentModel;
//using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;

namespace BZSpriteEditor.Controls
{
    public partial class SpriteListView : ListView
    {
        public class ListItemImageChangedEventArgs : EventArgs
        {
            public ListViewItem item;
        }

        public delegate void ListItemImageChangedHandler(object sender, ListItemImageChangedEventArgs args);
        //private ListItemImageChangedHandler listItemImageChangedHandler = null;
        public event ListItemImageChangedHandler ListItemImageChangedEvent;
        protected void OnListItemImageChangedEvent(ListViewItem item)
        {
            ListItemImageChangedEventArgs args = new ListItemImageChangedEventArgs() { item = item };
            ListItemImageChangedEvent(this, args);
        }

        private SpriteData source;

        [Bindable(true)]
        [TypeConverter(typeof(SpriteData))]
        [Category("Data")]
        public SpriteData DataSource
        {
            get
            {
                return source;
            }
            set
            {
                source = value;
            }
        }

        Bitmap EmptyImage;

        public SpriteListView()
        {
            //InitializeComponent();

            EmptyImage = new Bitmap(64, 64);
            {
                Graphics g = Graphics.FromImage(EmptyImage);

                //g.Clear(Color.Transparent);
                g.Clear(Color.FromArgb(64, 64, 64, 64));
                Pen dottedPen = new Pen(Color.FromArgb(128, 128, 128, 128), 2);
                dottedPen.DashPattern = new float[] { 2, 2 };
                //g.FillRectangle(new SolidBrush(Color.FromArgb(64, 64, 64, 64)), 2, 2, 60, 60);
                g.DrawRectangle(dottedPen, 1, 1, 62, 62);
            }
        }

        //private void bind()
        public void bind()
        {
            this.BeginUpdate();

            bool tmpShowGroups = ShowGroups;
            ShowGroups = true;

            Items.Clear();
            Groups.Clear();
            if (LargeImageList != null && LargeImageList.Images != null) LargeImageList.Images.Clear();

            if (source != null)
            {
                source.SpritesDeletedEvent += new SpriteData.SpritesDeletedHandler(source_SpritesDeletedEvent);
                source.SpritesAddedEvent += new SpriteData.SpritesAddedHandler(source_SpritesAddedEvent);

                if (LargeImageList != null && LargeImageList.Images != null)
                {
                    LargeImageList.Images.Add("nul", EmptyImage);
                    source.Sprites.ForEach(dr =>
                    {
                        dr.NewImageGeneratedEvent += new Sprite.NewImageGeneratedHandler(OnNewImageGeneratedEvent);
                        dr.UpdateGroupsEvent += new Sprite.UpdateGroupsHandler(OnUpdateGroupsEvent);

                        Image icon = dr.RenderImage;
                        if (icon != null)
                        {
                            LargeImageList.Images.Add(dr.GetImageName(), icon);
                        }
                    });
                }

                Groups.AddRange(source.GetGroups());
                Items.AddRange(source.Sprites.Select(dr => dr.ListItem).ToArray());
            }

            ShowGroups = tmpShowGroups;

            this.EndUpdate();
        }

        private void OnNewImageGeneratedEvent(object sender)
        {
            Sprite dr = (Sprite)sender;

            Image icon = dr.RenderImage;
            string name = dr.GetImageName();

            if (icon != null && !LargeImageList.Images.ContainsKey(name))
            {
                LargeImageList.Images.Add(dr.GetImageName(), icon);
            }

            OnListItemImageChangedEvent(dr.ListItem);
        }

        private void OnUpdateGroupsEvent(object sender)
        {
            BeginUpdate();

            bool tmpShowGroups = ShowGroups;
            ShowGroups = true;

            Sprite dr = (Sprite)sender;

            Items.Remove(dr.ListItem);

            dr.ListItem.Group = source.GetGroup(dr.Category);
            if (!Groups.Contains(dr.ListItem.Group)) Groups.Add(dr.ListItem.Group);

            Items.Add(dr.ListItem);

            ShowGroups = tmpShowGroups;

            EndUpdate();
        }

        public void source_SpritesDeletedEvent(object sender, List<ListViewItem> listItemsToDelete)
        {
            if (listItemsToDelete != null)
            {
                foreach (ListViewItem item in listItemsToDelete)
                {
                    Items.Remove(item);
                }
            }
        }

        public void source_SpritesAddedEvent(object sender, List<ListViewItem> listItemsToAdd)
        {
            if (listItemsToAdd != null)
            {
                if (LargeImageList != null && LargeImageList.Images != null)
                {
                    LargeImageList.Images.Add("nul", EmptyImage);
                    listItemsToAdd.Select(dr => (Sprite)(dr.Tag)).ToList().ForEach(dr =>
                    {
                        dr.NewImageGeneratedEvent += new Sprite.NewImageGeneratedHandler(OnNewImageGeneratedEvent);
                        dr.UpdateGroupsEvent += new Sprite.UpdateGroupsHandler(OnUpdateGroupsEvent);

                        Image icon = dr.RenderImage;
                        if (icon != null)
                        {
                            LargeImageList.Images.Add(dr.GetImageName(), icon);
                        }
                    });
                }

                Items.AddRange(listItemsToAdd.ToArray());
            }
        }
    }
}
