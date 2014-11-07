using System.IO;
using System.Runtime.InteropServices;
using CloudX.utils;
using common.message;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Device = SharpDX.Direct3D11.Device;

namespace CloudX.DesktopDuplication
{
    public class FrameData
    {
        public static Device Device;
        public int DirtyCount;
        public Rectangle[] DirtyRectangles;
        //TODO public Bitmap FrameBitmap;
        public Texture2D Frame;
        public OutputDuplicateFrameInformation FrameInfo;
        public int Height;
        public int MoveCount;
        public OutputDuplicateMoveRectangle[] MoveRectangles;
        public int Width;

        public void WriteToStream(Stream stream)
        {
            Video.Builder videoBuilder = Video.CreateBuilder();
            if (MoveRectangles != null || DirtyRectangles != null) //出包含完整frame信息外，还包含move/dirty rects
            {
                //打印数组中内容

                //Console.WriteLine("Print----------");

                //Console.WriteLine("MoveCount:{0}, MoveLength:{1}", MoveCount, MoveRectangles.Length);

                //for (int i = 0; i < MoveCount; i++)
                //{
                //    OutputDuplicateMoveRectangle moveRectangle = MoveRectangles[i];
                //    Rectangle destRect = moveRectangle.DestinationRect;
                //    Point sourcePoint = moveRectangle.SourcePoint;
                //    Console.WriteLine("FrameData.WriteToStream MoveRect destRect: {0} sourcePoint: {1}", destRect,
                //            sourcePoint);
                //}

                //Console.WriteLine("DirtyCount:{0}, DirtyLength:{1}", DirtyCount, DirtyRectangles.Length);

                //for (int i = 0; i < DirtyCount; i++)
                //{
                //    Rectangle dirtyRectangle = DirtyRectangles[i];
                //    Console.WriteLine("FrameData.WriteToStream dirtyRect:{0}", dirtyRectangle);
                //}

                //Console.WriteLine("End Print------");

                if (MoveRectangles != null)
                    //只传输moveCount个
                    for (int i = 0; i < MoveCount; i++)
                    {
                        OutputDuplicateMoveRectangle moveRectangle = MoveRectangles[i];
                        Rectangle destRect = moveRectangle.DestinationRect;
                        Point sourcePoint = moveRectangle.SourcePoint;

                        //不需要设置image
                        videoBuilder.AddMoveRects(Video.Types.MoveRectangle.CreateBuilder()
                            .SetDestinationRectangle(
                                Video.Types.Rectangle.CreateBuilder()
                                    .SetX(destRect.X)
                                    .SetY(destRect.Y)
                                    .SetWidth(destRect.Width)
                                    .SetHeight(destRect.Height)
                            )
                            .SetSourcePoint(
                                Video.Types.Point.CreateBuilder().SetX(sourcePoint.X).SetY(sourcePoint.Y)
                            ));

                        //Console.WriteLine("FrameData.WriteToStream MoveRect destRect: {0} sourcePoint: {1}", destRect,
                        //    sourcePoint);
                    }

                if (DirtyRectangles != null && Frame != null && Device != null)
                    //只传输dirtyCount个
                    for (int i = 0; i < DirtyCount; i++)
                    {
                        Rectangle dirtyRectangle = DirtyRectangles[i];
                        videoBuilder.AddDirtyRects(
                            Video.Types.Rectangle.CreateBuilder()
                                .SetX(dirtyRectangle.X).SetY(dirtyRectangle.Y)
                                .SetHeight(dirtyRectangle.Height).SetWidth(dirtyRectangle.Width)
                                .SetImage(
                                    DataTypeConverter.TextureRegionToByteString(ref Device, ref Frame, dirtyRectangle.X,
                                        dirtyRectangle.Y,
                                        dirtyRectangle.Width, dirtyRectangle.Height))
                            );


                        // Console.WriteLine("FrameData.WriteToStream dirtyRect:{0}", dirtyRectangle);
                    }
            }
            else //只包含frame
            {
                if (Device != null && Frame != null)
                    videoBuilder.SetFrame(DataTypeConverter.TextureRegionToByteString(ref Device, ref Frame, 0, 0, Width,
                        Height));
            }

            DataPacket.CreateBuilder()
                .SetDataPacketType(DataPacket.Types.DataPacketType.Video)
                .SetVideo(videoBuilder)
                .Build()
                .WriteDelimitedTo(stream);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public class Vertex
    {
        public float Pos_X;
        public float Pos_Y;
        public float Pos_Z;

        public float TexCoord_X;
        public float TexCoord_Y;

        public void SetPos(float x, float y, float z)
        {
            Pos_X = x;
            Pos_Y = y;
            Pos_Z = z;
        }

        public void SetTexCoord(float x, float y)
        {
            TexCoord_X = x;
            TexCoord_Y = y;
        }
    }
}