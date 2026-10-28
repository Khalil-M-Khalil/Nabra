using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace Nabra.Localization
{
    /// <summary>
    /// Live-switchable UI language (Arabic / English). XAML binds through {loc:Tr key}, so changing
    /// the language updates every open window instantly, including the layout direction.
    /// </summary>
    public sealed class Loc : INotifyPropertyChanged
    {
        public static Loc Instance { get; } = new Loc();

        private Loc()
        {
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public string Language { get; private set; } = "ar";

        public bool IsArabic => Language == "ar";

        public FlowDirection FlowDirection => IsArabic ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

        public string this[string key] => Get(key);

        public static string T(string key) => Instance.Get(key);

        public static string F(string key, params object[] args) => string.Format(Instance.Get(key), args);

        public void SetLanguage(string language)
        {
            if (language != "ar" && language != "en") language = "ar";
            if (language == Language) return;
            Language = language;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Language)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsArabic)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FlowDirection)));
        }

        private string Get(string key)
        {
            if (Strings.Table.TryGetValue(key, out var pair))
                return IsArabic ? pair.Ar : pair.En;
            return key;
        }
    }

    [MarkupExtensionReturnType(typeof(string))]
    public sealed class TrExtension : MarkupExtension
    {
        public TrExtension()
        {
        }

        public TrExtension(string key)
        {
            Key = key;
        }

        public string Key { get; set; } = string.Empty;

        public override object ProvideValue(System.IServiceProvider serviceProvider)
        {
            var binding = new Binding("[" + Key + "]")
            {
                Source = Loc.Instance,
                Mode = BindingMode.OneWay,
            };
            return binding.ProvideValue(serviceProvider);
        }
    }
}
