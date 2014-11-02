using System;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using System.Windows.Threading;
using CloudX.Models;
using CloudX.utils;
using Finisar.SQLite;
using MahApps.Metro.Controls;



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

            // int receivedClientCount = 1;

            WriteFileList();

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


        //[Conditional("ChinaUSMaker")]
        private void WriteFileList()
        {
            FileStream fileStream = new FileStream("C:/CloudX/fileList.txt", FileMode.Create);
            StreamWriter writer = new StreamWriter(fileStream);

            DataTable table = SQLiteUtils.LoadData("music");

            foreach (DataRow row in table.Rows)
            {
                string entry = row[0] + "";

                writer.WriteLine(entry);
                writer.Flush();
            }
            writer.Close();
            fileStream.Close();



            Console.WriteLine("WriteFileList Done");
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