using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UsbHid;
using UsbHid.USB;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace PremierePedalController
{
    public partial class Form1 : Form, IDisposable
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private static LowLevelKeyboardProc _proc = HookCallback;
        private static IntPtr _hookID = IntPtr.Zero;
        private static Form myForm;

        private delegate IntPtr LowLevelKeyboardProc(
            int nCode, IntPtr wParam, IntPtr lParam);

        private static IntPtr HookCallback(
            int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                Console.WriteLine((Keys)vkCode);

                if (vkCode == 122)
                {
                    // F11
                    myForm.Invoke((MethodInvoker)delegate
                    {
                        // splice
                        SendKeys.Send("^K");
                    });

                }
                else if (vkCode == 123)
                {
                    // F12
                    myForm.Invoke((MethodInvoker)delegate
                    {
                        // split, select clip, delete it, remove gaps between clips, and back up a bit
                        SendKeys.Send("^KD{DEL}{LEFT}+;x+{DEL}+{LEFT}+{LEFT}");
                    });
                }
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook,
        LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
            IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        byte[] left = new byte[3] { 0, 1, 0 };
        byte[] middle = new byte[3] { 0, 2, 0 };
        byte[] right = new byte[3] { 0, 4, 0 };
        byte[] up = new byte[3] { 0, 0, 0 };
        string pedalDown = "";

        public Form1()
        {
            InitializeComponent();
            myForm = this;
            this.FormClosing += Form1_FormClosing;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            UnhookWindowsHookEx(_hookID);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            _hookID = SetHook(_proc);
            var pedal = new UsbHidDevice(@"\\?\hid#vid_05f3&pid_00ff#6&8c35670&0&0000#{4d1e55b2-f16f-11cf-88cb-001111000030}");
            if (pedal is null)
                label1.Text = ("Pedal not found");
            else
            {
                label1.Text = ("Pedal Connected");
                label2.Text = "F11: Split Start\r\nF12: Split End and Delete";
                pedal.DataReceived += Pedal_DataReceived;
            }
        }

        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc,
                    GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private void Pedal_DataReceived(byte[] data)
        {
            if (data.SequenceEqual(left))
            {
                pedalDown = "LEFT";

                this.Invoke((MethodInvoker)delegate
                {
                    label1.Text = ("ZOOM OUT");
                    SendKeys.Send("-");
                });

            }
            else if (data.SequenceEqual(middle))
            {
                pedalDown = "MIDDLE";
                this.Invoke((MethodInvoker)delegate
                {
                    label1.Text = ($"PLAY");
                    SendKeys.Send(" ");
                });

            }
            else if (data.SequenceEqual(right))
            {
                pedalDown = "RIGHT";
                this.Invoke((MethodInvoker)delegate
                {
                    label1.Text = ("ZOOM IN");
                    SendKeys.Send("=");
                });
            }
            else if (data.SequenceEqual(up))
            {
                this.Invoke((MethodInvoker)delegate
                {
                    if (pedalDown == "MIDDLE")
                    {
                        label1.Text = ("STOP");
                        SendKeys.Send(" ");
                    }
                });
                pedalDown = "";
            }
        }
    }
}
