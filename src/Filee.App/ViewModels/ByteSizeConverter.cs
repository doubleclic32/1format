// Shows a byte count the way the tools say it ("1,4 Mo"), and an empty string while the number is not known yet.

using System.Globalization;
using Avalonia.Data.Converters;

namespace Filee.App.ViewModels;

/// <summary>Binding converter for the sizes the tool windows show.</summary>
public sealed class ByteSizeConverter : IValueConverter
{
    public static ByteSizeConverter Instance { get; } = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is long bytes ? ToolPreview.Human(bytes) : "";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
