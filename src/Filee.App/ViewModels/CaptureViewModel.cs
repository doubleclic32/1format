// The screen capture overlay's state: the picture of the desktop and the area the user is drawing on it.

using Avalonia;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Filee.App.ViewModels;

/// <summary>Draw the part of the screen to keep.</summary>
public sealed partial class CaptureViewModel : ObservableObject
{
    public CaptureViewModel(Bitmap picture)
    {
        Picture = picture;
        PictureSize = new Size(picture.PixelSize.Width, picture.PixelSize.Height);
        // Nothing framed yet: the first press draws the area. A rectangle covering everything would look like a
        // choice already made.
        _selection = new Rect(0, 0, 0, 0);
    }

    public Bitmap Picture { get; }
    public Size PictureSize { get; }

    [ObservableProperty]
    private Rect _selection;

    /// <summary>The size of the area being drawn, for the bar; empty while nothing is drawn.</summary>
    public string SizeLabel => Selection.Width < 1 || Selection.Height < 1
        ? ""
        : $"{(int)Math.Round(Selection.Width)} × {(int)Math.Round(Selection.Height)} px";

    /// <summary>True once an area worth keeping has been drawn.</summary>
    public bool HasArea => Selection.Width >= Controls.CropView.MinSide && Selection.Height >= Controls.CropView.MinSide;

    partial void OnSelectionChanged(Rect value)
    {
        _ = value;
        OnPropertyChanged(nameof(SizeLabel));
        OnPropertyChanged(nameof(HasArea));
    }
}
