using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Threading;
using common.message;
using Google.ProtocolBuffers;

namespace CloudX
{
    internal class FileSender
    {
        private readonly Dispatcher dispatcher;
        private readonly string filePath;
        private readonly string targetIP;

        private FileStream fileStream;
        private Stream outputStream;

        public FileSender(ClientInfo client, string filePath, Dispatcher dispatcher)
        {
            targetIP = client.ClientIP;
            this.filePath = filePath;
            this.dispatcher = dispatcher;
        }

        public FileSender(string targetIP, string filePath, Dispatcher dispatcher)
        {
            this.targetIP = targetIP;
            this.filePath = filePath;
            this.dispatcher = dispatcher;
        }

        public void BeginSend()
        {
            new Thread(sendFile).Start();
        }

        private void sendFile()
        {
            TcpClient client = null;
            try
            {
                client = new TcpClient(targetIP, 50323);

                Console.WriteLine("Connected to the phone " + targetIP);

                outputStream = client.GetStream();

                fileStream = new FileStream(filePath, FileMode.Open);
                long fileLength = fileStream.Length;

                ByteString fileName = ByteString.CopyFromUtf8(filePath.Substring(filePath.LastIndexOf("\\") + 1));

                int bufferSize = 2048;
                var buffer = new byte[bufferSize];
                int readLength;
                long readLengthInTotal = 0;
                while ((readLength = fileStream.Read(buffer, 0, bufferSize)) > 0)
                {
                    DataPacket.CreateBuilder().SetDataPacketType(DataPacket.Types.DataPacketType.SharedFile)
                        .SetSharedFile(
                            SharedFile.CreateBuilder()
                                .SetFileLength(fileLength)
                                .SetFileName(fileName)
                                .SetContent(ByteString.CopyFrom(buffer))
                        ).Build()
                        .WriteDelimitedTo(outputStream);
                    readLengthInTotal += readLength;

                    Console.WriteLine(Encoding.UTF8.GetString(fileName.ToByteArray()) + " " + readLengthInTotal);
                    //notify UI with progress
                    if(dispatcher != null)
                    dispatcher.BeginInvoke(MainWindow.UpdateProgress, ((double) readLengthInTotal/(double) fileLength));
                }

                Console.WriteLine("File Transmission Completed");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                if (fileStream != null) fileStream.Dispose();
                if (outputStream != null) outputStream.Dispose();
                if (client != null) client.Close();
            }
        }
    }
}