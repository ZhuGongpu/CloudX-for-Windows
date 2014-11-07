using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using common.message;
using Google.ProtocolBuffers;
using SharpDX;
using SharpDX.Direct3D11;

namespace CloudX.utils
{
    internal class DataTypeConverter
    {
        public static byte[] BitmapToBytes(Bitmap bitmap)
        {
            var memoryStream = new MemoryStream();
            bitmap.Save(memoryStream, ImageFormat.Jpeg);
            var data = memoryStream.ToArray();
            memoryStream.Close();
            return data;
        }

        public static string ByteStringToString(ByteString byteString)
        {
            return Encoding.UTF8.GetString(byteString.ToByteArray());
        }

        public static byte[] StringToBytes(string s)
        {
            return Encoding.UTF8.GetBytes(s);
        }

        public static byte[] IntPtrToBytes(IntPtr ptr, int length)
        {
            var data = new byte[length];
            Marshal.Copy(ptr, data, 0, length);
            return data;
        }

        public static ByteString BytesToByteString(byte[] bytes)
        {
            return ByteString.CopyFrom(bytes);
        }

        public static ByteString TextureRegionToByteString(ref SharpDX.Direct3D11.Device device, ref Texture2D texture,
            int originX, int originY, int width, int height)
        {
           //lock(device)
            {
                // Get the desktop capture screenTexture

                Console.WriteLine("TextureRegionToByteString : texture {0}", texture == null);
                Console.WriteLine("TextureRegionToByteSTring : device {0}", device == null);

                DataBox mapSource = device
                    .ImmediateContext.MapSubresource(texture, 0, MapMode.Read,
                        MapFlags.None);

                // Create Drawing.Bitmap
                var bitmap = new Bitmap(width, height, PixelFormat.Format32bppRgb); //不能是ARGB
                var boundsRect = new System.Drawing.Rectangle(0, 0, width, height);

                // Copy pixels from screen capture Texture to GDI bitmap
                BitmapData mapDest = bitmap.LockBits(boundsRect, ImageLockMode.WriteOnly, bitmap.PixelFormat);
                IntPtr sourcePtr = mapSource.DataPointer;
                IntPtr destPtr = mapDest.Scan0;

                sourcePtr = IntPtr.Add(sourcePtr, originY*mapSource.RowPitch + originX*4);
                for (int y = 0; y < height; y++)
                {
                    // Copy a single line 

                    Utilities.CopyMemory(destPtr, sourcePtr, width*4);

                    // Advance pointers
                    if (y != height - 1)
                        sourcePtr = IntPtr.Add(sourcePtr, mapSource.RowPitch);
                    destPtr = IntPtr.Add(destPtr, mapDest.Stride);
                }

                // Release source and dest locks
                bitmap.UnlockBits(mapDest);
                texture.Device.ImmediateContext.UnmapSubresource(texture, 0);

                return BytesToByteString(BitmapToBytes(bitmap));
            }
        }
    }
}