using Avalonia.Controls;
using Avalonia.Interactivity;
using Filee.App.Services;
using Filee.App.ViewModels;
using Filee.Core.Presets;

namespace Filee.App.Views;

/// <summary>Modal metadata editor; <see cref="Applied"/> says whether the choice is to be used.</summary>
public partial class MetadataToolWindow : Window
{
    public MetadataToolWindow()
    {
        InitializeComponent();
        Motion.Track(this);
    }

    public bool Applied { get; private set; }

    /// <summary>Puts what the user chose into <paramref name="preset"/>.</summary>
    public void WriteInto(Preset preset) => (DataContext as MetadataToolViewModel)?.WriteInto(preset);

    private void OnApply(object? sender, RoutedEventArgs e)
    {
        Applied = true;
        Close(true);
    }

    private void OnCancel(object? sender, RoutedEventArgs e) => Close(false);
}
