using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Security;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Threading;
using CloudX.DesktopDuplication;
using CloudX.Models;
using CloudX.network;
using CloudX.utils;
using common.message;

namespace CloudX.Client
{
    internal class Client
    {
        public delegate void MessageSender(string targetIP, string message);

        private static int MinTimeGapBetweenClicks = 300;

        public static Dictionary<String, ClientInfo> ClientDictionary = new Dictionary<string, ClientInfo>();
        public static MessageSender SendMessageToDevice = messageSender;

        private readonly string clientIP;

        private readonly Dispatcher dispatcher;
        private readonly Thread inputThread;
        private int ClientScreenHeight;
        private int ClientScreenWidth;

        private int ConnectionFailedTimes;
        private bool NeedToSendFrame = true; //标记是否需要传输完整帧

        private bool isWindowSelected;
        private DateTime lastLeftClickTime = DateTime.Now;
        private DateTime lastRightClickTime = DateTime.Now;
        private float scaleRate = 1;
        private int selectedWindowHwnd;
        private Stream stream;
        private Thread videoSenderThread;


        public Client(Stream stream, string clientIP, Dispatcher dispatcher)
        {
            this.stream = stream;
            this.clientIP = clientIP;
            this.dispatcher = dispatcher;

            inputThread = new Thread(DataReceiver);
        }

        public void Start()
        {
            try
            {
                inputThread.Start();
            }
            catch (Exception e)
            {
                Console.WriteLine("Client Start " + e);
                Finish();
            }
        }

        /// <summary>
        ///     处理所有接收的输入
        /// </summary>
        private void DataReceiver()
        {
            Console.WriteLine("data receiver online");

            if (stream == null) return;
            while (stream != null)
            {
                DataPacket dataPacket = null;

                try
                {
                    dataPacket = DataPacket.ParseDelimitedFrom(stream);
                    if (dataPacket == null) continue;
                    switch (dataPacket.DataPacketType)
                    {
                        case DataPacket.Types.DataPacketType.DeviceInfo:
                            //TODO
                            break;
                        case DataPacket.Types.DataPacketType.Command:
                            CommandProcessor(dataPacket.Command);
                            break;
                        case DataPacket.Types.DataPacketType.FileRequest:
                            //TODO
                            break;
                        case DataPacket.Types.DataPacketType.KeyboardEvent:
                            KeyBoardEventProcessor(dataPacket.KeyboardEvent);
                            break;
                        case DataPacket.Types.DataPacketType.SharedMessage:
                            SharedMessageProcessor(dataPacket.SharedMessage);
                            break;
                        case DataPacket.Types.DataPacketType.FileInfo:
                            //TODO
                            break;
                        default:
                            break;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Client dataReceiver  " + (dataPacket == null) + "  " + e);
                    ConnectionFailedHandler();
                }
            }

            Finish();
        }

        //public void ReceiveFile(string filePath)
        //{
        //    var file = new FileStream(filePath, FileMode.Create);

        //    Console.WriteLine("FileReceiver : ReceiveFile");
        //    long receivedSize = 0;
        //    while (true)
        //    {
        //        try
        //        {
        //            DataPacket dataPacket = DataPacket.ParseDelimitedFrom(stream);
        //            if (dataPacket.HasFileBlock)
        //            {
        //                Console.WriteLine("FileReceiver : ReceiveFile {0} / {1}", receivedSize,
        //                    dataPacket.file.FileLength);

        //                if (receivedSize >= dataPacket.SharedFile.FileLength)
        //                {
        //                    file.Close();
        //                    Console.WriteLine("File Transimission Done");
        //                    return;
        //                }

        //                byte[] buffer = dataPacket.SharedFile.Content.ToByteArray();
        //                file.Write(buffer, 0, buffer.Length);
        //                file.Flush();

        //                receivedSize += buffer.Length;

        //                Console.WriteLine("FileReceiver : ReceiveFile {0} / {1}", receivedSize,
        //                    dataPacket.SharedFile.FileLength);


        //                //todo notify ui
        //                //dispatcher.BeginInvoke(MainWindow.UpdateProgress, (double)receivedSize / dataPacket.SharedFile.FileLength);
        //            }
        //        }
        //        catch (Exception)
        //        {
        //            //todo notify UI
        //            //dispatcher.BeginInvoke(MainWindow.ShowMessageBox, null, null, "文件传输已中断", null);

        //            Console.WriteLine("File Transimission Failed");

        //            break;
        //        }
        //    }

        //    file.Close();
        //    Console.WriteLine("File Transimission Done");
        //}

        public void Finish()
        {
            try
            {
                if (inputThread != null && inputThread.IsAlive)
                    inputThread.Abort();

                if (videoSenderThread != null && videoSenderThread.IsAlive)
                    videoSenderThread.Abort();
            }
            catch (SecurityException exception)
            {
                Console.WriteLine("Client Finish " + exception);
            }
            catch (ThreadStateException exception)
            {
                Console.WriteLine("Client Finish " + exception);
            }
            catch (Exception e)
            {
                Console.WriteLine("Client Finish " + e);
            }
            finally
            {
                if (stream != null)
                    stream.Dispose();

                stream = null;
            }
        }

        /// <summary>
        ///     用于在catch中解决网络异常
        /// </summary>
        private void ConnectionFailedHandler()
        {
            ConnectionFailedTimes++;
            if (ConnectionFailedTimes > 3)
                Finish();
        }

        #region DataPacket Sender

        private void VideoSender()
        {
            int frameCounter = 0; //当其为0时，发送完整帧数据
            const int frameCounterThreshold = 10; //frameCounter达到这个值之后自动清零，并发送完整帧数据
            while (stream != null)
            {
                // 暂时不区分是否有窗口选中

                FrameData frameData = null;

                if (NeedToSendFrame || frameCounter == 0)
                {
                    NeedToSendFrame = false;
                    DuplicationManager.GetInstance().GetFrame(out frameData);
                }
                else
                    DuplicationManager.GetInstance().GetChangedRects(ref frameData);

                frameData.WriteToStream(stream);

                frameCounter = (frameCounter + 1)%frameCounterThreshold;
            }
        }

        /// <summary>
        ///     向targetIP的avtive input发送消息
        /// </summary>
        /// <param name="targetIP"></param>
        /// <param name="message"></param>
        private static void messageSender(string targetIP, string message)
        {
            if (ClientDictionary[targetIP] != null)
            {
                var tcpClient = new TcpClient(targetIP, 50323);

                if (tcpClient.Connected)
                {
                    ProtoBufHelper.WriteMessage(tcpClient.GetStream(), message);
                    tcpClient.Close();
                }
            }
        }

        #endregion

        #region DataPacket Processor

        private AudioSender audioSender;

        private void SharedMessageProcessor(SharedMessage message)
        {
            //notify UI
            dispatcher.BeginInvoke(MainWindow.ShowMessageBox
                , "复制", "取消", "收到消息：", DataTypeConverter.ByteStringToString(message.Content));

            Console.WriteLine("ReceiveFile a message : "
                              + DataTypeConverter.ByteStringToString(message.Content));

            //send the message to all
            foreach (var entry in ClientDictionary)
            {
                if (!entry.Key.Equals(clientIP))
                {
                    messageSender(entry.Key, DataTypeConverter.ByteStringToString(message.Content));
                }
            }
        }

        private void KeyBoardEventProcessor(KeyboardEvent keyboardEvent)
        {
            //int keyCode = KeyboardUtility.AndroidKeyToWindowsKey(dataPacket.KeyboardEvent.KeyCode);
            int keyCode = keyboardEvent.KeyCode;
            KeyboardUtility.keybd_event(keyCode, 0, 0, 0);
            KeyboardUtility.keybd_event(keyCode, 0, 2, 0);
        }

        /// <summary>
        ///     处理收到的Info包
        /// </summary>
        /// <param name="info"></param>
        private void InfoProcessor(DeviceInfo info)
        {
            var clientInfo = new ClientInfo();
            clientInfo.ClientIP = clientIP;
            clientInfo.ClientName = DataTypeConverter.ByteStringToString(info.DeviceName);
            clientInfo.ClientStream = stream;

            ClientScreenWidth = info.Resolution.Width;
            ClientScreenHeight = info.Resolution.Height;

            scaleRate = Math.Min(ClientScreenWidth/(float) Screen.PrimaryScreen.Bounds.Width,
                ClientScreenHeight/(float) Screen.PrimaryScreen.Bounds.Height
                );
            if (scaleRate > 1)
                scaleRate = 1;

            if (ClientDictionary.ContainsKey(clientIP))
                ClientDictionary[clientIP] = clientInfo;
            else
                ClientDictionary.Add(clientIP, clientInfo);

            //notify UI
            dispatcher.BeginInvoke(MainWindow.RefreshDeviceList);
        }

        /// <summary>
        ///     处理收到的Command包
        /// </summary>
        /// <param name="command"></param>
        private void CommandProcessor(Command command)
        {
            int hwnd;
            int x, y;

            switch (command.CommandType)
            {
                case Command.Types.CommandType.LeftClick:
                    if (!isWindowSelected)
                    {
                        if ((DateTime.Now - lastLeftClickTime).TotalMilliseconds > MinTimeGapBetweenClicks)
                            MouseUtility.SetCursorPos((int) command.X, (int) command.Y);

                        MouseUtility.mouse_event(
                            MouseUtility.MOUSEEVENTF_LEFTDOWN | MouseUtility.MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
                        lastLeftClickTime = DateTime.Now;

                        x = (int) command.X;
                        y = (int) command.Y;

                        // MouseUtility.mouse_event(MouseUtility.MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
                    }
                    else
                    {
                        var rect = new WindowCaptureUtility.RECT();
                        WindowsUtility.GetWindowRect(selectedWindowHwnd, ref rect);
                        if ((DateTime.Now - lastLeftClickTime).TotalMilliseconds > MinTimeGapBetweenClicks)
                            MouseUtility.SetCursorPos((int) command.X + rect.Left,
                                (int) command.Y + rect.Top);

                        MouseUtility.mouse_event(
                            MouseUtility.MOUSEEVENTF_LEFTDOWN | MouseUtility.MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
                        lastLeftClickTime = DateTime.Now;

                        x = (int) command.X + rect.Left;
                        y = (int) command.Y + rect.Top;

                        // MouseUtility.mouse_event(MouseUtility.MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
                    }

                    Console.WriteLine("mouse left clicked : {0},{1}", x, y);

                    break;
                case Command.Types.CommandType.RightClick:

                    if (!isWindowSelected)
                    {
                        if ((DateTime.Now - lastRightClickTime).TotalMilliseconds > MinTimeGapBetweenClicks)
                            MouseUtility.SetCursorPos((int) command.X, (int) command.Y);

                        MouseUtility.mouse_event
                            (MouseUtility.MOUSEEVENTF_RIGHTDOWN | MouseUtility.MOUSEEVENTF_RIGHTUP, 0, 0, 0, 0);

                        lastRightClickTime = DateTime.Now;

                        x = (int) command.X;
                        y = (int) command.Y;
                        //MouseUtility.mouse_event(MouseUtility.MOUSEEVENTF_RIGHTUP, 0, 0, 0, 0);
                    }
                    else
                    {
                        var rect = new WindowCaptureUtility.RECT();
                        WindowsUtility.GetWindowRect(selectedWindowHwnd, ref rect);

                        if ((DateTime.Now - lastRightClickTime).TotalMilliseconds > MinTimeGapBetweenClicks)
                            MouseUtility.SetCursorPos((int) command.X + rect.Left,
                                (int) command.Y + rect.Top)
                                ;

                        MouseUtility.mouse_event(
                            MouseUtility.MOUSEEVENTF_RIGHTDOWN | MouseUtility.MOUSEEVENTF_RIGHTUP, 0, 0, 0, 0);

                        lastRightClickTime = DateTime.Now;

                        x = (int) command.X + rect.Left;
                        y = (int) command.Y + rect.Top;

                        // MouseUtility.mouse_event(MouseUtility.MOUSEEVENTF_RIGHTUP, 0, 0, 0, 0);
                    }

                    Console.WriteLine("mouse right clicked : {0},{1}", x, y);

                    break;
                case Command.Types.CommandType.Scroll:

                    MouseUtility.mouse_event(MouseUtility.MOUSEEVENTF_WHEEL, 0, 0, (int) command.Y, 0);
                    Console.WriteLine("scroll : " + command.Y);
                    break;
                case Command.Types.CommandType.Minimize:

                    hwnd = isWindowSelected
                        ? selectedWindowHwnd
                        : WindowsUtility.WindowFromPoint((int) command.X, (int) command.Y);
                    //找到主窗口
                    while (WindowsUtility.GetParent(hwnd) != 0)
                        hwnd = WindowsUtility.GetParent(hwnd);

                    WindowsUtility.ShowWindow(hwnd, 2);
                    Console.WriteLine("Minimize");
                    break;
                case Command.Types.CommandType.SelectWindow:
                    if (command.X > 0 && command.Y > 0)
                    {
                        selectedWindowHwnd = WindowsUtility.WindowFromPoint((int) command.X, (int) command.Y);
                        if (selectedWindowHwnd != 0)
                        {
                            var rect = new WindowCaptureUtility.RECT();

                            WindowsUtility.GetWindowRect(selectedWindowHwnd, ref rect);

                            scaleRate = Math.Min(ClientScreenWidth/(float) (rect.Right - rect.Left),
                                ClientScreenHeight/(float) (rect.Bottom - rect.Top)
                                );

                            //todo uncertain
                            if (scaleRate > 1)
                                scaleRate = 1;

                            isWindowSelected = true;
                        }

                        Console.WriteLine("window selected");
                    }
                    else
                    {
                        isWindowSelected = false;
                        selectedWindowHwnd = 0;
                        scaleRate = Math.Min(ClientScreenWidth/(float) Screen.PrimaryScreen.Bounds.Width,
                            ClientScreenHeight/(float) Screen.PrimaryScreen.Bounds.Height
                            );

                        //todo uncertain
                        if (scaleRate > 1)
                            scaleRate = 1;

                        Console.WriteLine("canceled the selected window");
                    }
                    break;
                case Command.Types.CommandType.ShutDownApp:
                    hwnd = isWindowSelected
                        ? selectedWindowHwnd
                        : WindowsUtility.WindowFromPoint((int) command.X, (int) command.Y);
                    WindowsUtility.ShutDownSpecifiedProgram(hwnd);
                    Console.WriteLine("Shut down selected app");
                    break;
                case Command.Types.CommandType.ShowDesktop:
                    WindowsUtility.ShowDesktop();
                    break;
                case Command.Types.CommandType.StartAudioTransmission:
                    if (audioSender != null) audioSender.Finish();
                    audioSender = new AudioSender(stream);
                    audioSender.Start();
                    break;
                case Command.Types.CommandType.StartVideoTransmission:
                    if (videoSenderThread != null && videoSenderThread.IsAlive) videoSenderThread.Abort();
                    videoSenderThread = new Thread(VideoSender);
                    videoSenderThread.Start();
                    break;
                case Command.Types.CommandType.StopAudioTransmission:
                    if (audioSender != null)
                        audioSender.Finish();
                    break;
                case Command.Types.CommandType.StopVideoTransmission:
                    if (videoSenderThread != null && videoSenderThread.IsAlive)
                        videoSenderThread.Abort();
                    break;
                case Command.Types.CommandType.FindMyDevice:
                    //TODO 待定
                    break;
                default:
                    break;
            }

            Console.WriteLine("CommandType " + command.CommandType);

            //UpdateVideoAndAudioThreadStatus();
        }

        /// <summary>
        ///     处理收到的Request包
        /// </summary>
        /// <param name="request"></param>
        /// <param name="stream"></param>
        //private void RequestProcessor(Request request, Stream stream)
        //{
        //    string fileName = Encoding.UTF8.GetString(request.FilePath.ToByteArray());
        //    string tableName = null;

        //    Console.WriteLine("RequestProcessor : " + request.RequestType);

        //    bool isRemove = false;
        //    switch (request.RequestType)
        //    {
        //        case Request.Types.RequestType.Music:
        //            tableName = "music";
        //            //videoSendingThreadStatus = false;
        //            //audioSendingThreadStatus = true;
        //            break;
        //        case Request.Types.RequestType.RemoveMusic:
        //            tableName = "music";
        //            isRemove = true;
        //            break;
        //        case Request.Types.RequestType.Movie:
        //            tableName = "movie";
        //            //videoSendingThreadStatus = true;
        //            //audioSendingThreadStatus = true;
        //            break;
        //        case Request.Types.RequestType.RemoveMovie:
        //            tableName = "movie";
        //            isRemove = true;
        //            break;
        //        case Request.Types.RequestType.File:
        //            tableName = "file";
        //            break;
        //        case Request.Types.RequestType.RemoveFile:
        //            tableName = "file";
        //            isRemove = true;
        //            //默认不修改音频和视频输出
        //            break;

        //        case Request.Types.RequestType.SendFile: //移动终端请求向云端发送文件并加入文件管理中心

        //            Console.WriteLine("*********** send file 1");

        //            //new FileReceiver("C:\\CloudXDownloads\\" + fileName, stream, dispatcher).ReceiveFile();
        //            lock (stream)
        //            {
        //                ReceiveFile("C:\\CloudXDownloads\\" + fileName);
        //            }
        //            Console.WriteLine("*********** send file 2");
        //            //todo notify UI to pick up a folder and store the file.
        //            //dispatcher.BeginInvoke(MainWindow.PromptToSave, fileName, stream);
        //            return;
        //        default:
        //            //send the file
        //            var sender = new FileSender(clientIP, fileName, null);
        //            sender.BeginSend();
        //            break;
        //    }

        //    Console.WriteLine("receive the request of " + tableName + " search for " + fileName);

        //    if (fileName.Equals("*"))
        //    {
        //        //query the whole table
        //        DataTable dataTable = SQLiteUtils.LoadData(tableName);

        //        foreach (DataRow dataRow in dataTable.Rows) //send all the entries
        //        {
        //            RequestFeedbackSender(dataRow[0].ToString());
        //        }

        //        RequestFeedbackSender("<NULL>"); //send a null entry as an end
        //    }
        //    else
        //    {
        //        if (isRemove)
        //        {
        //            SQLiteUtils.Delete(tableName, fileName);
        //            SampleData.Artists.Remove(Movie.convertFileURLToMovieItem(fileName));
        //        }
        //        else
        //        {
        //            try
        //            {
        //                Process.Start(fileName); //open the file by the default application

        //                Console.WriteLine("Start " + fileName);
        //            }
        //            catch (Exception e)
        //            {
        //                Console.WriteLine(e);
        //            }
        //        }
        //    }
        //}

        #endregion
    }

    #region ClientInfo

    //TODO 
    public class ClientInfo
    {
        public string ClientIP { get; set; }
        public string ClientName { get; set; }
        public Stream ClientStream { get; set; }
    }

    #endregion

    public class AudioSender
    {
        private readonly Stream stream;
        private AudioCaptureUtils audioCaptureUtils;
        private bool running = true;

        public AudioSender(Stream stream)
        {
            this.stream = stream;
        }

        public void Start()
        {
            audioCaptureUtils = new AudioCaptureUtils();
            audioCaptureUtils.StartCapture(stream);
        }

        public void Pause()
        {
            if (audioCaptureUtils != null)
                audioCaptureUtils.StopRecording();
            audioCaptureUtils = null;
        }

        //整个Client结束时调用
        public void Finish()
        {
            try
            {
                Pause();
                if (stream != null)
                    stream.Dispose();
            }
            catch (SocketException exception)
            {
                Console.WriteLine("AudioSender Finish " + exception);
            }
        }
    }
}