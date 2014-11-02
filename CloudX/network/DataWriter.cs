using System.IO;
using common.message;
using Google.ProtocolBuffers;

namespace CloudX.network
{
    /// <summary>
    /// 将数据写入指定流中
    /// </summary>
    internal class DataWriter
    {
        private static void writeDataPacket(Stream stream, DataPacket dataPacket)
        {
            if (stream != null)
            {
                dataPacket.WriteDelimitedTo(stream);
            }
        }

        public static void writeMessage(Stream stream, string message)
        {
            if (stream != null)
                writeDataPacket(stream, DataPacket.CreateBuilder()
                    .SetDataPacketType(DataPacket.Types.DataPacketType.SharedMessage)
                    .SetSharedMessage(
                        SharedMessage.CreateBuilder()
                            .SetContent(ByteString.CopyFromUtf8(message)))
                    .Build());
        }

        public static void writeCommand(Stream stream, Command.Types.CommandType commandType, float x, float y)
        {
            if (stream != null)
            {
                writeDataPacket(stream,
                    DataPacket.CreateBuilder()
                        .SetDataPacketType(DataPacket.Types.DataPacketType.Command)
                        .SetCommand(
                            Command.CreateBuilder()
                                .SetCommandType(commandType)
                                .SetX(x)
                                .SetY(y))
                        .Build());
            }
        }

        public static void writeKeyCode(Stream stream, int keyCode)
        {
            if (stream != null)
            {
                writeDataPacket(stream, DataPacket.CreateBuilder()
                    .SetDataPacketType(DataPacket.Types.DataPacketType.KeyboardEvent)
                    .SetKeyboardEvent(
                        KeyboardEvent.CreateBuilder()
                            .SetKeyCode(keyCode))
                    .Build());
            }
        }

        public static void writeInfo(Stream stream, Info.Types.InfoType infoType, string deviceName, int width,
            int height, int portListening)
        {
            if (stream != null)
            {
                writeDataPacket(stream,
                    DataPacket.CreateBuilder()
                        .SetDataPacketType(DataPacket.Types.DataPacketType.Info)
                        .SetInfo(
                            Info.CreateBuilder()
                                .SetInfoType(infoType)
                                .SetDeviceName(ByteString.CopyFromUtf8(deviceName))
                                .SetWidth(width)
                                .SetHeight(height)
                                .SetPortListening(portListening)
                        )
                        .Build());
            }
        }
    }
}