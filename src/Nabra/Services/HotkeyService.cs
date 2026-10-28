using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Nabra.Core.Models;
using Nabra.Interop;

namespace Nabra.Services
{
    internal enum HotkeyAction
    {
        Convert = 1,
        Toggle = 2,
    }

    internal readonly struct HotkeyRegistrationResult
    {
        public HotkeyRegistrationResult(bool success, int errorCode)
        {
            Success = success;
            ErrorCode = errorCode;
        }

        public bool Success { get; }
        public int ErrorCode { get; }

        /// <summary>True when another application (or another Nabra action) already owns the combination.</summary>
        public bool InUse => !Success && ErrorCode == NativeMethods.ERROR_HOTKEY_ALREADY_REGISTERED;

        public static HotkeyRegistrationResult Ok => new HotkeyRegistrationResult(true, 0);
    }

    /// <summary>
    /// Registers global shortcuts through the Win32 RegisterHotKey API on a hidden message-only
    /// window. Only the exact registered combinations are delivered to Nabra; ordinary typing
    /// is never observed, hooked or logged.
    /// </summary>
    internal sealed class HotkeyService : IDisposable
    {
        private const int ProbeId = 0x7F00;

        private readonly HwndSource _source;
        private readonly HashSet<int> _registered = new HashSet<int>();

        public event Action<HotkeyAction>? HotkeyPressed;

        public HotkeyService()
        {
            var parameters = new HwndSourceParameters("NabraHotkeyWindow")
            {
                Width = 0,
                Height = 0,
                WindowStyle = 0,
                ParentWindow = NativeMethods.HWND_MESSAGE,
            };
            _source = new HwndSource(parameters);
            _source.AddHook(WndProc);
        }

        public HotkeyRegistrationResult Register(HotkeyAction action, HotkeyBinding binding)
        {
            Unregister(action);
            var result = RegisterCore((int)action, binding);
            if (result.Success) _registered.Add((int)action);
            return result;
        }

        /// <summary>Checks whether a combination is free, without keeping it.</summary>
        public HotkeyRegistrationResult Probe(HotkeyBinding binding)
        {
            var result = RegisterCore(ProbeId, binding);
            if (result.Success) NativeMethods.UnregisterHotKey(_source.Handle, ProbeId);
            return result;
        }

        public void Unregister(HotkeyAction action)
        {
            if (_registered.Remove((int)action))
                NativeMethods.UnregisterHotKey(_source.Handle, (int)action);
        }

        public void UnregisterAll()
        {
            foreach (var id in new List<int>(_registered))
                NativeMethods.UnregisterHotKey(_source.Handle, id);
            _registered.Clear();
        }

        private HotkeyRegistrationResult RegisterCore(int id, HotkeyBinding binding)
        {
            int vk = binding.VirtualKey;
            if (vk == 0) return new HotkeyRegistrationResult(false, 87); // ERROR_INVALID_PARAMETER

            uint mods = (uint)binding.ModifierFlags | NativeMethods.MOD_NOREPEAT;
            bool ok = NativeMethods.RegisterHotKey(_source.Handle, id, mods, (uint)vk);
            return ok
                ? HotkeyRegistrationResult.Ok
                : new HotkeyRegistrationResult(false, Marshal.GetLastWin32Error());
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == NativeMethods.WM_HOTKEY)
            {
                int id = wParam.ToInt32();
                if (id == (int)HotkeyAction.Convert || id == (int)HotkeyAction.Toggle)
                {
                    handled = true;
                    var action = (HotkeyAction)id;
                    // Leave the message handler immediately; do the real work on the dispatcher.
                    Application.Current.Dispatcher.BeginInvoke(new Action(() => HotkeyPressed?.Invoke(action)));
                }
            }
            return IntPtr.Zero;
        }

        public void Dispose()
        {
            UnregisterAll();
            _source.RemoveHook(WndProc);
            _source.Dispose();
        }
    }
}
