using System;
using System.Data;
using System.IO;
using System.Threading;
using System.Windows.Threading;
using common.message;

namespace CloudX.fileutils
{
    internal class FileReceiver
    {
        private readonly FileStream file;
        private readonly Stream stream;
        private Dispatcher dispatcher = null;

        public FileReceiver(string filePath, Stream stream, Dispatcher dispatcher)
        {
            file = new FileStream(filePath, FileMode.Create);
            this.stream = stream;
            this.dispatcher = dispatcher;
        }

        public void BeginReceive()
        {
            //new Thread(Receive).Start();
        }

        //public void Receive()
        //{

        //    Console.WriteLine("FileReceiver : ReceiveFile");
        //    long receivedSize = 0;
        //    while (true)
        //    {
        //        try
        //        {
        //            DataPacket dataPacket = DataPacket.ParseDelimitedFrom(stream);
        //            if (dataPacket.HasSharedFile)
        //            {
        //                Console.WriteLine("FileReceiver : ReceiveFile {0} / {1}", receivedSize, dataPacket.SharedFile.FileLength);

        //                if (receivedSize >= dataPacket.SharedFile.FileLength)
        //                    break;

        //                byte[] buffer = dataPacket.SharedFile.Content.ToByteArray();
        //                file.Write(buffer, 0, buffer.Length);
        //                file.Flush();

        //                receivedSize += buffer.Length;

        //                Console.WriteLine("FileReceiver : ReceiveFile {0} / {1}", receivedSize, dataPacket.SharedFile.FileLength);


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
    }
}