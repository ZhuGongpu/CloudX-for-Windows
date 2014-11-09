using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Reflection.Emit;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using CloudX.Models;
using CloudX.utils;
using SharpDX.Multimedia;
using DataFormats = System.Windows.DataFormats;
using DragDropEffects = System.Windows.DragDropEffects;
using DragEventArgs = System.Windows.DragEventArgs;
using Image = System.Windows.Controls.Image;
using Label = System.Windows.Controls.Label;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Point = System.Windows.Point;

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
            tmp.Click += DeleteFileItem;
        }

        void RefreshFileList()
        {
            FileListBox.Items.Refresh();
            Console.WriteLine("FileList Refreshed");
        }
        private void DeleteFileItem(object sender, EventArgs e)
        {
            selectFile = (File)FileListBox.SelectedItem;

            SampleData.FileList.Remove(selectFile);

            RefreshFileList();
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

        private Image GetFileSubImage(string fileUrl)
        {
            //todo
            throw new NotImplementedException();
        }

        private void FileListBox_OnDrop(object sender, DragEventArgs e)
        {
            var filePath = (string[])e.Data.GetData(DataFormats.FileDrop);
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


        private void TextBox_OnMouseEnter(object sender, MouseEventArgs e)
        {
            Console.WriteLine(((TextBlock)sender).Text);
        }

        private void FileImage_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount != 2) return;
            int fileTag = (int)((Image) sender).Tag;
            File openedFile = null;
            foreach (var file in SampleData.FileList)
            {
                if (file.fileTag == fileTag)
                {
                    openedFile = file;
                    break;
                }
            }
            try
            {
                Console.WriteLine(openedFile.Location.ToString());
            }
            catch (Exception)
            {
                throw;
            }

            String startUrl = openedFile.Location + "\\" + openedFile.Name;

            Console.WriteLine(startUrl);
            if (openedFile.Location == null)
            {
                return;
            }
            try
            {
                Process.Start(startUrl);
            }
            catch (Exception)
            {
                MainWindow.ShowMessageBox("确定", "取消", "注意：", "本设备上找不到该文件！");
            }
        }

        private void FileImage_OnMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (FileListBox.SelectedItems.Count != 0)
            {
                Point point = e.MouseDevice.GetPosition(FileListBox) - (Vector)FileListBox.PointFromScreen(new Point(0, 0));
                contextMenu.Show((int)point.X, (int)point.Y);
            }
        }

    }
}