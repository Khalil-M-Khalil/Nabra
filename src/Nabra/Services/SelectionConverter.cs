using System;
using System.Threading.Tasks;
using Nabra.Core.Models;
using Nabra.Core.Services;
using Nabra.Interop;

namespace Nabra.Services
{
    internal enum OutcomeKind
    {
        Success,
        NoSelection,
        NothingToConvert,
        Busy,
        Error,
    }

    /// <summary>What happened. Deliberately contains no text, only counts.</summary>
    internal sealed class ConversionOutcome
    {
        public ConversionOutcome(OutcomeKind kind, TextDirection direction = TextDirection.None, int characters = 0)
        {
            Kind = kind;
            Direction = direction;
            Characters = characters;
        }

        public OutcomeKind Kind { get; }
        public TextDirection Direction { get; }
        public int Characters { get; }
    }

    /// <summary>
    /// The copy -> convert -> paste pipeline for the currently selected text in any application.
    /// Must be called on the UI thread (clipboard access needs STA).
    /// </summary>
    internal sealed class SelectionConverter
    {
        private readonly ClipboardService _clipboard;
        private readonly TextConversionService _converter;
        private bool _busy;

        public SelectionConverter(ClipboardService clipboard, TextConversionService converter)
        {
            _clipboard = clipboard;
            _converter = converter;
        }

        public async Task<ConversionOutcome> RunAsync(AppSettings settings)
        {
            if (_busy) return new ConversionOutcome(OutcomeKind.Busy);
            _busy = true;

            ClipboardSnapshot? snapshot = null;
            ConversionOutcome outcome = new ConversionOutcome(OutcomeKind.Error);
            bool replacedSelection = false;

            try
            {
                var timings = settings.Speed.GetTimings();

                // 0. The user is still holding the shortcut keys: let go of them first.
                await InputSimulator.WaitForModifiersReleasedAsync(600);

                // 1. Keep the previous clipboard contents in memory.
                snapshot = await _clipboard.BackupAsync();

                // 2. Empty the clipboard for real, so nothing stale can be mistaken for the selection.
                await _clipboard.ClearAsync();
                uint sequence = NativeMethods.GetClipboardSequenceNumber();

                // 3. Ask the active application to copy its selection.
                InputSimulator.SendCtrlShortcut(NativeMethods.VK_C);

                // 4-5. Wait until the clipboard really changed AND holds new text.
                string? selected = await _clipboard.WaitForNewTextAsync(sequence, timings.CopyTimeoutMs);
                if (string.IsNullOrEmpty(selected))
                {
                    outcome = new ConversionOutcome(OutcomeKind.NoSelection);
                    return outcome;
                }

                // 6. Convert (pure local string processing).
                var result = _converter.Convert(selected, settings.ConversionMode);
                if (!result.Changed)
                {
                    outcome = new ConversionOutcome(OutcomeKind.NothingToConvert);
                    return outcome;
                }

                // 7. Put the corrected text on the clipboard and paste it over the selection.
                if (!await _clipboard.SetPrivateTextAsync(result.Text))
                {
                    outcome = new ConversionOutcome(OutcomeKind.Error);
                    return outcome;
                }
                await Task.Delay(20);
                InputSimulator.SendCtrlShortcut(NativeMethods.VK_V);
                replacedSelection = true;

                // 8. Give the target application time to read the clipboard before we change it back.
                await Task.Delay(timings.PasteDelayMs);

                outcome = new ConversionOutcome(OutcomeKind.Success, result.Direction, result.ConvertedCharacters);
                return outcome;
            }
            catch (Exception)
            {
                outcome = new ConversionOutcome(OutcomeKind.Error);
                return outcome;
            }
            finally
            {
                try
                {
                    // We emptied the user's clipboard, so on any failure it must be put back.
                    // On success it is put back only if the user wants that (default: yes).
                    bool shouldRestore = snapshot != null && (!replacedSelection || settings.RestoreClipboard);
                    if (shouldRestore) await _clipboard.RestoreAsync(snapshot!);
                }
                catch (Exception)
                {
                }
                _busy = false;
            }
        }
    }
}
