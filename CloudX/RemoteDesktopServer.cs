using System;
using System.Net;
using System.Net.Sockets;
using System.Windows.Threading;
using CloudX.utils;

namespace CloudX
{
    internal class RemoteDesktopServer
    {
        private readonly TcpListener Server;

        private readonly string ServerIP = IPUtils.GetHostIP();
        private readonly Dispatcher dispatcher;
        private int ServerPort = 50323;

        // private readonly AudioSender audioSender = null;

        private bool running;

        public RemoteDesktopServer(Dispatcher dispatcher)
        {
            Console.WriteLine("Server IP = {0}", ServerIP);
            Server = new TcpListener(IPAddress.Parse(ServerIP), ServerPort);
            running = true;
            this.dispatcher = dispatcher;
        }

        public void Start()
        {
            Server.Start();
            //audioThread.Start();

            Console.WriteLine("waiting...");

            TcpClient client = null;

            while (running)
            {
                try
                {
                    client = Server.AcceptTcpClient();
                    string clientIp = client.Client.RemoteEndPoint.ToString();
                    clientIp = clientIp.Split(':')[0];

                    Console.WriteLine("Accepted From " + clientIp);
                    new Client(client.GetStream(), clientIp, dispatcher).Start();
                    //  receivedClientCount++;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);

                    if (client != null)
                        client.Close();
                }
            }
            //  Finish();
        }


        public void Finish()
        {
            try
            {
                running = false;
                Server.Stop();

                Console.WriteLine("RemoteDesktopServer  Finish");

                //if(audioThread != null)
                //    audioThread.Abort();
                //audioSender.Finish();
            }
            catch (SocketException exception)
            {
                Console.WriteLine(exception);
            }
        }
    }
}