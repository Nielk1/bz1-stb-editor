using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using BZSpriteEditor.Controls;
using System.Threading;

namespace BZSpriteEditor
{
    public partial class Form1 : Form
    {
        SpriteData spriteData;
        List<Sprite> AnimationCells = new List<Sprite>();
        int animationIndex = 0;
        bool loadOperationIsImport = false;

        public Form1()
        {
            InitializeComponent();

            listView1.LargeImageList = new ImageList();
            listView1.LargeImageList.ImageSize = new Size(64, 64);//new Size(256, 128);
            listView1.LargeImageList.ColorDepth = ColorDepth.Depth32Bit;

            listView1.ListItemImageChangedEvent += new SpriteListView.ListItemImageChangedHandler(listView1_ListItemImageChanged);
        }

        private void openSTBToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                if (File.Exists(openFileDialog1.FileName))
                {
                    menuStrip1.Enabled = false;
                    listView1.Enabled = false;
                    listBox1.Enabled = false;
                    propertyGrid1.Enabled = false;
                    pictureBox1.Enabled = false;
                    exportSpritesToolStripMenuItem.Enabled = false;

                    pictureBox1.Image = null;

                    animationTimer.Stop();
                    listView1.Clear();
                    listBox1.DataSource = null;

                    toolStripButtonUp.Enabled = false;
                    toolStripButtonDown.Enabled = false;

                    toolStripStatusLabel1.Text = "Busy";
                    backgroundWorker1.RunWorkerAsync();
                }
            }
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            int numSelectedItems = listView1.SelectedItems.Count;

            if (numSelectedItems > 0)
            {
                exportSpritesToolStripMenuItem.Enabled = true;
                deleteSpriteToolStripMenuItem.Enabled = true;
            }
            else
            {
                exportSpritesToolStripMenuItem.Enabled = false;
                deleteSpriteToolStripMenuItem.Enabled = false;
            }

            if (numSelectedItems == 1)
            {
                animationTimer.Stop();
                propertyGrid1.SelectedObject = listView1.SelectedItems[0].Tag;
                pictureBox1.Image = ((Sprite)(listView1.SelectedItems[0].Tag)).Image;
                //propertyGrid1.Enabled = true;
                //moveBackToolStripMenuItem.Enabled = true;
                //moveForwardToolStripMenuItem.Enabled = true;

                listBox1.SelectedItem = listView1.SelectedItems[0].Tag;
            }
            else
            {
                //propertyGrid1.Enabled = false;
                propertyGrid1.SelectedObject = null;
                //moveBackToolStripMenuItem.Enabled = false;
                //moveForwardToolStripMenuItem.Enabled = false;

                listBox1.SelectedItem = null;

                if (numSelectedItems > 0)
                {
                    AnimationCells.Clear();
                    ListView.SelectedListViewItemCollection items = listView1.SelectedItems;
                    foreach(ListViewItem item in items)
                    {
                        AnimationCells.Add(((Sprite)(item.Tag)));
                    }
                    animationIndex = 0;
                    animationTimer.Start();
                }
                else
                {
                    animationTimer.Stop();
                    pictureBox1.Image = null;
                }
            }
        }

        private void listView1_ListItemImageChanged(object sender, SpriteListView.ListItemImageChangedEventArgs args)
        {
            int numSelectedItems = listView1.SelectedItems.Count;

            if (numSelectedItems == 1)
            {
                if (listView1.SelectedItems[0] == args.item)
                {
                    pictureBox1.Image = ((Sprite)(listView1.SelectedItems[0].Tag)).Image;
                }
            }
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Created by Nielk1 in a few hours on 2014-09-27", "About", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1);
        }

        private void toggleGroupsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            listView1.ShowGroups = toggleGroupsToolStripMenuItem.Checked;
            listView1.Refresh();

            if (toggleGroupsToolStripMenuItem.Checked)
            {
                toggleGroupsToolStripMenuItem.Text = "&Groups On";
            }
            else
            {
                toggleGroupsToolStripMenuItem.Text = "&Groups Off"; 
            }
        }

        private void animationTimer_Tick(object sender, EventArgs e)
        {
            if (animationIndex >= AnimationCells.Count)
                animationIndex = 0;
            if (AnimationCells.Count > 0)
            {
                pictureBox1.Image = AnimationCells[animationIndex].Image;
                animationIndex++;
            }
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            string filename = openFileDialog1.FileName;

            SpriteData spriteDataLocal = null;

            using (FileStream steam = File.Open(filename, FileMode.Open))
            {
                if (steam != null)
                {
                    Dictionary<string, string> Groups = new Dictionary<string, string>();
                    if (File.Exists(Path.ChangeExtension(filename, "meta")))
                    {
                        string[] lines = File.ReadAllLines(Path.ChangeExtension(filename, "meta"));
                        foreach (string line in lines)
                        {
                            string[] parts = line.Split('\t');
                            if (parts.Length == 3)
                            {
                                if (!Groups.ContainsKey(parts[0] + "\t" + parts[1]))
                                {
                                    Groups.Add(parts[0] + "\t" + parts[1], parts[2]);
                                }
                            }
                        }
                    }

                    if (!loadOperationIsImport || spriteData == null)
                    {
                        spriteDataLocal = new SpriteData();
                    }
                    else
                    {
                        spriteDataLocal = spriteData;
                    }
                    spriteDataLocal.ReadFile(steam, Groups);
                    if (!loadOperationIsImport)
                    {
                        spriteDataLocal.SpritesDeletedEvent += new SpriteData.SpritesDeletedHandler(spriteDataLocal_SpritesDeletedEvent);
                        spriteDataLocal.SpritesAddedEvent += new SpriteData.SpritesAddedHandler(spriteData_SpritesAddedEvent);
                    }
                    loadOperationIsImport = false;
                }
            }

            spriteData = spriteDataLocal;
        }

        private void UpdateListView()
        {
            listBox1.SelectedIndex = -1;
            listBox1.DataSource = null;
            listView1.DataSource = spriteData;
            listBox1.DataSource = spriteData.Sprites;
            listView1.bind();
            //listBox1.bind();

            toolStripStatusLabel1.Text = "Ready";

            menuStrip1.Enabled = true;
            listView1.Enabled = true;
            listBox1.Enabled = true;
            propertyGrid1.Enabled = true;
            pictureBox1.Enabled = true;
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            UpdateListView();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                if (backgroundWorker1.IsBusy && backgroundWorker1.WorkerSupportsCancellation && !backgroundWorker1.CancellationPending)
                {
                    backgroundWorker1.CancelAsync();
                    e.Cancel = true;
                }
            }
        }

        private void toolStripButtonUp_Click(object sender, EventArgs e)
        {
            int newIDX = listBox1.SelectedIndex;

            listBox1.DataSource = null;

            Sprite tmp = spriteData.Sprites[newIDX];
            spriteData.Sprites[newIDX] = spriteData.Sprites[newIDX - 1];
            spriteData.Sprites[newIDX - 1] = tmp;
            
            listBox1.DataSource = spriteData.Sprites;

            listBox1.SelectedIndex = newIDX - 1;
        }

        private void toolStripButtonDown_Click(object sender, EventArgs e)
        {
            int newIDX = listBox1.SelectedIndex;

            listBox1.DataSource = null;

            Sprite tmp = spriteData.Sprites[newIDX];
            spriteData.Sprites[newIDX] = spriteData.Sprites[newIDX + 1];
            spriteData.Sprites[newIDX + 1] = tmp;

            listBox1.DataSource = spriteData.Sprites;

            listBox1.SelectedIndex = newIDX + 1;
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex > 0)
            {
                toolStripButtonUp.Enabled = true;
            }
            else
            {
                toolStripButtonUp.Enabled = false;
            }

            if (listBox1.SelectedIndex < listBox1.Items.Count - 1)
            {
                toolStripButtonDown.Enabled = true;
            }
            else
            {
                toolStripButtonDown.Enabled = false;
            }
        }

        private void saveSTBToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (spriteData != null)
            {
                if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    string filename = saveFileDialog1.FileName;
                    string filenameMeta = Path.ChangeExtension(filename, "meta");

                    using (FileStream stream = File.Open(filename, FileMode.OpenOrCreate))
                    using (FileStream streamMeta = File.Open(filenameMeta, FileMode.OpenOrCreate))
                    {
                        if (stream != null && streamMeta != null)
                        {
                            spriteData.WriteFile(stream);
                            spriteData.Sprites.ForEach(dr =>
                            {
                                if (dr.Category != null && dr.Category.Length > 0)
                                {
                                    string lineToWrite = dr.Name + "\t" + dr.Texture + "\t" + dr.Category + "\r\n";
                                    byte[] bytes = lineToWrite.ToCharArray().Select(dx => (byte)dx).ToArray();
                                    streamMeta.Write(bytes, 0, bytes.Length);
                                }
                            });
                        }
                    }
                }
            }
        }

        private void newSpriteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (spriteData == null)
            {
                spriteData = new SpriteData();
                spriteData.SpritesDeletedEvent += new SpriteData.SpritesDeletedHandler(spriteDataLocal_SpritesDeletedEvent);
                spriteData.SpritesAddedEvent += new SpriteData.SpritesAddedHandler(spriteData_SpritesAddedEvent);

                UpdateListView();
            }
            spriteData.AddNewSprite();
        }

        private void deleteSpriteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            List<Sprite> sprites = new List<Sprite>();
            foreach (ListViewItem item in listView1.SelectedItems)
            {
                sprites.Add((Sprite)item.Tag);
            }

            spriteData.RemoveItems(sprites);
        }

        public void spriteDataLocal_SpritesDeletedEvent(object sender, List<ListViewItem> listItemsToDelete)
        {
            listBox1.DataSource = null;
            listBox1.DataSource = spriteData.Sprites;
            listBox1.SelectedIndex = -1;
        }

        public void spriteData_SpritesAddedEvent(object sender, List<ListViewItem> listItemsToAdd)
        {
            listBox1.DataSource = null;
            listBox1.DataSource = spriteData.Sprites;
            listBox1.SelectedIndex = -1;
        }

        private void importSpritesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                if (File.Exists(openFileDialog1.FileName))
                {
                    menuStrip1.Enabled = false;
                    listView1.Enabled = false;
                    listBox1.Enabled = false;
                    propertyGrid1.Enabled = false;
                    pictureBox1.Enabled = false;
                    exportSpritesToolStripMenuItem.Enabled = false;

                    pictureBox1.Image = null;

                    animationTimer.Stop();
                    listView1.Clear();
                    listBox1.DataSource = null;

                    toolStripButtonUp.Enabled = false;
                    toolStripButtonDown.Enabled = false;

                    toolStripStatusLabel1.Text = "Busy";

                    loadOperationIsImport = true;
                    backgroundWorker1.RunWorkerAsync();
                }
            }
        }

        private void exportSpritesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (spriteData != null && listView1.SelectedItems.Count > 0)
            {
                List<Sprite> Sprites = new List<Sprite>();
                foreach(ListViewItem item in listView1.SelectedItems)
                {
                    Sprites.Add((Sprite)item.Tag);
                }

                if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    string filename = saveFileDialog1.FileName;
                    string filenameMeta = Path.ChangeExtension(filename, "meta");

                    using (FileStream stream = File.Open(filename, FileMode.OpenOrCreate))
                    using (FileStream streamMeta = File.Open(filenameMeta, FileMode.OpenOrCreate))
                    {
                        if (stream != null && streamMeta != null)
                        {
                            spriteData.WriteFile(stream, Sprites);
                            spriteData.Sprites.ForEach(dr =>
                            {
                                if (dr.Category != null && dr.Category.Length > 0)
                                {
                                    string lineToWrite = dr.Name + "\t" + dr.Texture + "\t" + dr.Category + "\r\n";
                                    byte[] bytes = lineToWrite.ToCharArray().Select(dx => (byte)dx).ToArray();
                                    streamMeta.Write(bytes, 0, bytes.Length);
                                }
                            });
                        }
                    }
                }
            }
        }
    }
}
