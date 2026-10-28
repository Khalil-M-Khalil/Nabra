using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Nabra.Core.Models;
using Nabra.Localization;
using Nabra.Services;

namespace Nabra.Views
{
    internal enum BannerKind
    {
        Success,
        Warning,
        Error,
    }

    /// <summary>Small UI helpers shared by several pages.</summary>
    internal static class UiHelpers
    {
        /// <summary>Draws a shortcut as keycap chips: [Ctrl] + [Alt] + [K]. Always left-to-right.</summary>
        public static void FillChips(Panel panel, HotkeyBinding? binding, string emptyText = "")
        {
            panel.Children.Clear();
            panel.FlowDirection = FlowDirection.LeftToRight;

            if (binding == null)
            {
                var none = new TextBlock { Text = emptyText, VerticalAlignment = VerticalAlignment.Center };
                none.SetResourceReference(TextBlock.ForegroundProperty, "TextSecondary");
                panel.Children.Add(none);
                return;
            }

            var parts = binding.GetDisplayParts();
            for (int i = 0; i < parts.Count; i++)
            {
                if (i > 0)
                {
                    var plus = new TextBlock
                    {
                        Text = "+",
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(0, 0, 8, 0),
                        FontWeight = FontWeights.SemiBold,
                    };
                    plus.SetResourceReference(TextBlock.ForegroundProperty, "TextSecondary");
                    panel.Children.Add(plus);
                }

                var chip = new Border
                {
                    Child = new TextBlock
                    {
                        Text = parts[i],
                        FontFamily = new FontFamily("Consolas, Segoe UI"),
                        FontWeight = FontWeights.SemiBold,
                        FontSize = 15,
                        HorizontalAlignment = HorizontalAlignment.Center,
                    },
                };
                chip.SetResourceReference(FrameworkElement.StyleProperty, "KeyCap");
                panel.Children.Add(chip);
            }
        }

        public static void ShowBanner(Border banner, TextBlock text, BannerKind kind, string message)
        {
            string background;
            string foreground;
            switch (kind)
            {
                case BannerKind.Success:
                    background = "SuccessSoftBrush";
                    foreground = "SuccessBrush";
                    break;
                case BannerKind.Warning:
                    background = "WarnSoftBrush";
                    foreground = "WarnBrush";
                    break;
                default:
                    background = "DangerSoftBrush";
                    foreground = "DangerBrush";
                    break;
            }

            banner.SetResourceReference(Border.BackgroundProperty, background);
            text.SetResourceReference(TextBlock.ForegroundProperty, foreground);
            text.Text = message;
            banner.Visibility = Visibility.Visible;
        }

        public static void HideBanner(Border banner) => banner.Visibility = Visibility.Collapsed;

        public static BannerKind ToBanner(HotkeySeverity severity)
        {
            switch (severity)
            {
                case HotkeySeverity.Ok: return BannerKind.Success;
                case HotkeySeverity.Warning: return BannerKind.Warning;
                default: return BannerKind.Error;
            }
        }

        /// <summary>
        /// Opens the "press the new key combination" dialog (with Nabra's own shortcuts released so
        /// the combination can be captured), then validates, saves and activates the choice.
        /// Returns null if the user cancelled.
        /// </summary>
        public static HotkeyCheckResult? RunCapture(DependencyObject anchor, AppController app, HotkeyAction action)
        {
            var dialog = new HotkeyCaptureWindow(app, action) { Owner = Window.GetWindow(anchor) };

            app.SuspendHotkeys();
            bool? accepted;
            try
            {
                accepted = dialog.ShowDialog();
            }
            finally
            {
                app.ResumeHotkeys();
            }

            if (accepted == true && dialog.Result != null)
                return app.CommitHotkey(action, dialog.Result);
            return null;
        }

        /// <summary>Turns a commit result into the text and colour to show the user.</summary>
        public static string DescribeCommit(HotkeyCheckResult result, HotkeyBinding? saved, out BannerKind kind)
        {
            kind = ToBanner(result.Severity);
            if (!result.CanSave) return Loc.T(result.MessageKey);

            string display = saved != null ? saved.ToDisplayString() : string.Empty;
            string text = Loc.F("hk_saved_fmt", display);
            if (result.Severity == HotkeySeverity.Warning)
                text += "\n" + Loc.T(result.MessageKey);
            return text;
        }
    }
}
