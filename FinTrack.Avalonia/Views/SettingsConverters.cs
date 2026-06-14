using System;
using System.Globalization;
using System.Linq;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace FinTrack.Avalonia.Views;

/// <summary>
/// Converts SelectedTabIndex to IsVisible for the matching panel.
/// Usage: IsVisible="{Binding SelectedTabIndex, Converter={x:Static local:TabIndexConverter.Instance}, ConverterParameter=0}"
/// </summary>
public class TabIndexConverter : IValueConverter
{
    public static readonly TabIndexConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int tabIndex && parameter is string paramStr && int.TryParse(paramStr, out int targetIndex))
        {
            return tabIndex == targetIndex;
        }
        return false;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// Converts IsSubCategory (bool) to indent width (0 or 25).
/// </summary>
public class SubCategoryIndentConverter : IValueConverter
{
    public static readonly SubCategoryIndentConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isSub && isSub)
            return 25.0;
        return 0.0;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Converts Category.Type to color brush for the type marker.
/// </summary>
public class CategoryTypeColorConverter : IValueConverter
{
    public static readonly CategoryTypeColorConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is FinTrack.Core.Models.TransactionType type)
        {
            return type switch
            {
                FinTrack.Core.Models.TransactionType.Income => new SolidColorBrush(Color.Parse("#27AE60")),
                FinTrack.Core.Models.TransactionType.Expense => new SolidColorBrush(Color.Parse("#E74C3C")),
                FinTrack.Core.Models.TransactionType.Transfer => new SolidColorBrush(Color.Parse("#3498DB")),
                _ => new SolidColorBrush(Color.Parse("#95A5A6"))
            };
        }
        return new SolidColorBrush(Color.Parse("#95A5A6"));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Converts IsSubCategory to FontWeight (Normal for sub, SemiBold for parent).
/// </summary>
public class SubCategoryFontWeightConverter : IValueConverter
{
    public static readonly SubCategoryFontWeightConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isSub && isSub)
            return global::Avalonia.Media.FontWeight.Normal;
        return global::Avalonia.Media.FontWeight.SemiBold;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Converts IsSubCategory to Foreground color.
/// </summary>
public class SubCategoryForegroundConverter : IValueConverter
{
    public static readonly SubCategoryForegroundConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isSub && isSub)
            return new SolidColorBrush(Color.Parse("#5D6D7E"));
        return new SolidColorBrush(Color.Parse("#2C3E50"));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Converts IsSubCategory to FontStyle (Italic for sub, Normal for parent).
/// </summary>
public class SubCategoryFontStyleConverter : IValueConverter
{
    public static readonly SubCategoryFontStyleConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isSub && isSub)
            return global::Avalonia.Media.FontStyle.Italic;
        return global::Avalonia.Media.FontStyle.Normal;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Converts raw IBAN to spaced IBAN (TR12 3456...) and back.
/// </summary>
public class IbanConverter : IValueConverter
{
    public static readonly IbanConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string iban && !string.IsNullOrWhiteSpace(iban))
        {
            var cleanIban = new string(System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Where(iban, char.IsLetterOrDigit))).ToUpper();
            return string.Join(" ", System.Linq.Enumerable.Range(0, (cleanIban.Length + 3) / 4)
                                         .Select(i => cleanIban.Substring(i * 4, Math.Min(4, cleanIban.Length - i * 4))));
        }
        return string.Empty;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string iban)
        {
            return new string(System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Where(iban, char.IsLetterOrDigit))).ToUpper();
        }
        return string.Empty;
    }
}
