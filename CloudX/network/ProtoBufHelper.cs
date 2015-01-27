using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using CloudX.utils;
using common.message;

namespace CloudX.network
{
    /// <summary>
    ///     将数据写入指定流中
    /// </summary>
    internal class ProtoBufHelper
    {
        #region DataPacket Generator

        private static DataPacket.Builder GenDataPacketBuider(DataPacket.Types.DataPacketType type)
        {
            return DataPacket.CreateBuilder()
                .SetUnixTimeStamp((ulong) (DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalMilliseconds)
                .SetDataPacketType(type);
        }

        private static SharedMessage GenSharedMessage(string content)
        {
            return SharedMessage.CreateBuilder().SetContent(DataTypeConverter.StringToByteString(content)).Build();
        }

        private static Video.Types.Point GenPoint(int x, int y)
        {
            return Video.Types.Point.CreateBuilder().SetX(x).SetY(y).Build();
        }

        private static Video.Types.Rectangle.Builder GenRectangle(int width, int height, int x, int y)
        {
            return Video.Types.Rectangle.CreateBuilder().SetHeight(height).SetWidth(width).SetX(x).SetY(y);
        }

        public static Video.Types.Rectangle GenDirtyRectangle(int width, int height, int x, int y, Bitmap image)
        {
            return GenRectangle(width, height, x
                , y).SetImage(DataTypeConverter.BytesToByteString(DataTypeConverter.BitmapToBytes(image))).Build();
        }

        public static Video.Types.MoveRectangle GenMoveRectangle(int width, int height, int x, int y)
        {
            return Video.Types.MoveRectangle.CreateBuilder().SetDestinationRectangle(
                GenRectangle(width, height, x, y)).SetSourcePoint(GenPoint(x, y)).Build();
        }

        private static Video GenVideo(Bitmap frame)
        {
            return Video.CreateBuilder().SetFrame(
                DataTypeConverter.BytesToByteString(
                    DataTypeConverter.BitmapToBytes(frame))
                ).Build();
        }

        private static Video GenVideo(IEnumerable<Video.Types.MoveRectangle> moveRectangles,
            IEnumerable<Video.Types.Rectangle> dirtyRectangles)
        {
            Video.Builder builder = Video.CreateBuilder();
            builder.AddRangeDirtyRects(dirtyRectangles);
            builder.AddRangeMoveRects(moveRectangles);
            return builder.Build();
        }


        private static Audio GenAudio(byte[] audio)
        {
            return Audio.CreateBuilder().SetSound(DataTypeConverter.BytesToByteString(audio)).Build();
        }

        public static Resolution GetResolution()
        {
            return
                Resolution.CreateBuilder()
                    .SetHeight(WindowsUtility.GetScreenHeight())
                    .SetWidth(WindowsUtility.GetScreenWidth())
                    .Build();
        }

        public static DeviceInfo GenDeviceInfo(string deviceName)
        {
            return DeviceInfo.CreateBuilder()
                .SetDeviceName(DataTypeConverter.StringToByteString(deviceName))
                .SetResolution(GetResolution()).Build();
        }

        #endregion

        #region Data Writer

        private static void WriteDataPacket(Stream stream, DataPacket dataPacket)
        {
            if (stream != null)
            {
                dataPacket.WriteDelimitedTo(stream);
            }
        }

        private static void WriteDataPacket(Stream stream, DataPacket.Builder dataPacketBuilder)
        {
            WriteDataPacket(stream, dataPacketBuilder.Build());
        }

        public static void WriteMessage(Stream stream, string message)
        {
            WriteDataPacket(stream,
                GenDataPacketBuider(DataPacket.Types.DataPacketType.SharedMessage)
                    .SetSharedMessage(GenSharedMessage(message)));
        }

        public static void WriteAudio(Stream stream, byte[] audio)
        {
            WriteDataPacket(stream,
                GenDataPacketBuider(DataPacket.Types.DataPacketType.Audio).SetAudio(GenAudio(audio)));
        }

        /// <summary>
        ///     发送完整帧数据
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="frame"></param>
        public static void WriteVideo(Stream stream, Bitmap frame)
        {
            WriteDataPacket(stream, GenDataPacketBuider(DataPacket.Types.DataPacketType.Video).SetVideo(
                GenVideo(frame)));
        }

        /// <summary>
        ///     发送不带完整帧数据的包
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="dirtyRectangles"></param>
        /// <param name="moveRectangles"></param>
        public static void WriteVideo(Stream stream, IEnumerable<Video.Types.Rectangle> dirtyRectangles,
            IEnumerable<Video.Types.MoveRectangle> moveRectangles)
        {
            WriteDataPacket(stream,
                GenDataPacketBuider(DataPacket.Types.DataPacketType.Video)
                    .SetVideo(GenVideo(moveRectangles, dirtyRectangles)));
        }

        #endregion
    }
}