using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using WindowsInput;
namespace KinectCraft
{
    class InputController
    {
        [DllImport("user32.dll")]
        static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);

        public InputController()
        {
        }

        public static void SendKeyPress(VirtualKeyCode key)
        {
            keybd_event((byte)key, 0, 0, 0);
        }
    }
}
