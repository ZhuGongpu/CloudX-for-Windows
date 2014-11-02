using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net.Sockets;
using System.Security;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Threading;
using CloudX.fileutils;
using CloudX.Models;
using CloudX.utils;
using common.message;
using Google.ProtocolBuffers;

namespace CloudX
{
    internal class Client
    {
        public delegate void MessageSender(string targetIP, string message);

        private static int MinTimeGapBetweenClicks = 300;

        public static Dictionary<String, ClientInfo> ClientDictionary = new Dictionary<string, ClientInfo>();
        public static MessageSender SendMessageToDevice = messageSender;

        private readonly string clientIP;

        private readonly Thread dataReceivingThread;
        private readonly Dispatcher dispatcher;
        private readonly Thread videoSendingThread;
        private int ClientScreenHeight;
        private int ClientScreenWidth;

        private int ConnectionFailedTimes;

        private bool isWindowSelected;
        private DateTime lastLeftClickTime = DateTime.Now;
        private DateTime lastRightClickTime = DateTime.Now;
        private float scaleRate = 1;
        private int selectedWindowHwnd;
        private Stream stream;

        public Client(Stream stream, string clientIP, Dispatcher dispatcher)
        {
            this.stream = stream;
            this.clientIP = clientIP;
            this.dispatcher = dispatcher;

            dataReceivingThread = new Thread(DataReceiver);
            videoSendingThread = new Thread(VideoSender);
        }

        public void Start()
        {
            try
            {
                dataReceivingThread.Start();
            }
            catch (Exception e)
            {
                Console.WriteLine("Client Start " + e);
                Finish();
            }
        }

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

                    if (dataPacket.HasCommand) // command
                    {
                        CommandProcessor(dataPacket.Command);
                    }
                    else if (dataPacket.HasRequest) // request
                    {
                        RequestProcessor(dataPacket.Request, stream);
                    }
                    else if (dataPacket.HasInfo) // info 
                    {
                        InfoProcessor(dataPacket.Info);
                    }
                    else if (dataPacket.HasSharedMessage) // message
                    {
                        SharedMessageProcessor(dataPacket.SharedMessage);
                    }
                    else if (dataPacket.HasSharedFile) // file
                    {
                        SharedFileProcessor(dataPacket.SharedFile);
                    }
                    else if (dataPacket.HasKeyboardEvent) // keyboard
                    {
                        KeyBoardEventProcessor(dataPacket.KeyboardEvent);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Client dataReceiver  " + (dataPacket == null) + "  " + e);
                    //stream.Flush();
                    ConnectionFailedHandler();
                }
            }

            Finish();
        }

        #region DataPacket Processor
        private void SharedMessageProcessor(SharedMessage message)
        {
            //notify UI
            dispatcher.BeginInvoke(MainWindow.ShowMessageBox
                , "复制", "取消", "收到消息：", ByteStringToString(message.Content));

            Console.WriteLine("Receive a message : "
                              + ByteStringToString(message.Content));

            //send the message to all
            foreach (var entry in ClientDictionary)
            {
                if (!entry.Key.Equals(clientIP))
                {
                    messageSender(entry.Key, message.Content);
                }
            }
        }

        private void SharedFileProcessor(SharedFile file)
        {
            //todo
            Console.WriteLine("Receive a file");
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
        private void InfoProcessor(Info info)
        {
            switch (info.InfoType)
            {
                case Info.Types.InfoType.Login:
                    if (!ClientDictionary.ContainsKey(clientIP))
                    {
                        var clientInfo = new ClientInfo();
                        clientInfo.ClientIP = clientIP;
                        clientInfo.ClientName = ByteStringToString(info.DeviceName);
                        clientInfo.ClientStream = stream;

                        if (ClientDictionary.ContainsKey(clientIP))
                            ClientDictionary[clientIP] = clientInfo;
                        else
                            ClientDictionary.Add(clientIP, clientInfo);
                    }
                    break;
                case Info.Types.InfoType.Logout:

                    if (ClientDictionary.ContainsKey(clientIP))
                        ClientDictionary.Remove(clientIP);

                    break;
                case Info.Types.InfoType.NormalInfo:
                    if (info.Height == 0 && info.Width == 0 && info.PortListening == 0)
                    {
                        //deal as a request
                        InfoPacketSender();
                    }
                    else
                    {
                        if (info.HasDeviceName)
                        {
                            if (ClientDictionary.ContainsKey(clientIP))
                            {
                                var clientInfo = new ClientInfo();
                                clientInfo.ClientIP = clientIP;
                                clientInfo.ClientName = ByteStringToString(info.DeviceName);
                                clientInfo.ClientStream = stream;
                                ClientDictionary[clientIP] = clientInfo;
                            }
                        }

                        ClientScreenWidth = info.Width;
                        ClientScreenHeight = info.Height;

                        scaleRate = Math.Min(ClientScreenWidth / (float)Screen.PrimaryScreen.Bounds.Width,
                            ClientScreenHeight / (float)Screen.PrimaryScreen.Bounds.Height
                            );
                        if (scaleRate > 1)
                            scaleRate = 1;
                    }
                    break;
            }

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
                            MouseUtility.SetCursorPos((int)command.X, (int)command.Y);

                        MouseUtility.mouse_event(
                            MouseUtility.MOUSEEVENTF_LEFTDOWN | MouseUtility.MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
                        lastLeftClickTime = DateTime.Now;

                        x = (int)command.X;
                        y = (int)command.Y;

                        // MouseUtility.mouse_event(MouseUtility.MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
                    }
                    else
                    {
                        var rect = new WindowCaptureUtility.RECT();
                        WindowsUtility.GetWindowRect(selectedWindowHwnd, ref rect);
                        if ((DateTime.Now - lastLeftClickTime).TotalMilliseconds > MinTimeGapBetweenClicks)
                            MouseUtility.SetCursorPos((int)command.X + rect.Left,
                                (int)command.Y + rect.Top);

                        MouseUtility.mouse_event(
                            MouseUtility.MOUSEEVENTF_LEFTDOWN | MouseUtility.MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
                        lastLeftClickTime = DateTime.Now;

                        x = (int)command.X + rect.Left;
                        y = (int)command.Y + rect.Top;

                        // MouseUtility.mouse_event(MouseUtility.MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
                    }

                    Console.WriteLine("mouse left clicked : {0},{1}", x, y);

                    break;
                case Command.Types.CommandType.RightClick:

                    if (!isWindowSelected)
                    {
                        if ((DateTime.Now - lastRightClickTime).TotalMilliseconds > MinTimeGapBetweenClicks)
                            MouseUtility.SetCursorPos((int)command.X, (int)command.Y);

                        MouseUtility.mouse_event
                            (MouseUtility.MOUSEEVENTF_RIGHTDOWN | MouseUtility.MOUSEEVENTF_RIGHTUP, 0, 0, 0, 0);

                        lastRightClickTime = DateTime.Now;

                        x = (int)command.X;
                        y = (int)command.Y;
                        //MouseUtility.mouse_event(MouseUtility.MOUSEEVENTF_RIGHTUP, 0, 0, 0, 0);
                    }
                    else
                    {
                        var rect = new WindowCaptureUtility.RECT();
                        WindowsUtility.GetWindowRect(selectedWindowHwnd, ref rect);

                        if ((DateTime.Now - lastRightClickTime).TotalMilliseconds > MinTimeGapBetweenClicks)
                            MouseUtility.SetCursorPos((int)command.X + rect.Left,
                                (int)command.Y + rect.Top)
                                ;

                        MouseUtility.mouse_event(
                            MouseUtility.MOUSEEVENTF_RIGHTDOWN | MouseUtility.MOUSEEVENTF_RIGHTUP, 0, 0, 0, 0);

                        lastRightClickTime = DateTime.Now;

                        x = (int)command.X + rect.Left;
                        y = (int)command.Y + rect.Top;

                        // MouseUtility.mouse_event(MouseUtility.MOUSEEVENTF_RIGHTUP, 0, 0, 0, 0);
                    }

                    Console.WriteLine("mouse right clicked : {0},{1}", x, y);

                    break;
                case Command.Types.CommandType.Scroll:

                    MouseUtility.mouse_event(MouseUtility.MOUSEEVENTF_WHEEL, 0, 0, (int)command.Y, 0);
                    Console.WriteLine("scroll : " + command.Y);
                    break;
                case Command.Types.CommandType.Minimize:

                    hwnd = isWindowSelected
                        ? selectedWindowHwnd
                        : WindowsUtility.WindowFromPoint((int)command.X, (int)command.Y);
                    //找到主窗口
                    while (WindowsUtility.GetParent(hwnd) != 0)
                        hwnd = WindowsUtility.GetParent(hwnd);

                    WindowsUtility.ShowWindow(hwnd, 2);
                    Console.WriteLine("Minimize");
                    break;
                case Command.Types.CommandType.SelectWindow:
                    if (command.X > 0 && command.Y > 0)
                    {
                        selectedWindowHwnd = WindowsUtility.WindowFromPoint((int)command.X, (int)command.Y);
                        if (selectedWindowHwnd != 0)
                        {
                            var rect = new WindowCaptureUtility.RECT();

                            WindowsUtility.GetWindowRect(selectedWindowHwnd, ref rect);

                            scaleRate = Math.Min(ClientScreenWidth / (float)(rect.Right - rect.Left),
                                ClientScreenHeight / (float)(rect.Bottom - rect.Top)
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
                        scaleRate = Math.Min(ClientScreenWidth / (float)Screen.PrimaryScreen.Bounds.Width,
                            ClientScreenHeight / (float)Screen.PrimaryScreen.Bounds.Height
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
                        : WindowsUtility.WindowFromPoint((int)command.X, (int)command.Y);
                    WindowsUtility.ShutDownSpecifiedProgram(hwnd);
                    Console.WriteLine("Shut down selected app");
                    break;
                case Command.Types.CommandType.ShowDesktop:
                    WindowsUtility.ShowDesktop();
                    break;
                ////todo mute audio and etc
                case Command.Types.CommandType.StartAudioAndVideoTransmission:
                    //    audioSendingThreadStatus = true;
                    //videoSendingThreadStatus = true;

                    videoSendingThread.Start();

                    break;
                //case Command.Types.CommandType.StartAudioTransmission:
                //    audioSendingThreadStatus = true;

                //    break;
                //case Command.Types.CommandType.StartVideoTransmission:
                //    videoSendingThreadStatus = true;
                //    break;
                //case Command.Types.CommandType.StopAudioAndVideoTransmission:
                //    audioSendingThreadStatus = false;
                //    videoSendingThreadStatus = false;

                //    break;
                //case Command.Types.CommandType.StopAudioTransmission:
                //    audioSendingThreadStatus = false;

                //    break;
                //case Command.Types.CommandType.StopVideoTransmission:
                //    videoSendingThreadStatus = false;

                //    break;
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
        private void RequestProcessor(Request request, Stream stream)
        {
            string fileName = Encoding.UTF8.GetString(request.FilePath.ToByteArray());
            string tableName = null;

            Console.WriteLine("RequestProcessor : " + request.RequestType);

            bool isRemove = false;
            switch (request.RequestType)
            {
                case Request.Types.RequestType.Music:
                    tableName = "music";
                    //videoSendingThreadStatus = false;
                    //audioSendingThreadStatus = true;
                    break;
                case Request.Types.RequestType.RemoveMusic:
                    tableName = "music";
                    isRemove = true;
                    break;
                case Request.Types.RequestType.Movie:
                    tableName = "movie";
                    //videoSendingThreadStatus = true;
                    //audioSendingThreadStatus = true;
                    break;
                case Request.Types.RequestType.RemoveMovie:
                    tableName = "movie";
                    isRemove = true;
                    break;
                case Request.Types.RequestType.File:
                    tableName = "file";
                    break;
                case Request.Types.RequestType.RemoveFile:
                    tableName = "file";
                    isRemove = true;
                    //默认不修改音频和视频输出
                    break;

                case Request.Types.RequestType.SendFile://移动终端请求向云端发送文件并加入文件管理中心
                    
                    Console.WriteLine("*********** send file 1");

                    //new FileReceiver("C:\\CloudXDownloads\\" + fileName, stream, dispatcher).Receive();
                    lock (stream)
                    {


                        Receive("C:\\CloudXDownloads\\" + fileName);
                    }
                    Console.WriteLine("*********** send file 2");
                    //todo notify UI to pick up a folder and store the file.
                    //dispatcher.BeginInvoke(MainWindow.PromptToSave, fileName, stream);
                    return;
                default:
                    //send the file
                    var sender = new FileSender(clientIP, fileName, null);
                    sender.BeginSend();
                    break;
            }

            Console.WriteLine("receive the request of " + tableName + " search for " + fileName);

            if (fileName.Equals("*"))
            {
                //query the whole table
                DataTable dataTable = SQLiteUtils.LoadData(tableName);

                foreach (DataRow dataRow in dataTable.Rows) //send all the entries
                {
                    RequestFeedbackSender(dataRow[0].ToString());
                }

                RequestFeedbackSender("<NULL>"); //send a null entry as an end
            }
            else
            {
                if (isRemove)
                {
                    SQLiteUtils.Delete(tableName, fileName);
                    SampleData.Artists.Remove(Movie.convertFileURLToMovieItem(fileName));                    
                }
                else
                {
                    try
                    {
                        Process.Start(fileName); //open the file by the default application

                        Console.WriteLine("Start " + fileName);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }
            }
        }
        #endregion

        public void Receive(string filePath)
        {
            var file = new FileStream(filePath, FileMode.Create);

            Console.WriteLine("FileReceiver : Receive");
            long receivedSize = 0;
            while (true)
            {
                try
                {
                    DataPacket dataPacket = DataPacket.ParseDelimitedFrom(stream);
                    if (dataPacket.HasSharedFile)
                    {
                        Console.WriteLine("FileReceiver : Receive {0} / {1}", receivedSize, dataPacket.SharedFile.FileLength);

                        if (receivedSize >= dataPacket.SharedFile.FileLength)
                        {
                            file.Close();
                            Console.WriteLine("File Transimission Done");
                            return;
                        }

                        byte[] buffer = dataPacket.SharedFile.Content.ToByteArray();
                        file.Write(buffer, 0, buffer.Length);
                        file.Flush();

                        receivedSize += buffer.Length;

                        Console.WriteLine("FileReceiver : Receive {0} / {1}", receivedSize, dataPacket.SharedFile.FileLength);


                        //todo notify ui
                        //dispatcher.BeginInvoke(MainWindow.UpdateProgress, (double)receivedSize / dataPacket.SharedFile.FileLength);
                    }
                }
                catch (Exception)
                {
                    //todo notify UI
                    //dispatcher.BeginInvoke(MainWindow.ShowMessageBox, null, null, "文件传输已中断", null);

                    Console.WriteLine("File Transimission Failed");

                    break;
                }
            }

            file.Close();
            Console.WriteLine("File Transimission Done");
        }


        #region DataPacket Sender
        /// <summary>
        ///     发送单个RequestFeedback
        /// </summary>
        private void RequestFeedbackSender(string name)
        {
            try
            {
                //lock (stream) //todo 不能加锁，否则可能会造成死锁
                {
                    DataPacket.CreateBuilder().SetDataPacketType(DataPacket.Types.DataPacketType.RequestFeedback)
                        .SetRequestFeedback(
                            RequestFeedback.CreateBuilder()
                                .SetFilePath(ByteString.CopyFrom(name, Encoding.UTF8)
                                ).Build()
                        ).Build().WriteDelimitedTo(stream);
                    Console.WriteLine("request feed back sent  " + name);
                }
            }
            catch (Exception)
            {
                ConnectionFailedHandler();
            }
        }

        /// <summary>
        /// 发送主机分辨率
        /// </summary>
        private void InfoPacketSender()
        {
            try
            {
                {
                    DataPacket.CreateBuilder()
                        .SetDataPacketType(DataPacket.Types.DataPacketType.Info)
                        .SetInfo(
                            Info.CreateBuilder()
                            .SetInfoType(Info.Types.InfoType.NormalInfo)    
                            .SetPortListening(50324)
                                .SetHeight(Screen.PrimaryScreen.Bounds.Height)
                                .SetWidth(Screen.PrimaryScreen.Bounds.Width)
                        ).Build().WriteDelimitedTo(stream);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                ConnectionFailedHandler();
            }
        }

        private void VideoSender()
        {
            // Console.WriteLine("video sender online");
            //while (videoSendingThreadStatus)
            while (stream != null)
            {
                //todo
                //Thread.Sleep(100);

                Bitmap bitmap;
                if (isWindowSelected)
                {
                    bitmap = WindowCaptureUtility.CaptureSelectedWindow(selectedWindowHwnd);
                }
                else
                {
                    var rect = new WindowCaptureUtility.RECT
                    {
                        Left = Screen.PrimaryScreen.Bounds.Left,
                        Right = Screen.PrimaryScreen.Bounds.Right,
                        Top = Screen.PrimaryScreen.Bounds.Top,
                        Bottom = Screen.PrimaryScreen.Bounds.Bottom
                    };

                    bitmap = WindowCaptureUtility.Capture(rect);
                }

                if (bitmap == null) continue;

                if (stream != null)
                 {
                    try
                    {
                        //todo changed
                        //bitmap.SetResolution(bitmap.Width*scaleRate, bitmap.Height*scaleRate);

                        //send the bitmap


                        SendBitmap(new Bitmap(bitmap, (int)(bitmap.Width * scaleRate), (int)(bitmap.Height * scaleRate)));
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Before Exception : {0}, {1}  {2}", bitmap.Width, bitmap.Height, scaleRate);

                        Console.WriteLine("Client VideoReceiver Run Into An Exception : \n" + e);
                        ConnectionFailedHandler();
                    }
                }
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
                //todo changed
                messageSender(targetIP, ByteString.CopyFrom(StringToBytes(message)));
            }
        }

        private static void messageSender(string targetIP, ByteString message)
        {
            var tcpClient = new TcpClient(targetIP, 50323);

            if (tcpClient.Connected)
            {
                messageSender(tcpClient.GetStream(), message);
                tcpClient.Close();
            }
        }

        private static void messageSender(Stream targetStream, ByteString message)
        {
            if (targetStream != null)
            {
                DataPacket.CreateBuilder().SetDataPacketType(DataPacket.Types.DataPacketType.SharedMessage)
                    .SetSharedMessage(
                        SharedMessage.CreateBuilder().SetContent(message).Build()
                    ).Build().WriteDelimitedTo(targetStream);
            }
        }

        private void SendBitmap(Bitmap bitmap)
        {
            //lock (stream)
            {
                //TimeSpan timeSpan = new TimeSpan();
                //DateTime date = new DateTime(1970, 1, 1, 0, 0, 0, 0);
                //timeSpan = DateTime.Now - new DateTime();

                DataPacket.CreateBuilder()
                    .SetDataPacketType(DataPacket.Types.DataPacketType.Video)
                    .SetTimeStamp(
                        ByteString.CopyFromUtf8(DateTime.Now.ToLongTimeString() + "." + DateTime.Now.Millisecond)
                    ) //todo for debug only
                    .SetVideo(
                        Video.CreateBuilder()
                            .SetImage(ByteString.CopyFrom(
                    //CompressionAndDecompressionUtils.GZipCompress(BitmapToBytes(bitmap)) //compress
                    //CompressionAndDecompressionUtils.SnappyCompress(BitmapToBytes(bitmap))
                                BitmapToBytes(bitmap)
                                ))
                            .Build()
                    )
                    .Build()
                    .WriteDelimitedTo(stream);
            }
        }
        #endregion

        public void Finish()
        {
            try
            {
                if (dataReceivingThread != null && dataReceivingThread.IsAlive)
                    dataReceivingThread.Abort();

                if (videoSendingThread != null && videoSendingThread.IsAlive)
                    videoSendingThread.Abort();
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

        #region utilities
        private byte[] BitmapToBytes(Bitmap bitmap)
        {
            var memoryStream = new MemoryStream();
            bitmap.Save(memoryStream, ImageFormat.Jpeg);
            return memoryStream.ToArray();
        }

        private string ByteStringToString(ByteString byteString)
        {
            return Encoding.UTF8.GetString(byteString.ToByteArray());
        }

        private static byte[] StringToBytes(string s)
        {
            return Encoding.UTF8.GetBytes(s);
        }
        #endregion

    }

    #region ClientInfo

    public class ClientInfo
    {
        public string ClientIP { get; set; }
        public string ClientName { get; set; }
        public Stream ClientStream { get; set; }
    }

    #endregion

    #region 发送音频

    //public class AudioSender
    //{
    //    private readonly TcpListener audioSocketListener;

    //    private AudioCaptureUtils audioCaptureUtils;
    //    private TcpClient audioClient;

    //    public AudioSender(string ip, int port)
    //    {
    //        audioSocketListener = new TcpListener(IPAddress.Parse(ip), port);
    //        audioSocketListener.Start();
    //    }

    //    public void Start()
    //    {
    //        while (true)
    //        {
    //            audioClient = audioSocketListener.AcceptTcpClient();

    //            audioCaptureUtils = new AudioCaptureUtils();
    //            audioCaptureUtils.StartCapture(audioClient.GetStream());
    //        }
    //    }

    //    public void Pause()
    //    {
    //        if (audioCaptureUtils != null)
    //            audioCaptureUtils.StopRecording();
    //        audioCaptureUtils = null;
    //        if (audioClient != null)
    //            audioClient.Close();
    //    }

    //    //整个Client结束时调用
    //    public void Finish()
    //    {
    //        try
    //        {
    //            Pause();
    //            if (audioClient != null)
    //                audioClient.Close();
    //        }
    //        catch (SocketException exception)
    //        {
    //            Console.WriteLine("AudioSender Finish " + exception);
    //        }
    //        finally
    //        {
    //            if (audioSocketListener != null)
    //                audioSocketListener.Stop();
    //        }
    //    }
    //}

    #endregion
}