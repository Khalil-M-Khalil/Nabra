using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using Nabra.Interop;

namespace Nabra.Services
{
    internal sealed class ClipboardEntry
    {
        public ClipboardEntry(string format, object data, bool isStream)
        {
            Format = format;
            Data = data;
            IsStream = isStream;
        }

        public string Format { get; }
        public object Data { get; }
        public bool IsStream { get; }
    }

    /// <summary>An in-memory copy of whatever was on the clipboard. Never written to disk.</summary>
    internal sealed class ClipboardSnapshot
    {
        public List<ClipboardEntry> Entries { get; } = new List<ClipboardEntry>();
        public bool IsEmpty => Entries.Count == 0;
    }

    /// <summary>
    /// All clipboard work. Every method must run on the UI (STA) thread; the awaits inside resume
    /// on that same thread, so the apartment state is preserved. Other programs often hold the
    /// clipboard open for a few milliseconds, hence the retry loop.
    /// </summary>
    internal sealed class ClipboardService
    {
        private const int MaxAttempts = 12;
        private const int RetryDelayMs = 25;

        public async Task<ClipboardSnapshot> BackupAsync()
        {
            var snapshot = new ClipboardSnapshot();
            var data = await RetryAsync(() => Clipboard.GetDataObject());
            if (data == null) return snapshot;

            string[] formats;
            try { formats = data.GetFormats(false); }
            catch (Exception) { return snapshot; }

            foreach (var format in formats)
            {
                try
                {
                    object? value = data.GetData(format, false);
                    if (value == null) continue;

                    if (value is string || value is string[] || value is byte[])
                    {
                        snapshot.Entries.Add(new ClipboardEntry(format, value, false));
                    }
                    else if (value is MemoryStream ms)
                    {
                        snapshot.Entries.Add(new ClipboardEntry(format, ms.ToArray(), true));
                    }
                    else if (value is BitmapSource bmp)
                    {
                        var copy = new WriteableBitmap(bmp);
                        copy.Freeze();
                        snapshot.Entries.Add(new ClipboardEntry(format, copy, false));
                    }
                }
                catch (Exception)
                {
                    // A format we cannot copy (delayed rendering, GDI handle...) is simply skipped.
                }
            }
            return snapshot;
        }

        public Task<bool> ClearAsync() => TryAsync(() => Clipboard.Clear());

        /// <summary>
        /// Waits until the clipboard's sequence number differs from <paramref name="sequenceBefore"/>
        /// AND it contains non-empty text. Returns null on timeout, so a stale value is never used.
        /// </summary>
        public async Task<string?> WaitForNewTextAsync(uint sequenceBefore, int timeoutMs)
        {
            var sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < timeoutMs)
            {
                if (NativeMethods.GetClipboardSequenceNumber() != sequenceBefore)
                {
                    string? text = null;
                    try
                    {
                        if (Clipboard.ContainsText(TextDataFormat.UnicodeText))
                            text = Clipboard.GetText(TextDataFormat.UnicodeText);
                    }
                    catch (ExternalException)
                    {
                        // clipboard busy; try again on the next tick
                    }
                    if (!string.IsNullOrEmpty(text)) return text;
                }
                await Task.Delay(12);
            }
            return null;
        }

        /// <summary>
        /// Puts text on the clipboard and asks Windows not to keep it in Clipboard History or sync
        /// it to the cloud clipboard. Falls back to plain text if that richer form is refused.
        /// </summary>
        public async Task<bool> SetPrivateTextAsync(string text)
        {
            bool ok = await TryAsync(() =>
            {
                var obj = new DataObject();
                obj.SetData(DataFormats.UnicodeText, text);
                obj.SetData("CanIncludeInClipboardHistory", new MemoryStream(BitConverter.GetBytes(0)));
                obj.SetData("CanUploadToCloudClipboard", new MemoryStream(BitConverter.GetBytes(0)));
                obj.SetData("ExcludeClipboardContentFromMonitorProcessing", new MemoryStream(BitConverter.GetBytes(1)));
                Clipboard.SetDataObject(obj, true);
            });
            if (ok) return true;
            return await TryAsync(() => Clipboard.SetText(text, TextDataFormat.UnicodeText));
        }

        public async Task<bool> RestoreAsync(ClipboardSnapshot snapshot)
        {
            if (snapshot.IsEmpty) return await ClearAsync();

            return await TryAsync(() =>
            {
                var obj = new DataObject();
                foreach (var entry in snapshot.Entries)
                {
                    try
                    {
                        object payload = entry.IsStream ? new MemoryStream((byte[])entry.Data) : entry.Data;
                        obj.SetData(entry.Format, payload, false);
                    }
                    catch (Exception)
                    {
                        // skip a format that cannot be restored
                    }
                }
                Clipboard.SetDataObject(obj, true);
            });
        }

        private static async Task<bool> TryAsync(Action op)
        {
            for (int i = 0; i < MaxAttempts; i++)
            {
                try
                {
                    op();
                    return true;
                }
                catch (ExternalException)
                {
                    // CLIPBRD_E_CANT_OPEN and friends: someone else has the clipboard right now
                }
                await Task.Delay(RetryDelayMs);
            }
            return false;
        }

        private static async Task<T?> RetryAsync<T>(Func<T> op) where T : class
        {
            for (int i = 0; i < MaxAttempts; i++)
            {
                try
                {
                    return op();
                }
                catch (ExternalException)
                {
                }
                await Task.Delay(RetryDelayMs);
            }
            return null;
        }
    }
}
