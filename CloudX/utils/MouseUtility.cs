using System.Runtime.InteropServices;

namespace CloudX.utils
{
    internal class MouseUtility
    {
        public const int MOUSEEVENTF_MOVE = 0x1;
        public const int MOUSEEVENTF_LEFTDOWN = 0x2;
        public const int MOUSEEVENTF_LEFTUP = 0x4;
        public const int MOUSEEVENTF_RIGHTDOWN = 0x8;
        public const int MOUSEEVENTF_RIGHTUP = 0x10;
        public const int MOUSEEVENTF_MIDDLEDOWN = 0x20;
        public const int MOUSEEVENTF_MIDDLEUP = 0x40;
        public const int MOUSEEVENTF_XDOWN = 0x80;
        public const int MOUSEEVENTF_XUP = 0x100;
        public const int MOUSEEVENTF_WHEEL = 0x800;
        public const int MOUSEEVENTF_VIRTUALDESK = 0x4000;
        public const int MOUSEEVENTF_ABSOLUTE = 0x8000;


        [DllImport("user32.dll")]
        public static extern bool SetCursorPos(int X, int Y);

        /**
         * 
         *  //方向相反
         *  mouse_event(MOUSEEVENTF_MOVE,
         *      -(int) dataPacket.DistanceX, -(int) dataPacket.DistanceY, 0, 0);
         */

        [DllImport("user32.dll")]
        public static extern int mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);

        [DllImport("user32.dll")]
        public static extern int GetCursorPos(ref int x, ref int y);

        [DllImport("user32.dll", EntryPoint = "GetCursorPos")] //获取鼠标坐标
        public static extern int GetCursorPos(ref WindowCaptureUtility.POINTAPI point);
    }
}