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
using Label = System.Windows.Controls.Label;

namespace CloudX.SubViews
{
    /// <summary>
    ///     FileView.xaml 的交互逻辑
    /// </summary>
    public partial class FileView
    {
        private readonly ContextMenuStrip contextMenu = new ContextMenuStrip();
        private File selectFile = new File();

        public FileView()
        {
            InitializeComponent();
            contextMenu.Items.Clear();
            contextMenu.Items.Add("删除");
            ToolStripItem tmp = contextMenu.Items[0];
            tmp.Click += deleteFileItem;

            IList<StackPanel> value = new List<StackPanel>();
            for (int i = 0; i < 10; i++)
            {
                StackPanel st = new StackPanel();
                Image img = new Image();
                //img.FindResource("/Asset/folder.png");
                st.Children.Add(img);
                Label lb = new Label();
                lb.Content = "text";
                st.Children.Add(lb);
                //bug text加入无效
                value.Add(st);
            }

            FileFolderList.ItemsSource = value;
        }

        private void RefreshFileList()
        {
            FileList.Items.Refresh();
            Console.WriteLine("FileList Refresh");
        }

        private void deleteFileItem(object sender, EventArgs e)
        {
            SampleData.FileList.Remove(selectFile);

            FileList.Items.Refresh();

            //todo uncertain
            SQLiteUtils.Delete("file", selectFile.Location + "\\" + selectFile.Name);
        }

        private void ListView_DragEnter_1(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.Link;
        }

        private bool isFileType(string name)
        {
            return true;
        }

        private void FileList_OnDrop(object sender, DragEventArgs e)
        {
            var filePath = (string[]) e.Data.GetData(DataFormats.FileDrop);
            foreach (string file in filePath)
            {
                File addFile = File.convertFileURLToFileItem(file);
                if (isFileType(addFile.Name))
                {
                    SampleData.FileList.Add(addFile);

                    SQLiteUtils.Insert("file", file);

                    RefreshFileList();
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
            selectFile = (File) e.AddedItems[0];
        }

        private void FileList_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            String startUrl = selectFile.Location + "//" + selectFile.Name;

            try
            {
                Process.Start(startUrl);
            }
            catch (Exception)
            {
                MainWindow.ShowMessageBox("确定", "取消", "注意：", "本设备上找不到该文件！");
            }
        }

        private void FileList_OnMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (FileList.SelectedItems.Count != 0)
            {
                Point point = e.MouseDevice.GetPosition(FileList) - (Vector) FileList.PointFromScreen(new Point(0, 0));
                contextMenu.Show((int) point.X, (int) point.Y);
            }
        }

        private Image GetFileSubImage(string fileUrl)
        {
            //todo
            throw new NotImplementedException();
        }
    }
}