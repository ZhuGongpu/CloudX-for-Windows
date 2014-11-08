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
            tmp.Click += deleteFileItem;
        }

        void RefreshFileList()
        {
            FileFolderScrollViewer.Items.Refresh();
            Console.WriteLine("FileList Refreshed");
        }
        private void deleteFileItem(object sender, EventArgs e)
        {
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

        private void FileFolderScrollViewer_OnDrop(object sender, DragEventArgs e)
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

        private void UIElement_OnMouseEnter(object sender, MouseEventArgs e)
        {
            Console.WriteLine(FileFolderScrollViewer.Items.GetItemAt(0).GetType().ToString());
        }

        private void TextBox_OnMouseEnter(object sender, MouseEventArgs e)
        {
            Console.WriteLine(((TextBlock)sender).Text);
        }

        private void FileImage_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
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
        }
    }
}