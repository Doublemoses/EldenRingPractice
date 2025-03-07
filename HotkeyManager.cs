using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using static EldenRingPractice.ERLink;
using static EldenRingPractice.ItemSpawner;
using static EldenRingPractice.MainWindow;

namespace EldenRingPractice
{
    class HotkeyManager : IDisposable
    {
        static MainWindow? mainWindow;

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private static LowLevelKeyboardProc _proc = HookCallback;
        private static IntPtr _hookID = IntPtr.Zero;
        private bool disposed = false;

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        static extern int GetForegroundWindow();

        [DllImport("user32.dll")]
        static extern int GetWindowText(int hWnd, StringBuilder text, int count);
        
        /*[Flags]
        public enum Modifiers
        {
            None    = 0,
            Control = 1,
            Alt     = 2,
            Shift   = 4,
        }*/

        public HotkeyManager(MainWindow mainoptions)
        {
            mainWindow = mainoptions;
            loadHotkeys();
            _hookID = SetHook(_proc);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
            { return; }

            if (disposing)
            {

            }

            disposed = true;
        }

        //test stuff
        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        //static Dictionary<int, ERLink.GameOptions> hotkeyList = new Dictionary<int, ERLink.GameOptions>();
        //static Dictionary<(int, int), ERLink.GameOptions> hotkeyList = new Dictionary<(int, int), ERLink.GameOptions>(); // (hotkey, modifiers), option

        public Dictionary<(int, ModifierKeys), ERLink.GameOptions> hotkeyList = new Dictionary<(int, ModifierKeys), ERLink.GameOptions>(); // (hotkey, modifiers), option

        /*private void loadHotkeys()
        {
            // hardcode hotkeys for now
            // https://learn.microsoft.com/en-us/windows/win32/inputdev/virtual-key-codes

            hotkeyList.Add((0x4F, 0), ERLink.GameOptions.RUNE_ARC); // o
            hotkeyList.Add((0x50, 0), ERLink.GameOptions.FAST_QUIT); // p
            hotkeyList.Add((0x36, 0), ERLink.GameOptions.NO_DEATH_PLAYER); // 6
            hotkeyList.Add((0x37, 0), ERLink.GameOptions.ONE_SHOT); // 7
            hotkeyList.Add((0x55, 0), ERLink.GameOptions.LOAD_SAVE); // u
            hotkeyList.Add((0x70, 0), ERLink.GameOptions.DISABLE_AI_UPDATES); // f1
            hotkeyList.Add((0x56, 0), ERLink.GameOptions.NO_GRAVITY); // v
            hotkeyList.Add((0xDB, 0), ERLink.GameOptions.NUDGE_PLUS_Y); // [
            hotkeyList.Add((0xDD, 0), ERLink.GameOptions.NUDGE_MINUS_Y); // ]




            //hotkeyList2.Add(ERLink.GameOptions.NO_DEATH_PLAYER, (0x36, 0));

        }*/

        public void loadHotkeys()
        {
            //try
            //{
            string line;
            var assembly = Assembly.GetExecutingAssembly();
            string resource = Assembly.GetExecutingAssembly().GetManifestResourceNames().Single(str => str.EndsWith("hotkeysettings.erd"));
            StreamReader sr = new StreamReader(assembly.GetManifestResourceStream(resource));

            

            while ((line = sr.ReadLine()) != null)
            {
                string[] split = line.Split(",");
                int hotkey = 0;
                int modifiers = 0;
                GameOptions option = 0;

                if (split.Length == 2 || split.Length == 3)
                {
                    if (split.Length == 3)
                    {
                        try { modifiers = Convert.ToInt32(split[2]); }
                        catch {  }
                    }

                    //try
                    //{
                    if (split[1] != "")
                    {
                        hotkey = Convert.ToInt32(split[1], 16);

                        switch (split[0])
                        {
                            case "NO_DEATH_ALL":
                                option = GameOptions.NO_DEATH_ALL; break;
                            case "NO_DEATH_PLAYER":
                                option = GameOptions.NO_DEATH_PLAYER; break;
                            case "RUNE_ARC":
                                option = GameOptions.RUNE_ARC; break;
                            default:
                                option = 0; break;
                        }
                        hotkeyList.Add((hotkey, (ModifierKeys)modifiers), option);
                    }
                    //}
                    //catch
                    //{
                        // don't care
                    //}
                }

            }            
            /*}
            catch (Exception e)
            {
                MessageBox.Show("item id list fail?");
            }*/

        }

        public (int hotkey, ModifierKeys) getOptionHotkey(ERLink.GameOptions option)
        {
            (int key, ModifierKeys modifier) returnHotkey;

            
            /*if (hotkeyList.ContainsKey(option))
            {
                hotkeyList.TryGetValue(option, out returnHotkey);
                return returnHotkey;
            }*/
            return (0, ModifierKeys.None);
        }

        public string getHotkeyText(ERLink.GameOptions option)
        {
            string returnText = "";
            /*(int key, ModifierKeys modifier) hotkey = getOptionHotkey(option);
            
            if (hotkey.key != 0)
            {
                if (hotkey.modifier != ModifierKeys.None)
                {
                    if (hotkey.modifier.HasFlag(ModifierKeys.Control)) { returnText = "Ctrl+"; }
                    if (hotkey.modifier.HasFlag(ModifierKeys.Alt)) { returnText += "Alt+"; }
                    if (hotkey.modifier.HasFlag(ModifierKeys.Shift)) { returnText += "Shift+"; }
                }

                returnText += (char)hotkey.key;
            }*/

            return returnText;
        }

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode < 0) { return CallNextHookEx(_hookID, nCode, wParam, lParam); }

            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                int handle = GetForegroundWindow();
                StringBuilder sb = new StringBuilder(9);
                GetWindowText(handle, sb, 11);

                int vkCode = Marshal.ReadInt32(lParam);

                /*if (sb.ToString() == "ELDEN RING")
                {
                    //if (hotkeyList.ContainsKey(vkCode))
                    if (hotkeyList.ContainsKey((vkCode, 0)))
                    {
                        ERLink.GameOptions option;
                        hotkeyList.TryGetValue((vkCode, 0), out option);
                        mainWindow.ActionHotkey(option);                        
                    }
                }*/

            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }
    }
}
