using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.MarkupExtensions;
using System;
using System.Globalization;

namespace FinTrack.Avalonia.Localization
{
    public class TranslateConverter : IValueConverter
    {
        public static TranslateConverter Instance { get; } = new TranslateConverter();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (parameter is string key)
            {
                return LocalizationService.Instance[key];
            }
            return parameter;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    public class TranslateExtension : MarkupExtension
    {
        public string Key { get; set; }

        public TranslateExtension(string key)
        {
            Key = key;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            var binding = new ReflectionBindingExtension(nameof(LocalizationService.LanguageVersion))
            {
                Mode = BindingMode.OneWay,
                Source = LocalizationService.Instance,
                Converter = TranslateConverter.Instance,
                ConverterParameter = Key
            };
            return binding.ProvideValue(serviceProvider);
        }
    }
}
