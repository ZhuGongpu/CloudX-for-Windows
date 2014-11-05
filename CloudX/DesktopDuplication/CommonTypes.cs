using System.IO;
using System.Runtime.InteropServices;
using CloudX.utils;
using common.message;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;

namespace CloudX.DesktopDuplication
{
    public class FrameData
    {
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
                if (MoveRectangles != null)
                    foreach (OutputDuplicateMoveRectangle moveRectangle in MoveRectangles)
                    {
                        Rectangle destRect = moveRectangle.DestinationRect;
                        Point sourcePoint = moveRectangle.SourcePoint;

                        //不需要设置image
                        videoBuilder.AddMoveRects(Video.Types.MoveRectangle.CreateBuilder().SetDestinationRectangle(
                            Video.Types.Rectangle.CreateBuilder()
                                .SetX(destRect.X)
                                .SetY(destRect.Y)
                                .SetWidth(destRect.Width)
                                .SetHeight(destRect.Height)
                            )
                            .SetSourcePoint(
                                Video.Types.Point.CreateBuilder().SetX(sourcePoint.X).SetY(sourcePoint.Y)
                            ));
                    }

                if (DirtyRectangles != null)
                    foreach (Rectangle dirtyRectangle in DirtyRectangles)
                    {
                        videoBuilder.AddDirtyRects(
                            Video.Types.Rectangle.CreateBuilder()
                                .SetX(dirtyRectangle.X).SetY(dirtyRectangle.Y)
                                .SetHeight(dirtyRectangle.Height).SetWidth(dirtyRectangle.Width)
                                .SetImage(DataTypeConverter.TextureRegionToByteString(ref Frame, dirtyRectangle.X,
                                    dirtyRectangle.Y,
                                    dirtyRectangle.Width, dirtyRectangle.Height))
                            );
                    }
            }
            else //只包含frame
            {
                videoBuilder.SetFrame(DataTypeConverter.TextureRegionToByteString(ref Frame, 0, 0, Width, Height));
            }

            DataPacket.CreateBuilder()
                .SetVideo(videoBuilder.Build())
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