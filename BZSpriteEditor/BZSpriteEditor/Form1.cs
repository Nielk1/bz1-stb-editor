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
using System.Drawing.Imaging;

namespace BZSpriteEditor
{
    public partial class Form1 : Form
    {
        public static QueuedBackgroundWorker BackgroundQueueWorker = new QueuedBackgroundWorker();

        SpriteDataViewModel spriteDataViewModel;
        List<Sprite> AnimationCells = new List<Sprite>();
        int animationIndex = 0;
        bool loadOperationIsImport = false;
        bool PictureBoxWhite = false;

        //QueuedBackgroundWorker BackgroundQueueWorker;

        public Form1()
        {
            InitializeComponent();

            //BackgroundQueueWorker = new QueuedBackgroundWorker();
            spriteDataViewModel = new SpriteDataViewModel();
            listView1.DataSource = spriteDataViewModel;
            listView1.bind();

            spriteDataViewModel.RefreshEntireListEvent += new SpriteDataViewModel.RefreshEntireListHandler(spriteData_RefreshEntireListEvent);

            listView1.ListItemImageChangedEvent += new SpriteListView.ListItemImageChangedHandler(listView1_ListItemImageChanged);
        }

        private void SetInterfaceStateBusy(bool FullReset)
        {
            menuStrip1.Enabled = false;
            listView1.Enabled = false;
            listBox1.Enabled = false;
            propertyGrid1.Enabled = false;
            pictureBox1.Enabled = false;
            exportSpritesToolStripMenuItem.Enabled = false;

            pictureBox1.Image = null;

            animationTimer.Stop();

            if (FullReset)
            {
                listView1.Clear();
                listBox1.DataSource = null;
            }

            toolStripButtonUp.Enabled = false;
            toolStripButtonDown.Enabled = false;

            toolStripStatusLabel1.Text = "Busy";
        }

        private void openSTBToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                if (File.Exists(openFileDialog1.FileName))
                {
                    SetInterfaceStateBusy(true);

                    //backgroundWorker1.RunWorkerAsync();
                    BackgroundQueueWorker.RunAsync(
                        (obj, args) =>
                        {
                            BackgroundWorker wrkr = ((BackgroundWorker)obj);

                            spriteDataViewModel.OpenSpriteData(openFileDialog1.FileName);
                        },
                        (obj, args) =>
                        {
                            UpdateListView();
                        },
                        //(obj, args) =>
                        //{
                        //    
                        //}
                        null
                    );
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
                pictureBox1.Image = listView1.GetFullImage((Sprite)(listView1.SelectedItems[0].Tag));
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
                        AnimationCells.Add((Sprite)(item.Tag));
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
                    pictureBox1.Image = listView1.GetFullImage((Sprite)(listView1.SelectedItems[0].Tag));
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
                pictureBox1.Image = listView1.GetFullImage(AnimationCells[animationIndex]);
                animationIndex++;
            }
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            string filename = openFileDialog1.FileName;

            SpriteDataViewModel spriteDataLocal = null;

            /*using (FileStream steam = File.Open(filename, FileMode.Open))
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
                        spriteDataLocal = new SpriteDataViewModel();
                    }
                    else
                    {
                        spriteDataLocal = spriteData;
                    }
                    spriteDataLocal.ReadFile(steam, Groups);
                    if (!loadOperationIsImport)
                    {
                        spriteDataLocal.SpritesDeletedEvent += new SpriteDataViewModel.SpritesDeletedHandler(spriteDataLocal_SpritesDeletedEvent);
                        spriteDataLocal.SpritesAddedEvent += new SpriteDataViewModel.SpritesAddedHandler(spriteData_SpritesAddedEvent);
                    }
                    loadOperationIsImport = false;
                }
            }*/

            spriteDataViewModel = spriteDataLocal;
        }

        private void UpdateListView()
        {
            listBox1.SelectedIndex = -1;
//            listBox1.DataSource = null;
//            listView1.DataSource = spriteData;
//            listBox1.DataSource = spriteData.Sprites;
//            listView1.bind();

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

            /*Sprite tmp = spriteData.Sprites[newIDX];
            spriteData.Sprites[newIDX] = spriteData.Sprites[newIDX - 1];
            spriteData.Sprites[newIDX - 1] = tmp;
            
            listBox1.DataSource = spriteData.Sprites;*/

            listBox1.SelectedIndex = newIDX - 1;
        }

        private void toolStripButtonDown_Click(object sender, EventArgs e)
        {
            int newIDX = listBox1.SelectedIndex;

            listBox1.DataSource = null;

            /*Sprite tmp = spriteData.Sprites[newIDX];
            spriteData.Sprites[newIDX] = spriteData.Sprites[newIDX + 1];
            spriteData.Sprites[newIDX + 1] = tmp;

            listBox1.DataSource = spriteData.Sprites;*/

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
            if (spriteDataViewModel != null)
            {
                if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    string filename = saveFileDialog1.FileName;

                    SetInterfaceStateBusy(false);

                    //backgroundWorker1.RunWorkerAsync();
                    BackgroundQueueWorker.RunAsync(
                        (obj, args) =>
                        {
                            BackgroundWorker wrkr = ((BackgroundWorker)obj);

                            spriteDataViewModel.SaveSpriteData(filename);
                        },
                        (obj, args) =>
                        {
                            UpdateListView();
                        },
                        //(obj, args) =>
                        //{
                        //    
                        //}
                        null
                    );
                }
            }
        }

        private void newSpriteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            /*if (spriteData == null)
            {
                spriteData = new SpriteDataViewModel();
                spriteData.SpritesDeletedEvent += new SpriteDataViewModel.SpritesDeletedHandler(spriteDataLocal_SpritesDeletedEvent);
                spriteData.SpritesAddedEvent += new SpriteDataViewModel.SpritesAddedHandler(spriteData_SpritesAddedEvent);

                UpdateListView();
            }
            spriteData.AddNewSprite();*/
            spriteDataViewModel.AddNewSprite();
        }

        private void deleteSpriteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            /*List<Sprite> sprites = new List<Sprite>();
            foreach (ListViewItem item in listView1.SelectedItems)
            {
                sprites.Add((Sprite)item.Tag);
            }

            spriteData.RemoveItems(sprites);*/
        }

        public void spriteDataLocal_SpritesDeletedEvent(object sender, List<ListViewItem> listItemsToDelete)
        {
            listBox1.DataSource = null;
            //listBox1.DataSource = spriteData.Sprites;
            listBox1.SelectedIndex = -1;
        }

        public void spriteData_SpritesAddedEvent(object sender, List<ListViewItem> listItemsToAdd)
        {
            listBox1.DataSource = null;
            //listBox1.DataSource = spriteData.Sprites;
            listBox1.SelectedIndex = -1;
        }

        private void importSpritesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                if (File.Exists(openFileDialog1.FileName))
                {
                    SetInterfaceStateBusy(false);

                    //backgroundWorker1.RunWorkerAsync();
                    BackgroundQueueWorker.RunAsync(
                        (obj, args) =>
                        {
                            BackgroundWorker wrkr = ((BackgroundWorker)obj);

                            spriteDataViewModel.ImportSpriteData(openFileDialog1.FileName);
                        },
                        (obj, args) =>
                        {
                            UpdateListView();
                        },
                        //(obj, args) =>
                        //{
                        //    
                        //}
                        null
                    );
                }
            }
        }

        private void exportSpritesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (spriteDataViewModel != null && listView1.SelectedItems.Count > 0)
            {
                List<Sprite> Sprites = new List<Sprite>();
                foreach(ListViewItem item in listView1.SelectedItems)
                {
                    Sprites.Add((Sprite)item.Tag);
                }

                if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    string filename = saveFileDialog1.FileName;
                    /*string filenameMeta = Path.ChangeExtension(filename, "meta");

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
                    }*/
                }
            }
        }

        public void spriteData_RefreshEntireListEvent(object sender)
        {
            
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            if (PictureBoxWhite)
            {
                pictureBox1.BackgroundImage = Properties.Resources.bg;
            }
            else
            {
                pictureBox1.BackgroundImage = Properties.Resources.bg2;
            }
            PictureBoxWhite = !PictureBoxWhite;
        }
    }
}
