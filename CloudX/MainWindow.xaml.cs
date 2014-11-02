using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using CloudX.fileutils;
using CloudX.SubViews;
using CloudX.SubWindows;
using CloudX.utils;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using Application = System.Windows.Application;
using Clipboard = System.Windows.Clipboard;
using DataFormats = System.Windows.DataFormats;
using DragDropEffects = System.Windows.DragDropEffects;
using DragEventArgs = System.Windows.DragEventArgs;
using MessageBox = System.Windows.MessageBox;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace CloudX
{
    /// <summary>
    ///     MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow
    {
        public static DelegateDeclarations.PromptToSaveFile PromptToSave;

        public static DelegateDeclarations.PopupMessageBox ShowMessageBox;

        public static DelegateDeclarations.RefreshDeviceUI RefreshDeviceList;

        public static DelegateDeclarations.RefreshProgressUI UpdateProgress;
        private readonly AudioServer audioServer;

        private readonly ContextMenuStrip contextMenu = new ContextMenuStrip();
        private readonly List<Device> deviceList = new List<Device>();
        private readonly GameView gameView = new GameView();
        private readonly MainView mainView = new MainView();
        private readonly MovieView movieView = new MovieView();
        private readonly MusicView musicView = new MusicView();

        private readonly RemoteDesktopServer remoteDesktopServer;
        private int currentDeviceCount;
        private Device currentSelectDevice = new Device();
        private int currentSelectDeviceIndex;
        private double progressValue;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainWindowViewModel();

            var mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow != null)
            {
                StackPanel stackPanel = mainWindow.RightView;
                stackPanel.Children.Add(mainView);
            }
            tablControl.Visibility = Visibility.Hidden;

            //initialize context menu
            contextMenu.Items.Clear();
            contextMenu.Items.Add("发送消息");
            // contextMenu.Items.Add("发送文件");
            contextMenu.Items.Add("查找我的设备");
            contextMenu.Items[0].Click += ShowInputDialog;
            //  contextMenu.Items[1].Click += SendFiles;
            contextMenu.Items[1].Click += FindMyDevice;

            //initialize device interface
            currentDeviceCount = 0;
            currentSelectDeviceIndex = 0;

            deviceList.Add(new Device(DevicePanel1, DeviceNameBlock1, ""));
            deviceList.Add(new Device(DevicePanel2, DeviceNameBlock2, ""));
            deviceList.Add(new Device(DevicePanel3, DeviceNameBlock3, ""));

            //挂载
            ShowMessageBox = ShowMessageDialog;
            RefreshDeviceList = RefreshDevices;
            UpdateProgress = RefreshProgressValue;
            PromptToSave = promptToSaveFile;

            progressValue = 0.0;

            #region Start Servers

            try
            {
                remoteDesktopServer = new RemoteDesktopServer(Dispatcher);
                audioServer = new AudioServer();
                new Thread(remoteDesktopServer.Start).Start();
                new Thread(audioServer.Start).Start();
            }
            catch (Exception e)
            {
                //todo give some notifications
            }

            #endregion
        }

        public void RefreshDevices()
        {
            foreach (Device curDevice in deviceList)
            {
                curDevice.photo.Visibility = Visibility.Hidden;
                curDevice.deviceName.Visibility = Visibility.Hidden;
            }
            currentDeviceCount = 0;
            foreach (var entry in Client.ClientDictionary)
            {
                ClientInfo info = entry.Value;
                if (info != null)
                {
                    Device device = deviceList[currentDeviceCount];
                    currentDeviceCount++;
                    device.deviceName.Text = info.ClientName;
                    device.photo.Visibility = Visibility.Visible;
                    device.deviceName.Visibility = Visibility.Visible;
                    device.ip = info.ClientIP;
                }
            }
        }

        private void showMessageBox(string message)
        {
            MessageBox.Show(message);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            if (remoteDesktopServer != null)
                remoteDesktopServer.Finish();
            if (audioServer != null)
                audioServer.Finish();
        }

        public void CheckRightViewContains(StackPanel stackPanel)
        {
            if (stackPanel.Children.Contains(mainView)) stackPanel.Children.Remove(mainView);
            if (stackPanel.Children.Contains(musicView)) stackPanel.Children.Remove(musicView);
            if (stackPanel.Children.Contains(movieView)) stackPanel.Children.Remove(movieView);
            if (stackPanel.Children.Contains(gameView)) stackPanel.Children.Remove(gameView);
        }

        public void ClickedMovieButton(object sender, RoutedEventArgs e)
        {
            StackPanel stackPanel = (Application.Current.MainWindow as MainWindow).RightView;
            CheckRightViewContains(stackPanel);
            stackPanel.Children.Add(movieView);
        }

        public void ClickedMusicButton(object sender, RoutedEventArgs e)
        {
            StackPanel stackPanel = (Application.Current.MainWindow as MainWindow).RightView;
            CheckRightViewContains(stackPanel);
            stackPanel.Children.Add(musicView);
        }

        public void ClickedGameButton(object sender, RoutedEventArgs e)
        {
            StackPanel stackPanel = (Application.Current.MainWindow as MainWindow).RightView;
            CheckRightViewContains(stackPanel);
            stackPanel.Children.Add(gameView);
        }

        public void ClickedMainButton(object sender, RoutedEventArgs e)
        {
            StackPanel stackPanel = (Application.Current.MainWindow as MainWindow).RightView;
            CheckRightViewContains(stackPanel);
            stackPanel.Children.Add(mainView);
            tablControl.Visibility = Visibility.Hidden;
        }

        private void ShowBottom(object sender, RoutedEventArgs e)
        {
            ToggleFlyout(0);
        }

        private void ToggleFlyout(int index)
        {
            var flyout = Flyouts.Items[index] as Flyout;
            if (flyout == null)
            {
                return;
            }

            flyout.IsOpen = !flyout.IsOpen;
        }

        private void SettingButtonClicked(object sender, RoutedEventArgs e)
        {
            string ip = IPUtils.GetHostIP();
            IpButton.Content = ip;
        }

        private void ShowAboutUs(object sender, RoutedEventArgs e)
        {
            var aboutUsWindow = new AboutUsWindow();
            aboutUsWindow.Show();
        }

        private void ClickOnContextMenu(object sender, MouseButtonEventArgs e)
        {
            Point point = e.MouseDevice.GetPosition(currentSelectDevice.photo) -
                          (Vector) currentSelectDevice.photo.PointFromScreen(new Point(0, 0));
            contextMenu.Show((int) point.X, (int) point.Y);
        }

        private async void ShowInputDialog(object sender, EventArgs e)
        {
            string result = await this.ShowInputAsync("To : " + currentSelectDevice.deviceName.Text, "发送消息");

            if (result == null) //user pressed cancel
                return;

            //给设备发送消息
            Dispatcher
                .BeginInvoke(Client.SendMessageToDevice, currentSelectDevice.ip, result);

            await this.ShowMessageAsync("成功发送消息", result + "!");
        }


        public async void ShowMessageDialog(string buttonMessage1, string buttonMessage2, string firstMessage,
            string secondMessage)
        {
            // This demo runs on .Net 4.0, but we're using the Microsoft.Bcl.Async package so we have async/await support
            // The package is only used by the demo and not a dependency of the library!

            var mySettings = new MetroDialogSettings
            {
                AffirmativeButtonText = buttonMessage1,
                NegativeButtonText = buttonMessage2,
            };

            MessageDialogResult result = await this.ShowMessageAsync(firstMessage, secondMessage,
                MessageDialogStyle.AffirmativeAndNegative, mySettings);

            if (result == MessageDialogResult.Affirmative)
            {
                Clipboard.SetDataObject(secondMessage);
            }
        }

        public void RefreshProgressValue(double value)
        {
            progressValue = value;
        }

        private void ClickOnDevice1(object sender, RoutedEventArgs e)
        {
        }

        private void ClickOnDevice2(object sender, RoutedEventArgs e)
        {
        }

        private void ClickOnDevice3(object sender, RoutedEventArgs e)
        {
        }

        private void SendFiles(object sender, EventArgs e)
        {
        }

        private void FindMyDevice(object sender, EventArgs e)
        {
            new DeviceFinder(currentSelectDevice.ip, Dispatcher).BeginFind();
            //todo popup window
        }

        private void ChangeSelectDevice1(object sender, EventArgs e)
        {
            currentSelectDeviceIndex = 1;
            currentSelectDevice = deviceList[currentSelectDeviceIndex - 1];
        }

        private void ChangeSelectDevice2(object sender, EventArgs e)
        {
            currentSelectDeviceIndex = 2;
            currentSelectDevice = deviceList[currentSelectDeviceIndex - 1];
        }

        private void ChangeSelectDevice3(object sender, EventArgs e)
        {
            currentSelectDeviceIndex = 3;
            currentSelectDevice = deviceList[currentSelectDeviceIndex - 1];
        }

        private void ChangeSelectDevice4(object sender, EventArgs e)
        {
            currentSelectDeviceIndex = 4;
            currentSelectDevice = deviceList[currentSelectDeviceIndex - 1];
        }

        private void ToAllButtonOnMouseEnter(object sender, MouseEventArgs e)
        {
            currentSelectDeviceIndex = 0;
        }

        private void ToALLButtonOnClick(object sender, RoutedEventArgs e)
        {
            ShowInputDialog(sender, e);
        }

        private void DevicesDragEnter(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.Link;
        }

        private async void ShowProgressDialog()
        {
            ProgressDialogController controller = await this.ShowProgressAsync("请稍等...", "文件正在传输");

            await Task.Delay(1000);

            controller.SetCancelable(true);

            double i = 0.0;
            double val = 0;
            while (val < 1.0)
            {
                //更新传输进度
                val = progressValue;

                controller.SetProgress(val);

                controller.SetMessage("已完成 " + (val*100).ToString("0") + "%" + "...");

                if (controller.IsCanceled)
                    break; //canceled progressdialog auto closes.

                i += 1.0;

                await Task.Delay(1000);
            }

            await controller.CloseAsync();

            if (controller.IsCanceled)
            {
                await this.ShowMessageAsync("文件传输中断！", "已被取消");
            }
            else
            {
                await
                    this.ShowMessageAsync("传输完成",
                        currentSelectDevice == null ? "" : currentSelectDevice.deviceName.Text + " 已经接收到文件！");
            }
            progressValue = 0;
        }

        private void DeviceFileOnDrop(object sender, DragEventArgs e)
        {
            if (currentSelectDeviceIndex != 0)
            {
                var filePath = (string[]) e.Data.GetData(DataFormats.FileDrop);
                foreach (string file in filePath)
                {
                    //send files to devices
                    new FileSender(currentSelectDevice.ip, file, Dispatcher).BeginSend();

                    ShowProgressDialog();
                }
            }
            else
            {
                for (int i = 0; i < currentDeviceCount; i++)
                {
                    currentSelectDevice = deviceList[i];
                    var filePath = (string[]) e.Data.GetData(DataFormats.FileDrop);
                    foreach (string file in filePath)
                    {
                        //send files to devices
                        new FileSender(currentSelectDevice.ip, file, Dispatcher).BeginSend();

                        ShowProgressDialog();
                    }
                }
            }
        }

        private void ReceiveFiles()
        {
            //todo receive files to PC
        }

        private void FileManageButtonOnClick(object sender, RoutedEventArgs e)
        {
            var mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow != null)
            {
                StackPanel stackPanel = mainWindow.RightView;
                CheckRightViewContains(stackPanel);
            }
            tablControl.Visibility = Visibility.Visible;
        }

        private void promptToSaveFile(string fileName, Stream stream)
        {
            //var saveFileDialog = new SaveFileDialog();

            //saveFileDialog.CreatePrompt = true;

            //saveFileDialog.OverwritePrompt = true;
            //saveFileDialog.AddExtension = true;

            //saveFileDialog.FileName = fileName;

            //if (saveFileDialog.ShowDialog() == true)
            //{
                ////加入文件列表                
                //Console.WriteLine(saveFileDialog.FileName);
                ////save file
                //new FileReceiver(saveFileDialog.FileName, stream, Dispatcher).BeginReceive();
            //}

            new FileReceiver("C:\\CloudXDownloads\\"+ fileName, stream, Dispatcher).BeginReceive();
        }

        private class Device
        {
            public readonly TextBlock deviceName;
            public readonly StackPanel photo;
            public string ip;

            public Device()
            {
            }

            public Device(StackPanel myPhoto, TextBlock myDeviceName, string myIp)
            {
                photo = myPhoto;
                deviceName = myDeviceName;
                ip = myIp;
            }
        }
    }
}