using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using CloudX.Models;
using CloudX.utils;
using DataFormats = System.Windows.DataFormats;
using DragDropEffects = System.Windows.DragDropEffects;
using DragEventArgs = System.Windows.DragEventArgs;
using UserControl = System.Windows.Controls.UserControl;

namespace CloudX.SubViews
{
    /// <summary>
    ///     MusicView.xaml 的交互逻辑
    /// </summary>
    public partial class MusicView : UserControl
    {
        public static List<string> MusicTypeList = new List<string>();
        private readonly ContextMenuStrip contextMenu = new ContextMenuStrip();
        private Music selectMusic = new Music();

        public MusicView()
        {
            InitializeComponent();
            contextMenu.Items.Clear();
            contextMenu.Items.Add("删除");
            ToolStripItem tmp = contextMenu.Items[0];
            tmp.Click += deleteMusicItem;

            string[] musicTypeList = {"mp3", "wav", "wma", "aac", "asf", "ogg", "m4a", "flac", "ape", "mod", "aiff"};
            for (int i = 0; i < musicTypeList.Length; i++)
                MusicTypeList.Add(musicTypeList[i]);
        }

        private void deleteMusicItem(object sender, EventArgs e)
        {
            SampleData.Albums.Remove(selectMusic);

            MusicList.Items.Refresh();

            //todo uncertain
            SQLiteUtils.Delete("music", selectMusic.Location + "\\" + selectMusic.Name);
        }

        private void ListView_DragEnter_1(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effects = DragDropEffects.Link;
            else e.Effects = DragDropEffects.None;
        }

        private Music convertFileURLToMusicItem(string url)
        {
            int len = url.Length, dividePoint = 0;
            bool findName = false;
            for (int i = len - 1; i >= 0; i--)
            {
                if (url[i] == '\\' && !findName)
                {
                    dividePoint = i;
                    break;
                }
            }
            string Locate = url.Substring(0, dividePoint);
            string Artist = "";
            string Name = url.Substring(dividePoint + 1, len - dividePoint - 1);
            var addMusic = new Music {Artist = Artist, Location = Locate, Name = Name};
            return addMusic;
        }

        private bool isMusicType(string name)
        {
            int len = name.Length, p = 0;
            for (int i = len - 1; i >= 0; i--)
            {
                if (name[i] == '.')
                {
                    p = i;
                    break;
                }
            }

            String type = name.Substring(p + 1, len - p - 1);

            foreach (string MusicType in MusicTypeList)
            {
                if (type == MusicType) return true;
            }
            return false;
        }

        private void MusicList_OnDrop(object sender, DragEventArgs e)
        {
            var filePath = (string[]) e.Data.GetData(DataFormats.FileDrop);
            foreach (string file in filePath)
            {
                Music addMusic = convertFileURLToMusicItem(file);
                if (isMusicType(addMusic.Name))
                {
                    SampleData.Albums.Add(addMusic);

                    SQLiteUtils.Insert("music", file);

                    MusicList.Items.Refresh();
                }
                else
                {
                    MainWindow.ShowMessageBox("确定", "取消", "注意：", "这个文件应该不是添加在这里的哟！");
                }
            }
        }

        private void ListView_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0) return;
            selectMusic = (Music) e.AddedItems[0];
        }

        private void MusicList_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            String startUrl = selectMusic.Location + "//" + selectMusic.Name;
            try
            {
                Process.Start(startUrl);
            }
            catch (Exception)
            {
                MainWindow.ShowMessageBox("确定", "取消", "注意：", "本设备上找不到该文件！");
            }
        }

        private void MusicList_OnMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (MusicList.SelectedItems.Count != 0)
            {
                Point point = e.MouseDevice.GetPosition(MusicList) - (Vector) MusicList.PointFromScreen(new Point(0, 0));
                contextMenu.Show((int) point.X, (int) point.Y);
            }
        }
    }
}