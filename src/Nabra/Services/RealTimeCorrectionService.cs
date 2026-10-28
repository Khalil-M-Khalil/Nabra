using System;
using System.Text;
using System.Windows;
using System.Windows.Threading;
using Nabra.Core.Models;
using Nabra.Core.Services;
using Nabra.Interop;

namespace Nabra.Services
{
    /// <summary>
    /// Monitors keystrokes in real-time when enabled.
    /// The buffer always stores the ENGLISH key equivalent of what was physically pressed.
    /// _screenCharCount tracks how many characters are actually visible on screen for the
    /// current word — this can differ from _buffer.Length because some keys produce multi-char
    /// output (e.g. 'b' on Arabic layout produces لا = 2 screen chars).
    ///
    /// Conversion directions:
    ///   ToArabic / Auto+EnglishLayout:
    ///     Buffer = English chars typed on English layout (screen also shows English chars).
    ///     _screenCharCount == _buffer.Length always.
    ///     Convert buffer → Arabic, backspaces = _screenCharCount.
    ///
    ///   ToEnglish / Auto+ArabicLayout:
    ///     Buffer = English chars mapped from VK codes (what the same key gives on English layout).
    ///     Screen shows Arabic chars (what the OS typed via the Arabic layout).
    ///     _screenCharCount may differ from _buffer.Length (b→لا = 1 buffer, 2 screen).
    ///     Convert buffer → Arabic first to get what's on screen, then produce English output.
    ///     Backspaces = _screenCharCount (not buffer length!).
    /// </summary>
    internal sealed class RealTimeCorrectionService : IDisposable
    {
        private static readonly char[] ArabicPunctuationKeys = { ';', '\'', ',', '.', '/', '[', ']', '`', '~', '?' };

        private readonly TextConversionService _converter;
        private readonly KeyboardLayoutService _layout;
        private readonly DispatcherTimer _idleTimer;
        private readonly StringBuilder _buffer = new(64);

        private NativeMethods.HookProc? _keyboardProc;
        private IntPtr _keyboardHook = IntPtr.Zero;

        private NativeMethods.HookProc? _mouseProc;
        private IntPtr _mouseHook = IntPtr.Zero;

        private AppSettings _settings = new();
        private IntPtr _activeHwnd = IntPtr.Zero;
        private bool _isHookInstalled;

        /// <summary>Number of characters currently visible on screen for the word being typed.</summary>
        private int _screenCharCount;

        /// <summary>True if Backspace was used during the current word — disables idle-timer conversion.</summary>
        private bool _hadBackspace;

        /// <summary>True when buffer no longer matches screen content (e.g. user backspaced into converted text).</summary>
        private bool _bufferInvalid;

        /// <summary>True when the active window had Arabic layout when the current word started.</summary>
        private bool _isArabicSession;

        public event Action<string, string>? WordConverted;

        public RealTimeCorrectionService(TextConversionService converter, KeyboardLayoutService layout)
        {
            _converter = converter;
            _layout = layout;

            _idleTimer = new DispatcherTimer(DispatcherPriority.Normal)
            {
                Interval = TimeSpan.FromSeconds(2.0),
            };
            _idleTimer.Tick += OnIdleTimerTick;
        }

        public bool IsActive => _isHookInstalled;

        public void Refresh(AppSettings settings)
        {
            _settings = settings;
            _idleTimer.Interval = TimeSpan.FromSeconds(Math.Max(0.5, settings.RealTimeIdleSeconds));

            bool shouldBeHooked = settings.IsEnabled && settings.RealTimeCorrection;
            if (shouldBeHooked && !_isHookInstalled)
            {
                InstallHook();
            }
            else if (!shouldBeHooked && _isHookInstalled)
            {
                UninstallHook();
            }
        }

        private void InstallHook()
        {
            if (_isHookInstalled) return;

            _keyboardProc = KeyboardHookCallback;
            _mouseProc = MouseHookCallback;

            IntPtr hMod = NativeMethods.GetModuleHandle(null);
            _keyboardHook = NativeMethods.SetWindowsHookEx(NativeMethods.WH_KEYBOARD_LL, _keyboardProc, hMod, 0);
            _mouseHook = NativeMethods.SetWindowsHookEx(NativeMethods.WH_MOUSE_LL, _mouseProc, hMod, 0);

            _isHookInstalled = _keyboardHook != IntPtr.Zero;
        }

        private void UninstallHook()
        {
            _idleTimer.Stop();
            ResetBuffer();
            _activeHwnd = IntPtr.Zero;

            if (_keyboardHook != IntPtr.Zero)
            {
                NativeMethods.UnhookWindowsHookEx(_keyboardHook);
                _keyboardHook = IntPtr.Zero;
            }

            if (_mouseHook != IntPtr.Zero)
            {
                NativeMethods.UnhookWindowsHookEx(_mouseHook);
                _mouseHook = IntPtr.Zero;
            }

            _isHookInstalled = false;
        }

        /// <summary>Clears the buffer and all associated state flags in one place.</summary>
        private void ResetBuffer()
        {
            _buffer.Clear();
            _idleTimer.Stop();
            _screenCharCount = 0;
            _hadBackspace = false;
            _bufferInvalid = false;
            _isArabicSession = false;
        }

        private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int msg = (int)wParam;
                if (msg == NativeMethods.WM_LBUTTONDOWN ||
                    msg == NativeMethods.WM_RBUTTONDOWN ||
                    msg == NativeMethods.WM_MBUTTONDOWN)
                {
                    ResetBuffer();
                }
            }
            return NativeMethods.CallNextHookEx(_mouseHook, nCode, wParam, lParam);
        }

        private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && lParam != IntPtr.Zero)
            {
                try
                {
                    var info = System.Runtime.InteropServices.Marshal.PtrToStructure<NativeMethods.KBDLLHOOKSTRUCT>(lParam);

                    // Skip synthetic/injected events to prevent feedback loops
                    if ((info.flags & NativeMethods.LLKHF_INJECTED) != 0 ||
                        info.dwExtraInfo == (IntPtr)InputSimulator.MAGIC_SIGNATURE)
                    {
                        return NativeMethods.CallNextHookEx(_keyboardHook, nCode, wParam, lParam);
                    }

                    int msg = (int)wParam;
                    if (msg == NativeMethods.WM_KEYDOWN || msg == NativeMethods.WM_SYSKEYDOWN)
                    {
                        if (HandleKeyDown(info, out bool blockKey))
                        {
                            if (blockKey) return (IntPtr)1;
                        }
                    }
                }
                catch
                {
                    // Fail-safe: never let an exception escape into the OS hook chain
                }
            }

            return NativeMethods.CallNextHookEx(_keyboardHook, nCode, wParam, lParam);
        }

        private bool HandleKeyDown(NativeMethods.KBDLLHOOKSTRUCT info, out bool blockKey)
        {
            blockKey = false;
            uint vk = info.vkCode;

            // 0. Focus tracking: reset buffer if focus changed
            IntPtr fg = NativeMethods.GetForegroundWindow();
            if (fg != IntPtr.Zero && _activeHwnd != IntPtr.Zero && fg != _activeHwnd)
            {
                ResetBuffer();
            }
            if (fg != IntPtr.Zero) _activeHwnd = fg;

            // 1. Detect active keyboard layout
            bool windowIsArabic = IsActiveWindowArabic();

            // Decide the effective conversion direction for this keystroke
            // Auto: English layout → ToArabic, Arabic layout → ToEnglish
            // ToArabic: Only convert when English layout is active
            // ToEnglish: Only convert when Arabic layout is active
            bool shouldTrack;
            switch (_settings.ConversionMode)
            {
                case ConversionMode.ToArabic:
                    shouldTrack = !windowIsArabic; // Only when English layout
                    break;
                case ConversionMode.ToEnglish:
                    shouldTrack = windowIsArabic; // Only when Arabic layout
                    break;
                default: // Auto
                    shouldTrack = true; // Track regardless; conversion direction decided at conversion time
                    break;
            }

            if (!shouldTrack)
            {
                ResetBuffer();
                return false;
            }

            // If the buffer is empty and we're starting a new word, record the layout session
            if (_buffer.Length == 0)
            {
                _isArabicSession = windowIsArabic;
            }

            // 2. Modifiers combinations (Ctrl, Alt, Win): clear buffer
            bool ctrlDown = (NativeMethods.GetAsyncKeyState(NativeMethods.VK_CONTROL) & 0x8000) != 0;
            bool altDown = (NativeMethods.GetAsyncKeyState(NativeMethods.VK_MENU) & 0x8000) != 0;
            bool winDown = (NativeMethods.GetAsyncKeyState(NativeMethods.VK_LWIN) & 0x8000) != 0 ||
                           (NativeMethods.GetAsyncKeyState(NativeMethods.VK_RWIN) & 0x8000) != 0;

            if (ctrlDown || altDown || winDown)
            {
                ResetBuffer();
                return false;
            }

            // 3. Backspace: remove last character
            if (vk == NativeMethods.VK_BACK)
            {
                if (_buffer.Length > 0)
                {
                    // Determine how many screen chars the removed buffer char contributed
                    char removed = _buffer[_buffer.Length - 1];
                    _buffer.Remove(_buffer.Length - 1, 1);

                    if (_isArabicSession)
                    {
                        // On Arabic layout, the char we're removing from the buffer is the English
                        // equivalent; on screen it produced Arabic char(s). Look up how many.
                        string arChars = _layout.ToArabic(removed.ToString());
                        // User presses Backspace once = 1 screen char removed.
                        // But we tracked arChars.Length screen chars for this key.
                        // If the key produced multiple chars (e.g. b→لا=2), OS deletes 1 per Backspace.
                        // So we must only decrement by 1 (the OS Backspace deletes 1 char).
                        _screenCharCount = Math.Max(0, _screenCharCount - 1);
                        // BUT the buffer char produced more than 1 screen char, so now the buffer
                        // is out of sync with the screen. Mark it invalid.
                        if (arChars.Length > 1)
                        {
                            _bufferInvalid = true;
                        }
                    }
                    else
                    {
                        // English layout: 1 buffer char = 1 screen char always
                        _screenCharCount = Math.Max(0, _screenCharCount - 1);
                    }

                    _hadBackspace = true;

                    if (_buffer.Length > 0)
                        ResetIdleTimer();
                    else
                        _idleTimer.Stop();
                }
                else
                {
                    // Buffer is empty but Backspace pressed — user is editing text we don't track
                    // Do NOT set _bufferInvalid here, otherwise the NEXT word won't be tracked!
                    _idleTimer.Stop();
                }
                return false;
            }

            // 4. Navigation & cancellation keys: reset buffer
            if (vk == NativeMethods.VK_ESCAPE ||
                vk == NativeMethods.VK_LEFT || vk == NativeMethods.VK_RIGHT ||
                vk == NativeMethods.VK_UP || vk == NativeMethods.VK_DOWN ||
                vk == NativeMethods.VK_HOME || vk == NativeMethods.VK_END ||
                vk == NativeMethods.VK_PRIOR || vk == NativeMethods.VK_NEXT ||
                vk == NativeMethods.VK_DELETE || vk == NativeMethods.VK_INSERT ||
                (vk >= 0x6A && vk <= 0x6F)) // Numpad operators
            {
                ResetBuffer();
                return false;
            }

            // 5. Standalone modifier keys (Shift, Ctrl, Alt, CapsLock)
            if (vk == NativeMethods.VK_SHIFT || vk == 0xA0 || vk == 0xA1 ||
                vk == NativeMethods.VK_CONTROL || vk == 0xA2 || vk == 0xA3 ||
                vk == NativeMethods.VK_MENU || vk == 0xA4 || vk == 0xA5 ||
                vk == NativeMethods.VK_CAPITAL)
            {
                return false;
            }

            // 6. Delimiters (Space, Tab, Enter)
            bool isSpace = vk == NativeMethods.VK_SPACE;
            bool isTab = vk == NativeMethods.VK_TAB;
            bool isEnter = vk == NativeMethods.VK_RETURN;
            bool convertOnEnter = _settings.RealTimeOnEnter && isEnter;

            if (isSpace || isTab || convertOnEnter)
            {
                _idleTimer.Stop();

                if (_bufferInvalid)
                {
                    // Buffer doesn't match screen. Skip conversion, reset.
                    ResetBuffer();
                    return false;
                }

                if (_buffer.Length > 0)
                {
                    string word = _buffer.ToString();
                    int backspaceCount = _screenCharCount;
                    bool wasArabicSession = _isArabicSession;
                    ResetBuffer();

                    if (TryConvert(word, wasArabicSession, out string converted))
                    {
                        InputSimulator.ReplaceTypedText(backspaceCount, converted, (ushort)vk);
                        NotifyConverted(word, converted);
                        blockKey = true; // Consume physical delimiter
                        return true;
                    }
                }
                return false;
            }

            if (isEnter)
            {
                // Enter pressed but RealTimeOnEnter is false — clear buffer to prevent cross-line merging
                ResetBuffer();
                return false;
            }

            // 7. Character typing keys: accurately map physical keystrokes
            bool shift = (NativeMethods.GetAsyncKeyState(NativeMethods.VK_SHIFT) & 0x8000) != 0;
            char? ch = MapVkToChar(vk, shift);

            if (ch.HasValue)
            {
                if (_bufferInvalid)
                {
                    // Sync was lost. Don't track characters until next word boundary.
                    return false;
                }

                if (IsWordChar(ch.Value))
                {
                    _buffer.Append(ch.Value);

                    if (_isArabicSession)
                    {
                        // On Arabic layout, look up how many chars this key produces on screen
                        string arChars = _layout.ToArabic(ch.Value.ToString());
                        _screenCharCount += arChars.Length;
                    }
                    else
                    {
                        // English layout: 1 key = 1 screen char
                        _screenCharCount++;
                    }

                    ResetIdleTimer();
                }
                else
                {
                    // Non-word symbol or punctuation: end of word, trigger conversion
                    _idleTimer.Stop();

                    if (!_bufferInvalid && _buffer.Length > 0)
                    {
                        string word = _buffer.ToString();
                        int backspaceCount = _screenCharCount;
                        bool wasArabicSession = _isArabicSession;
                        ResetBuffer();

                        if (TryConvert(word, wasArabicSession, out string converted))
                        {
                            InputSimulator.ReplaceTypedText(backspaceCount, converted, (ushort)vk);
                            NotifyConverted(word, converted);
                            blockKey = true;
                            return true;
                        }
                    }
                    else
                    {
                        ResetBuffer();
                    }
                }
            }

            return false;
        }

        private static bool IsActiveWindowArabic()
        {
            try
            {
                IntPtr fg = NativeMethods.GetForegroundWindow();
                if (fg == IntPtr.Zero) return false;

                uint tid = NativeMethods.GetWindowThreadProcessId(fg, out _);
                if (tid == 0) return false;

                var gui = new NativeMethods.GUITHREADINFO();
                gui.cbSize = System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.GUITHREADINFO>();
                if (NativeMethods.GetGUIThreadInfo(tid, ref gui) && gui.hwndFocus != IntPtr.Zero)
                {
                    uint focusTid = NativeMethods.GetWindowThreadProcessId(gui.hwndFocus, out _);
                    if (focusTid != 0) tid = focusTid;
                }

                IntPtr hkl = NativeMethods.GetKeyboardLayout(tid);
                uint langId = (uint)((ulong)hkl & 0xFFFF);
                return (langId & 0x3FF) == 0x01; // LANG_ARABIC
            }
            catch
            {
                return false;
            }
        }

        private void ResetIdleTimer()
        {
            _idleTimer.Stop();
            _idleTimer.Interval = TimeSpan.FromSeconds(Math.Max(0.5, _settings.RealTimeIdleSeconds));
            _idleTimer.Start();
        }

        private void OnIdleTimerTick(object? sender, EventArgs e)
        {
            _idleTimer.Stop();
            if (_buffer.Length == 0 || _bufferInvalid) return;

            // If user was editing (pressed Backspace), don't auto-convert on idle.
            // Wait for explicit Space/Tab/Enter to avoid converting partially-edited words.
            if (_hadBackspace)
            {
                return;
            }

            IntPtr fg = NativeMethods.GetForegroundWindow();
            if (fg != IntPtr.Zero && _activeHwnd != IntPtr.Zero && fg != _activeHwnd)
            {
                ResetBuffer();
                return;
            }

            string word = _buffer.ToString();
            int backspaceCount = _screenCharCount;
            bool wasArabicSession = _isArabicSession;
            ResetBuffer();

            if (TryConvert(word, wasArabicSession, out string converted))
            {
                InputSimulator.ReplaceTypedText(backspaceCount, converted, 0);
                NotifyConverted(word, converted);
            }
        }

        private void NotifyConverted(string from, string to)
        {
            Application.Current?.Dispatcher?.BeginInvoke(new Action(() =>
            {
                try { WordConverted?.Invoke(from, to); } catch { }
            }));
        }

        /// <summary>
        /// Attempts to convert the buffered word. The buffer always contains English key equivalents.
        ///
        /// When the user typed on ENGLISH layout (_isArabicSession=false):
        ///   Buffer "lvpfh" → screen shows "lvpfh" → convert to Arabic "مرحبا"
        ///
        /// When the user typed on ARABIC layout (_isArabicSession=true):
        ///   Buffer "lvpfh" → screen shows "مرحبا" → convert buffer back to Arabic first to get
        ///   what's on screen, then produce the English equivalent.
        ///   In other words: the user typed Arabic by mistake when they wanted English.
        ///   Screen has "مرحبا", we need to replace it with "lvpfh".
        /// </summary>
        private bool TryConvert(string bufferWord, bool wasArabicSession, out string converted)
        {
            converted = bufferWord;
            if (string.IsNullOrWhiteSpace(bufferWord)) return false;

            if (wasArabicSession)
            {
                // User was typing on Arabic layout. The buffer has English key equivalents.
                // The screen shows the Arabic text that the OS produced.
                // The user WANTED to type English but forgot to switch layout.
                // So we just output the English key equivalents directly.
                //
                // But we should check: does the user's chosen mode allow this?
                if (_settings.ConversionMode == ConversionMode.ToArabic)
                    return false; // User explicitly only wants English→Arabic, never the reverse

                // The screen shows Arabic text. We want to replace it with the English buffer content.
                converted = bufferWord;

                // Smart filter: if in Auto mode, check if this looks like intentional Arabic
                if (_settings.ConversionMode == ConversionMode.Auto && _settings.RealTimeSmartFilter)
                {
                    bool isKnown = EnglishWordCatalog.IsCommonEnglishWord(bufferWord);
                    bool looksLike = EnglishWordCatalog.LooksLikeEnglishWord(bufferWord);
                    
                    if (bufferWord.Length <= 3)
                    {
                        // For short words on an Arabic layout, they are most likely valid Arabic words.
                        // ONLY convert them if they are explicitly known English words (like 'the', 'and').
                        if (!isKnown) return false;
                    }
                    else
                    {
                        // For longer words, convert if it's in the dictionary or if it mathematically looks like English (has vowels).
                        if (!isKnown && !looksLike) return false;
                    }
                }

                return true;
            }
            else
            {
                // User was typing on English layout. Buffer = English chars = what's on screen.
                // Convert to Arabic.
                if (_settings.ConversionMode == ConversionMode.ToEnglish)
                    return false; // User explicitly only wants Arabic→English, never this direction

                var arRes = _converter.Convert(bufferWord, ConversionMode.ToArabic);
                if (arRes.Changed && !string.Equals(arRes.Text, bufferWord, StringComparison.Ordinal))
                {
                    // In Auto mode with Smart Filter
                    if (_settings.ConversionMode == ConversionMode.Auto && _settings.RealTimeSmartFilter)
                    {
                        // If it contains Arabic punctuation keys, it's 100% Arabic intent
                        if (bufferWord.IndexOfAny(ArabicPunctuationKeys) >= 0)
                        {
                            converted = arRes.Text;
                            return true;
                        }

                        // Protect common English words of length >= 4
                        if (bufferWord.Length >= 4 && EnglishWordCatalog.IsCommonEnglishWord(bufferWord))
                        {
                            return false;
                        }
                    }

                    converted = arRes.Text;
                    return true;
                }

                return false;
            }
        }

        /// <summary>Backward-compatible wrapper used by tests and the old ShouldConvert path.</summary>
        public bool ShouldConvert(string word, out string converted)
        {
            return TryConvert(word, false, out converted);
        }

        private static bool IsWordChar(char c)
        {
            if (char.IsLetterOrDigit(c)) return true;

            // Characters on English layout that map to Arabic letters or diacritics
            return c is ';' or '\'' or ',' or '.' or '/' or '[' or ']' or '`' or '~' or '?';
        }

        /// <summary>
        /// Accurately maps physical keystrokes (virtual key codes) to their character representation
        /// on the standard layout, unaffected by external window thread layout anomalies.
        /// </summary>
        public static char? MapVkToChar(uint vk, bool shift)
        {
            if (vk >= 0x41 && vk <= 0x5A) // A-Z
            {
                return shift ? (char)vk : char.ToLowerInvariant((char)vk);
            }
            if (vk >= 0x30 && vk <= 0x39) // 0-9
            {
                if (!shift) return (char)vk;
                return vk switch
                {
                    0x31 => '!', 0x32 => '@', 0x33 => '#', 0x34 => '$', 0x35 => '%',
                    0x36 => '^', 0x37 => '&', 0x38 => '*', 0x39 => '(', 0x30 => ')',
                    _ => (char)vk
                };
            }
            if (vk >= 0x60 && vk <= 0x69) // Numpad 0-9
            {
                return (char)('0' + (vk - 0x60));
            }
            return vk switch
            {
                0xBA => shift ? ':' : ';',  // VK_OEM_1 -> ';' (maps to Arabic 'ك')
                0xDE => shift ? '"' : '\'', // VK_OEM_7 -> '\'' (maps to Arabic 'ط')
                0xBC => shift ? '<' : ',',  // VK_OEM_COMMA -> ',' (maps to Arabic 'و')
                0xBE => shift ? '>' : '.',  // VK_OEM_PERIOD -> '.' (maps to Arabic 'ز')
                0xBF => shift ? '?' : '/',  // VK_OEM_2 -> '/' (maps to Arabic 'ظ', '?' -> '؟')
                0xDB => shift ? '{' : '[',  // VK_OEM_4 -> '[' (maps to Arabic 'ج')
                0xDD => shift ? '}' : ']',  // VK_OEM_6 -> ']' (maps to Arabic 'د')
                0xC0 => shift ? '~' : '`',  // VK_OEM_3 -> '`' (maps to Arabic 'ذ', '~' -> 'ّ')
                0xBD => shift ? '_' : '-',  // VK_OEM_MINUS
                0xBB => shift ? '+' : '=',  // VK_OEM_PLUS
                0xDC => shift ? '|' : '\\', // VK_OEM_5
                _ => null
            };
        }

        public void Dispose()
        {
            UninstallHook();
        }
    }
}
