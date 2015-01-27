using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Threading;
using common.message;

namespace CloudX
{
    internal class DeviceFinder
    {
        private readonly Dispatcher dispatcher;

        private readonly string ip;

        public DeviceFinder(string ip, Dispatcher dispatcher)
        {
            this.dispatcher = dispatcher;
            this.ip = ip;
        }

        public void BeginFind()
        {
            new Thread(Find).Start();
        }

        private void Find()
        {
            TcpClient tcpClient = null;
            Stream targetStream = null;
            try
            {
                tcpClient = new TcpClient(ip, 50323);
                targetStream = tcpClient.GetStream();

                DataPacket.CreateBuilder()
                    .SetDataPacketType(DataPacket.Types.DataPacketType.Command)
                    .SetCommand(Command.CreateBuilder().SetCommandType(Command.Types.CommandType.FindMyDevice))
                    .Build()
                    .WriteDelimitedTo(targetStream);

                //todo pop up a window
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                if (targetStream != null)
                    targetStream.Dispose();

                if (tcpClient != null)
                    tcpClient.Close();
            }
        }
    }
}