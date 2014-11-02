using System.Runtime.InteropServices;

namespace CloudX.utils
{
    internal class KeyboardUtility
    {
        [DllImport("user32.dll", EntryPoint = "keybd_event")]
        public static extern void keybd_event(
            int bVk, //虚拟键值
            byte bScan, // 一般为0
            int dwFlags, //这里是整数类型 0 为按下，2为释放
            int dwExtraInfo //这里是整数类型 一般情况下设成为0
            );

        public static int AndroidKeyToWindowsKey(int androidKeyCode)
        {
            switch (androidKeyCode)
            {
                    #region 0~9

                case 7:
                    return 0x30;
                case 8:
                    return 0x31;
                case 9:
                    return 0x32;
                case 10:
                    return 0x33;
                case 11:
                    return 0x34;
                case 12:
                    return 0x35;
                case 13:
                    return 0x36;
                case 14:
                    return 0x37;
                case 15:
                    return 0x38;
                case 16:
                    return 0x39;

                    #endregion

                    #region A~Z

                case 29:
                    return 0x41;
                case 30:
                    return 0x42;
                case 31:
                    return 0x43;
                case 32:
                    return 0x44;
                case 33:
                    return 0x45;
                case 34:
                    return 0x46;
                case 35:
                    return 0x47;
                case 36:
                    return 0x48;
                case 37:
                    return 0x49;
                case 38:
                    return 0x4A;
                case 39:
                    return 0x4B;
                case 40:
                    return 0x4C;
                case 41:
                    return 0x4D;
                case 42:
                    return 0x4E;
                case 43:
                    return 0x4F;
                case 44:
                    return 0x50;
                case 45:
                    return 0x51;
                case 46:
                    return 0x52;
                case 47:
                    return 0x53;
                case 48:
                    return 0x54;
                case 49:
                    return 0x55;
                case 50:
                    return 0x56;
                case 51:
                    return 0x57;
                case 52:
                    return 0x58;
                case 53:
                    return 0x59;
                case 54:
                    return 0x5A;

                    #endregion

                case 55:
                    return 0xBC; //',' comma
                case 56:
                    return 0xBE; //'.' period
                case 57:
                    //left alt
                case 58:
                    //right alt
                    return 0x12;
                case 59:
                    return 0xA0; //left shift
                case 60:
                    return 0xA1; //right shift
                case 61:
                    return 0x09; //tab
                case 62:
                    return 0x20; //space

                case 66:
                    return 0x0D; //enter
                case 67:
                    return 0x08; //backspace
                case 68:
                    return 0xC0; //'`' (backtick) grave
                case 69:
                    return 0xBD; //'-' minus
                case 70:
                    return 0; //'=' equals todo 
                case 71:
                    return 0xDB; //'[' left bracket
                case 72:
                    return 0xDD; //']' right bracket
                case 73:
                    return 0xE2; //'\' backslash
                case 74:
                    return 0xBA; //';' semicolon
                case 75:
                    return 0xDE; //''' apostrophe
                case 76:
                    return 0xBF; //'/' slash
                case 77:
                    return 0; //'@' at //TODO

                case 81:
                    return 0xBB; //'+' plus

                case 204:
                    return 0; //Shift+Spacebar language switch //TODO
                default:
                    return 0;
            }
        }
    }
}