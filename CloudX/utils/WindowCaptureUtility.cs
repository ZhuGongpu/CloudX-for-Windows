using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace CloudX.utils
{
    internal class WindowCaptureUtility
    {
        public static Bitmap Capture(RECT rect)
        {
            int width = rect.Right - rect.Left;
            int height = rect.Bottom - rect.Top;
            try
            {
                var bitmap = new Bitmap(width, height);
                Graphics g = Graphics.FromImage(bitmap);
                g.CopyFromScreen(new Point(rect.Left, rect.Top), new Point(0, 0),
                    new Size(width, height));

                IntPtr dc = g.GetHdc();
                g.ReleaseHdc(dc);

                //            bitmap.Save(Path + "\\" + GenerateFileName() + ".png", ImageFormat.Png);
                //Console.WriteLine("Captured");

                //g.Clear(Color.Transparent);
                return bitmap;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static Bitmap CaptureSelectedWindow(int hwnd)
        {
            var rect = new RECT();

            //todo
            WindowsUtility.GetWindowRect(hwnd, ref rect);

            Console.WriteLine("Capture Selected Window : " + rect.Left + ", " + rect.Right);

            return Capture(rect);
        }

        [StructLayout(LayoutKind.Sequential)] //定义与API相兼容结构体，实际上是一种内存转换
        public struct POINTAPI
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left; //最左坐标
            public int Top; //最上坐标
            public int Right; //最右坐标
            public int Bottom; //最下坐标
        }
    }
}