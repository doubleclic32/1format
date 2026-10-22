using Avalonia.Controls;
using Avalonia.Interactivity;
using Filee.App.Services;
using Filee.App.ViewModels;
using Filee.Core.Presets;

namespace Filee.App.Views;

/// <summary>
/// Modal redaction editor. <see cref="Result"/> holds the boxes the user applied once the window closes; it is null
/// when they cancelled.
/// </summary>
public partial class RedactToolWindow : Window
{
    public RedactToolWindow()
    {
        InitializeComponent();
        Motion.Track(this);
        Opened += (_, _) => Areas.Focus();
        Areas.RedactionsChanged += (_, _) => (DataContext as RedactToolViewModel)?.Refresh();
    }

    /// <summary>The boxes to paint over, or null when the user cancelled.</summary>
    public List<CropRect>? Result { get; private set; }

    private void OnApply(object? sender, RoutedEventArgs e)
    {
        Result = (DataContext as RedactToolViewModel)?.Result ?? [];
        Close(true);
    }

    private void OnCancel(object? sender, RoutedEventArgs e) => Close(false);
}
