using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Drawing;

namespace UmamusumeOCR
{
    public class WindowsUtilities
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        [DllImport("user32.dll", EntryPoint = "FindWindow", SetLastError = true, CharSet = CharSet.Unicode)]
        static extern IntPtr FindWindowByCaption(IntPtr ZeroOnly, string lpWindowName);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetWindowRect(IntPtr hwnd, out WindowRect lpRect);

        public struct WindowRect
        {
            public int Left;        // x position of upper-left corner
            public int Top;         // y position of upper-left corner
            public int Right;       // x position of lower-right corner
            public int Bottom;      // y position of lower-right corner
        }

        public static IntPtr GetActiveWindowHandler()
        {
            return GetForegroundWindow();
        }

        public static string GetWindowTitle(IntPtr windowHandler)
        {
            const int nChars = 256;
            StringBuilder Buff = new(nChars);

            if (GetWindowText(windowHandler, Buff, nChars) > 0)
            {
                return Buff.ToString();
            }
            return null;
        }

        public static Rectangle GetWindowArea(IntPtr windowHandler)
        {
            GetWindowRect(windowHandler, out WindowRect rect);
            return new()
            {
                X = rect.Left,
                Y = rect.Top,
                Width = rect.Right - rect.Left,
                Height = rect.Bottom - rect.Top
            };
        }

        public static bool SetWindowPos(IntPtr windowHandler, int x, int y, int width, int height)
        {
            
            return SetWindowPos(windowHandler, IntPtr.Zero, x, y, width, height, 0);
        }

        //[DllImport("user32.dll")]
        //private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        //[DllImport("user32.dll")]
        //private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        //private IntPtr _windowHandle;
        //private HwndSource _source;

        //private const int HOTKEY_ID = 9000;
        //private const uint MOD_NONE = 0x0000; //(none)
        //private const uint MOD_ALT = 0x0001; //ALT
        //private const uint MOD_CONTROL = 0x0002; //CTRL
        //private const uint MOD_SHIFT = 0x0004; //SHIFT
        //private const uint MOD_WIN = 0x0008; //WINDOWS
        //private const uint VK_CAPITAL = 0x14;
        //private const uint VK_MBTN = 0x04;

        //protected override void OnSourceInitialized(EventArgs e)
        //{
        //    base.OnSourceInitialized(e);

        //    _windowHandle = new WindowInteropHelper(this).Handle;
        //    _source = HwndSource.FromHwnd(_windowHandle);
        //    _source.AddHook(HwndHook);

        //    RegisterHotKey(_windowHandle, HOTKEY_ID, MOD_NONE, VK_CAPITAL); //CTRL + CAPS_LOCK
        //}

        //private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        //{
        //    const int WM_HOTKEY = 0x0312;

        //    switch (msg)
        //    {
        //        case WM_HOTKEY:
        //            {
        //                switch (wParam.ToInt32())
        //                {
        //                    case HOTKEY_ID:
        //                        int vkey = (((int)lParam >> 16) & 0xFFFF);
        //                        if (vkey == VK_CAPITAL)
        //                        {
        //                            CaptureAreaEvent(StoryDialogueBtn, null);
        //                        }
        //                        //handled = true;
        //                        break;
        //                }
        //                break;
        //            }

        //    }

        //    return IntPtr.Zero;
        //}

        //protected override void OnClosed(EventArgs e)
        //{
        //    _source.RemoveHook(HwndHook);
        //    UnregisterHotKey(_windowHandle, HOTKEY_ID);
        //    base.OnClosed(e);
        //}
    }
}
