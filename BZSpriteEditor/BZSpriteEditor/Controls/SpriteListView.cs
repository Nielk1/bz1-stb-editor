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
        public class ListItemImageChangedEventArgs : EventArgs { public ListViewItem item;}
        public delegate void ListItemImageChangedHandler(object sender, ListItemImageChangedEventArgs args);
        public event ListItemImageChangedHandler ListItemImageChangedEvent;
        protected void OnListItemImageChangedEvent(ListViewItem item)
        {
            ListItemImageChangedEventArgs args = new ListItemImageChangedEventArgs() { item = item };
            ListItemImageChangedEvent(this, args);
        }

        private SpriteDataViewModel source;

        [Bindable(true)]
        [TypeConverter(typeof(SpriteDataViewModel))]
        [Category("Data")]
        public SpriteDataViewModel DataSource
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
        Bitmap ErrorImage;

        public SpriteListView()
        {
            //InitializeComponent();

            LargeImageList = new ImageList();
            LargeImageList.ImageSize = new Size(64, 64);
            LargeImageList.ColorDepth = ColorDepth.Depth32Bit;

            EmptyImage = new Bitmap(64, 64);
            {
                Graphics g = Graphics.FromImage(EmptyImage);

                g.Clear(Color.FromArgb(64, 64, 64, 64));
                Pen dottedPen = new Pen(Color.FromArgb(128, 128, 128, 128), 2);
                dottedPen.DashPattern = new float[] { 2, 2 };
                g.DrawRectangle(dottedPen, 1, 1, 62, 62);
            }

            ErrorImage = new Bitmap(64, 64);
            {
                Graphics g = Graphics.FromImage(ErrorImage);

                g.Clear(Color.FromArgb(64, 255, 0, 0));
                Pen dottedPen = new Pen(Color.FromArgb(255, 128, 0, 0), 2);
                Pen solidPen = new Pen(Color.FromArgb(255, 255, 0, 0), 6);
                dottedPen.DashPattern = new float[] { 2, 2 };
                g.DrawRectangle(dottedPen, 1, 1, 62, 62);
                g.DrawLine(solidPen, 16, 16, 32 + 16, 32 + 16);
                g.DrawLine(solidPen, 16, 32 + 16, 32 + 16, 16);
            }
        }

        public void bind()
        {
            this.BeginUpdate();

            bool tmpShowGroups = ShowGroups;
            ShowGroups = true;

            Items.Clear();
            Groups.Clear();
            ResetImageList();

            if (source != null)
            {
                source.RefreshEntireListEvent += new SpriteDataViewModel.RefreshEntireListHandler(source_RefreshEntireListEvent);
                source.RefreshEntireImageListEvent += new SpriteDataViewModel.RefreshEntireImageListHandler(source_RefreshEntireImageListEvent);
                source.NewListItemEvent += new SpriteDataViewModel.NewListItemHandler(source_NewListItemEvent);
                source.ImageUpdatedEvent += new SpriteDataViewModel.ImageUpdatedHandler(source_ImageUpdatedEvent);
            }

            ShowGroups = tmpShowGroups;

            this.EndUpdate();
        }

        private void source_ImageUpdatedEvent(object sender, SpriteDataViewModel.ImageUpdatedEventArgs args)
        {
            if (args.oldKey != null) LargeImageList.Images.RemoveByKey(args.oldKey);
            if (!LargeImageList.Images.ContainsKey(args.newKey)) LargeImageList.Images.Add(args.newKey, args.listImage);

            OnListItemImageChangedEvent(args.listitem);
        }

        private void source_RefreshEntireListEvent(object sender)
        {
            ListViewItem[] items = source.GetListViewItems();

            Form1.BackgroundQueueWorker.RunAsync(null, (obj, args) => { FullRefreshList(items); }, null);
        }

        private void FullRefreshList(ListViewItem[] items)
        {
            BeginUpdate();

            Items.Clear();
            Items.AddRange(items);

            EndUpdate();
        }

        private void ResetImageList()
        {
            LargeImageList.Images.Clear();
            LargeImageList.Images.Add("nul", EmptyImage);
            LargeImageList.Images.Add("err", ErrorImage);
        }

        private void source_RefreshEntireImageListEvent(object sender)
        {
            List<KeyValuePair<string,Image>> images = source.GetImageListItems();
            ResetImageList();
            images.ForEach(dr =>
            {
                LargeImageList.Images.Add(dr.Key, dr.Value);
            });
        }

        private void source_NewListItemEvent(object sender, SpriteDataViewModel.NewListItemEventArgs args)
        {
            List<ListViewItem> Sprites = args.NewSprites;
            Form1.BackgroundQueueWorker.RunAsync(null, (obj, argsX) => { NewListItem(Sprites); }, null);
        }

        private void NewListItem(List<ListViewItem> Sprites)
        {
            BeginUpdate();

            Items.AddRange(Sprites.ToArray());

            EndUpdate();
        }

        public Image GetFullImage(Sprite sprite)
        {
            return source.GetFullImage(sprite);
        }
    }
}
