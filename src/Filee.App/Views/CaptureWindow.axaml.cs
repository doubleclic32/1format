using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Filee.App.ViewModels;

namespace Filee.App.Views;

/// <summary>
/// The full-screen capture overlay. <see cref="Area"/> holds the rectangle the user kept, in the picture's own
/// pixels, once the window closes; it is null when they cancelled or drew nothing.
/// </summary>
public partial class CaptureWindow : Window
{
    public CaptureWindow()
    {
        InitializeComponent();
        Opened += (_, _) => Area.Focus();
    }

    /// <summary>The area to keep, or null.</summary>
    public PixelRect? Kept { get; private set; }

    private void OnTake(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not CaptureViewModel vm || !vm.HasArea)
            return;
        var rect = Filee.App.Controls.CropView.Clamp(vm.Selection, vm.PictureSize);
        Kept = new PixelRect((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height);
        Close();
    }

    private void OnCancel(object? sender, RoutedEventArgs e) => Close();
}
