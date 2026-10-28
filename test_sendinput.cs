using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Collections.Generic;

public class Test {
    [DllImport("user32.dll", SetLastError = true)]
    public static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [StructLayout(LayoutKind.Sequential)]
    public struct INPUT { public uint type; public InputUnion U; }
    [StructLayout(LayoutKind.Explicit)]
    public struct InputUnion { [FieldOffset(0)] public MOUSEINPUT mi; [FieldOffset(0)] public KEYBDINPUT ki; [FieldOffset(0)] public HARDWAREINPUT hi; }
    [StructLayout(LayoutKind.Sequential)]
    public struct MOUSEINPUT { public int dx; public int dy; public uint mouseData; public uint dwFlags; public uint time; public IntPtr dwExtraInfo; }
    [StructLayout(LayoutKind.Sequential)]
    public struct KEYBDINPUT { public ushort wVk; public ushort wScan; public uint dwFlags; public uint time; public IntPtr dwExtraInfo; }
    [StructLayout(LayoutKind.Sequential)]
    public struct HARDWAREINPUT { public uint uMsg; public ushort wParamL; public ushort wParamH; }

    public static void Main() {
        Thread.Sleep(3000);
        ReplaceTypedText(4, "????", 0x30);
    }

    public static void ReplaceTypedText(int backspaceCount, string newText, ushort trailingVk)
    {
        var inputs = new List<INPUT>();
        for (int i = 0; i < backspaceCount; i++)
        {
            inputs.Add(Key(0x08, false));
            inputs.Add(Key(0x08, true));
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
        var arr = inputs.ToArray();
        SendInput((uint)arr.Length, arr, Marshal.SizeOf(typeof(INPUT)));
    }
    private static INPUT UnicodeKey(char c, bool up) {
        return new INPUT { type = 1, U = new InputUnion { ki = new KEYBDINPUT { wVk = 0, wScan = (ushort)c, dwFlags = 4 | (up ? 2u : 0u) } } };
    }
    private static INPUT Key(ushort vk, bool up) {
        return new INPUT { type = 1, U = new InputUnion { ki = new KEYBDINPUT { wVk = vk, wScan = 0, dwFlags = up ? 2u : 0u } } };
    }
}
