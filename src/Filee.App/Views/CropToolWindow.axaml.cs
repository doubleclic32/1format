using Avalonia.Controls;
using Avalonia.Interactivity;
using Filee.App.Services;
using Filee.App.ViewModels;
using Filee.Core.Presets;

namespace Filee.App.Views;

/// <summary>
/// Modal crop editor. <see cref="Result"/> holds the rectangle the user settled on once the window closes, and is
/// null when they cancelled or kept the whole picture.
/// </summary>
public partial class CropToolWindow : Window
{
    public CropToolWindow()
    {
        InitializeComponent();
        Motion.Track(this);
        Opened += (_, _) => Crop.Focus();
    }

    /// <summary>The rectangle to crop with, or null when nothing is to be cut.</summary>
    public CropRect? Result { get; private set; }

    /// <summary>True once the user applied: a cancelled window converts nothing at all.</summary>
    public bool Applied { get; private set; }

    private void OnApply(object? sender, RoutedEventArgs e)
    {
        Result = (DataContext as CropToolViewModel)?.Result;
        Applied = true;
        Close(true);
    }

    private void OnCancel(object? sender, RoutedEventArgs e) => Close(false);
}
