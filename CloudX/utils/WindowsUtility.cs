using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;

namespace CloudX.utils
{
    internal class WindowsUtility
    {
        /// <summary>
        ///     打开和关闭CD托盘.
        /// </summary>
        [DllImport("winmm.dll", EntryPoint = "mciSendString", CharSet = CharSet.Auto)]
        public static extern int mciSendString(string lpstrCommand, string lpstrReturnstring, int uReturnLength,
            int hwndCallback);


        public static int GetScreenWidth()
        {
            return (int)SystemParameters.PrimaryScreenWidth;
        }

        public static int GetScreenHeight()
        {
            return (int)SystemParameters.PrimaryScreenHeight;
        }

        /// <summary>
        ///     显示和隐藏鼠标指针.
        /// </summary>
        [DllImport("user32.dll", EntryPoint = "ShowCursor", CharSet = CharSet.Auto)]
        public static extern int ShowCursor(int bShow);


        /// <summary>
        ///     清空回收站.
        /// </summary>
        [DllImport("shell32.dll", EntryPoint = "SHEmptyRecycleBin", CharSet = CharSet.Auto)]
        public static extern long SHEmptyRecycleBin(IntPtr hwnd, string pszRootPath, long dwFlags);


        /// <summary>
        ///     打开浏览器
        /// </summary>
        [DllImport("shell32.dll", EntryPoint = "ShellExecute", CharSet = CharSet.Auto)]
        public static extern int ShellExecute(IntPtr hwnd, string lpOperation, string lpFile, string lpParameters,
            string lpDirectory, int nShowCmd);


        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        public static extern int GetForegroundWindow();

        /// 向指定程序发送消息
        [DllImport("User32.dll", EntryPoint = "SendMessage")]
        //public static extern int SendMessage(int hWnd, int wParam, int Msg, int lParam);
        public static extern int SendMessage(int Hwnd, int Msg, int wpala, int lpala);

        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(int hwnd, ref WindowCaptureUtility.RECT rect);


        [DllImport("user32.dll", EntryPoint = "WindowFromPoint")] //指定坐标处窗体句柄
        public static extern int WindowFromPoint(int xPoint, int yPoint);


        /// <summary>
        ///     最大化窗口:3，最小化窗口:2，正常大小窗口:1；
        /// </summary>
        [DllImport("user32.dll", EntryPoint = "ShowWindow", CharSet = CharSet.Auto)]
        public static extern int ShowWindow(int hwnd, int nCmdShow);


        public static void ShowDesktop()
        {
            Type oleType = Type.GetTypeFromProgID("Shell.Application");
            object oleObject = Activator.CreateInstance(oleType);
            oleType.InvokeMember("ToggleDesktop", BindingFlags.InvokeMethod, null, oleObject, null);
        }

        public static void ShutDownSpecifiedProgram(int hwnd)
        {
            //找到主窗口
            while (GetParent(hwnd) != 0)
                hwnd = GetParent(hwnd);

            //const int WM_CLOSE = 0x0010;
            SendMessage(hwnd, 0x0010, 0, 0);
        }

        [DllImport("user32.dll", EntryPoint = "GetParent", SetLastError = true)]
        public static extern int GetParent(int hWnd);
    }
}