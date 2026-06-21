using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace FinTrack.Avalonia.Converters
{
    public class ProfitColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is decimal d)
            {
                if (d < 0) return new SolidColorBrush(Color.Parse("#E74C3C")); // Red
                if (d > 0) return new SolidColorBrush(Color.Parse("#27AE60")); // Green
            }
            else if (value is double dbl)
            {
                if (dbl < 0) return new SolidColorBrush(Color.Parse("#E74C3C"));
                if (dbl > 0) return new SolidColorBrush(Color.Parse("#27AE60"));
            }
            return new SolidColorBrush(Color.Parse("#34495E")); // Default
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
