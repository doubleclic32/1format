using Avalonia.Controls;
using Avalonia.Interactivity;
using Filee.App.Services;
using Filee.App.ViewModels;
using Filee.Core.Presets;

namespace Filee.App.Views;

/// <summary>Modal compression editor; <see cref="Applied"/> says whether the settings are to be used.</summary>
public partial class CompressToolWindow : Window
{
    public CompressToolWindow()
    {
        InitializeComponent();
        Motion.Track(this);
        Closed += (_, _) => (DataContext as CompressToolViewModel)?.Dispose();
    }

    public bool Applied { get; private set; }

    /// <summary>Puts what the user settled on into <paramref name="preset"/>.</summary>
    public void WriteInto(Preset preset) => (DataContext as CompressToolViewModel)?.WriteInto(preset);

    private void OnApply(object? sender, RoutedEventArgs e)
    {
        Applied = true;
        Close(true);
    }

    private void OnCancel(object? sender, RoutedEventArgs e) => Close(false);
}
