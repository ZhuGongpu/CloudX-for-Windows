using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using CloudX.utils;

namespace CloudX
{
    internal class AudioServer
    {
        private readonly string ServerIP;
        private int ServerPort = 50324;
        private TcpListener listener;
        private bool running = true;

        public AudioServer()
        {
            ServerIP = IPUtils.GetHostIP();
        }

        public void Start()
        {
            try
            {
                listener = new TcpListener(IPAddress.Parse(ServerIP), ServerPort);
                listener.Start();

                while (running)
                {
                    TcpClient client = null;
                    try
                    {
                        client = listener.AcceptTcpClient();
                        Console.WriteLine("AudioServer Accept");
                        new Thread(new AudioSender(client.GetStream()).Start).Start();
                    }
                    catch (Exception)
                    {
                        if (client != null)
                            client.Close();
                    }
                }

                Console.WriteLine("AudioServer Done");

            }
            catch (Exception)
            {

            }
        }

        public void Finish()
        {
            running = false;
            if (listener != null)
                listener.Stop();
        }
    }

    #region AudioSender

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

    #endregion
}