using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Nabra.Interop;

namespace Nabra.Services
{
    /// <summary>Sends Ctrl+C / Ctrl+V to whichever window currently has focus.</summary>
    internal static class InputSimulator
    {
        public const int MAGIC_SIGNATURE = 0x4E414252; // 'NABR'

        private static readonly ushort[] ModifierVks =
        {
            NativeMethods.VK_SHIFT, NativeMethods.VK_CONTROL, NativeMethods.VK_MENU,
            NativeMethods.VK_LWIN, NativeMethods.VK_RWIN
        };

        /// <summary>
        /// Atomically erases the typed word using Backspaces, types the converted text via
        /// Unicode SendInput, and optionally sends the trailing key (Space, Tab, or Return).
        /// All events are tagged with MAGIC_SIGNATURE to prevent re-entrancy into the hook.
        /// </summary>
        public static void ReplaceTypedText(int backspaceCount, string newText, ushort trailingVk = 0)
        {
            if (backspaceCount <= 0 && string.IsNullOrEmpty(newText)) return;

            int totalInputs = (backspaceCount * 2) + (newText.Length * 2) + (trailingVk != 0 ? 2 : 0);
            var inputs = new List<NativeMethods.INPUT>(totalInputs);

            for (int i = 0; i < backspaceCount; i++)
            {
                inputs.Add(Key(NativeMethods.VK_BACK, false));
                inputs.Add(Key(NativeMethods.VK_BACK, true));
            }

            foreach (char c in newText)
            {
                inputs.Add(UnicodeKey(c, false));
                inputs.Add(UnicodeKey(c, true));
            }

            if (trailingVk != 0)
            {
                inputs.Add(Key(trailingVk, false));
                inputs.Add(Key(trailingVk, true));
            }

            if (inputs.Count > 0)
            {
                var arr = inputs.ToArray();
                NativeMethods.SendInput((uint)arr.Length, arr, Marshal.SizeOf<NativeMethods.INPUT>());
            }
        }

        private static NativeMethods.INPUT UnicodeKey(char c, bool up)
        {
            return new NativeMethods.INPUT
            {
                type = NativeMethods.INPUT_KEYBOARD,
                U = new NativeMethods.InputUnion
                {
                    ki = new NativeMethods.KEYBDINPUT
                    {
                        wVk = 0,
                        wScan = (ushort)c,
                        dwFlags = NativeMethods.KEYEVENTF_UNICODE | (up ? NativeMethods.KEYEVENTF_KEYUP : 0u),
                        time = 0,
                        dwExtraInfo = (IntPtr)MAGIC_SIGNATURE,
                    },
                },
            };
        }

        public static void SendCtrlShortcut(ushort vk)
        {
            var inputs = new[]
            {
                Key(NativeMethods.VK_CONTROL, false),
                Key(vk, false),
                Key(vk, true),
                Key(NativeMethods.VK_CONTROL, true),
            };
            NativeMethods.SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<NativeMethods.INPUT>());
        }

        public static bool AnyModifierDown()
        {
            foreach (var vk in ModifierVks)
            {
                if ((NativeMethods.GetAsyncKeyState(vk) & 0x8000) != 0) return true;
            }
            return false;
        }

        /// <summary>
        /// The user is still physically holding the shortcut's modifiers when the hotkey fires.
        /// If we sent Ctrl+C right away it could arrive as Ctrl+Alt+C. So wait for release first;
        /// on timeout, synthesise key-up events as a last resort.
        /// </summary>
        public static async Task WaitForModifiersReleasedAsync(int timeoutMs)
        {
            var sw = Stopwatch.StartNew();
            while (AnyModifierDown())
            {
                if (sw.ElapsedMilliseconds > timeoutMs)
                {
                    ForceReleaseModifiers();
                    return;
                }
                await Task.Delay(10);
            }
        }

        private static void ForceReleaseModifiers()
        {
            var ups = new List<NativeMethods.INPUT>();
            foreach (var vk in ModifierVks)
            {
                if ((NativeMethods.GetAsyncKeyState(vk) & 0x8000) != 0) ups.Add(Key(vk, true));
            }
            if (ups.Count > 0)
            {
                var arr = ups.ToArray();
                NativeMethods.SendInput((uint)arr.Length, arr, Marshal.SizeOf<NativeMethods.INPUT>());
            }
        }

        private static NativeMethods.INPUT Key(ushort vk, bool up)
        {
            return new NativeMethods.INPUT
            {
                type = NativeMethods.INPUT_KEYBOARD,
                U = new NativeMethods.InputUnion
                {
                    ki = new NativeMethods.KEYBDINPUT
                    {
                        wVk = vk,
                        wScan = 0,
                        dwFlags = up ? NativeMethods.KEYEVENTF_KEYUP : 0u,
                        time = 0,
                        dwExtraInfo = (IntPtr)MAGIC_SIGNATURE,
                    },
                },
            };
        }
    }
}
