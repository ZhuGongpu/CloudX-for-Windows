using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using CloudX.Models;
using CloudX.utils;
using DataFormats = System.Windows.DataFormats;
using DragDropEffects = System.Windows.DragDropEffects;
using DragEventArgs = System.Windows.DragEventArgs;
using Label = System.Windows.Controls.Label;

namespace CloudX.SubViews
{
    /// <summary>
    ///     MovieView.xaml 的交互逻辑
    /// </summary>
    public partial class MovieView
    {
        public static List<string> MovieTypeList = new List<string>();
        private readonly ContextMenuStrip contextMenu = new ContextMenuStrip();
        private Movie selectMovie = new Movie();

        public MovieView()
        {
            InitializeComponent();
            contextMenu.Items.Clear();
            contextMenu.Items.Add("删除");
            ToolStripItem tmp = contextMenu.Items[0];
            tmp.Click += deleteMovieItem;

            string[] movieTypeList = {"mkv", "rmvb", "flv", "mp4", "avi", "f4v", "mov", "wmv", "ram", "3gp", "rm"};
            for (int i = 0; i < movieTypeList.Length; i ++)
                MovieTypeList.Add(movieTypeList[i]);

            /////New
            //string id = TB_CollectorID.Text.Trim();

            IList<double> values = new List<double>();
            for (int i = 0; i < 40 ;++i )  values.Add(i);
            MovieFolderList.ItemsSource = values;
        }

        private void RefreshMovieList()
        {
            MovieList.Items.Refresh();
            Console.WriteLine("MovieList Refresh");
        }

        private void deleteMovieItem(object sender, EventArgs e)
        {
          
            SampleData.Artists.Remove(selectMovie);

            selectMovie.Location = null;

            MovieList.Items.Refresh();

            //todo uncertain
            SQLiteUtils.Delete("movie", selectMovie.Location + "\\" + selectMovie.Name);
        }

        private void ListView_DragEnter_1(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.Link;
        }

        private Movie convertFileURLToMovieItem(string url)
        {
            int len = url.Length, dividePoint = 0;
            bool findName = false;
            for (int i = len - 1; i >= 0; i --)
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
            var addMovie = new Movie {Artist = Artist, Location = Locate, Name = Name};
            return addMovie;
        }

        private bool isMovieType(string name)
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

            foreach (string movieType in MovieTypeList)
            {
                if (type == movieType) return true;
            }
            return false;
        }

        private void MovieList_OnDrop(object sender, DragEventArgs e)
        {
            var filePath = (string[]) e.Data.GetData(DataFormats.FileDrop);
            foreach (string file in filePath)
            {
                Movie addMovie = convertFileURLToMovieItem(file);
                if (isMovieType(addMovie.Name))
                {
                    SampleData.Artists.Add(addMovie);

                    SQLiteUtils.Insert("movie", file);

                    MovieList.Items.Refresh();
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
            selectMovie = (Movie) e.AddedItems[0];
        }

        private void MovieList_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            String startUrl = selectMovie.Location + "//" + selectMovie.Name;

            if (selectMovie.Location == null)
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

        private void MovieList_OnMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (MovieList.SelectedItems.Count != 0)
            {
                Point point = e.MouseDevice.GetPosition(MovieList) - (Vector) MovieList.PointFromScreen(new Point(0, 0));
                contextMenu.Show((int) point.X, (int) point.Y);
            }
        }

    }
}