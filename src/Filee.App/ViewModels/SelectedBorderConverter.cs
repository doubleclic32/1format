// A colour swatch shows it is the chosen one by growing a ring, not by changing its colour: its colour is the point.

using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace Filee.App.ViewModels;

/// <summary>True becomes a 3-pixel border, false none.</summary>
public sealed class SelectedBorderConverter : IValueConverter
{
    public static SelectedBorderConverter Instance { get; } = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        new Thickness(value is true ? 3 : 0);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
