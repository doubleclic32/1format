using Avalonia.Controls;
using Avalonia.Interactivity;
using Filee.App.Services;
using Filee.App.ViewModels;
using Filee.Core.Presets;

namespace Filee.App.Views;

/// <summary>Modal annotation editor; <see cref="Applied"/> says whether the marks are to be used.</summary>
public partial class AnnotateToolWindow : Window
{
    public AnnotateToolWindow()
    {
        InitializeComponent();
        Motion.Track(this);
        Opened += (_, _) => Canvas.Focus();
        Canvas.MarksChanged += (_, _) => (DataContext as AnnotateToolViewModel)?.Refresh();
    }

    public bool Applied { get; private set; }

    /// <summary>Puts the marks into <paramref name="preset"/>.</summary>
    public void WriteInto(Preset preset) => (DataContext as AnnotateToolViewModel)?.WriteInto(preset);

    private void OnApply(object? sender, RoutedEventArgs e)
    {
        Applied = true;
        Close(true);
    }

    private void OnCancel(object? sender, RoutedEventArgs e) => Close(false);
}
