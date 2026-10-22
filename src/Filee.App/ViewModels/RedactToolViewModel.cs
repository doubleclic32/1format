// The redaction editor's state: the picture and the boxes drawn over it.
//
// The boxes live in the picture's own pixels, like the crop rectangle, so what is applied to the file does not depend
// on how large the picture was shown.

using Avalonia;
using Avalonia.Collections;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Filee.Core.Presets;

namespace Filee.App.ViewModels;

/// <summary>Paint over the parts of a picture to hide.</summary>
public sealed partial class RedactToolViewModel : ObservableObject
{
    public RedactToolViewModel(Bitmap picture, string fileName)
    {
        Picture = picture;
        FileName = fileName;
        PictureSize = new Size(picture.PixelSize.Width, picture.PixelSize.Height);
    }

    public Bitmap Picture { get; }
    public string FileName { get; }
    public Size PictureSize { get; }

    /// <summary>The boxes, in the picture's pixels. The control adds to and removes from this list directly.</summary>
    public AvaloniaList<Rect> Areas { get; } = [];

    public int Count => Areas.Count;

    public bool HasAreas => Areas.Count > 0;

    /// <summary>Called by the view after the control changed the list.</summary>
    public void Refresh()
    {
        OnPropertyChanged(nameof(Count));
        OnPropertyChanged(nameof(HasAreas));
    }

    /// <summary>The boxes as the conversion wants them.</summary>
    public List<CropRect> Result =>
    [
        .. Areas.Select(a => Controls.CropView.Clamp(a, PictureSize))
            .Select(r => new CropRect { X = (int)r.X, Y = (int)r.Y, Width = (int)r.Width, Height = (int)r.Height })
            .Where(r => r.Width > 0 && r.Height > 0),
    ];

    [RelayCommand]
    private void Clear()
    {
        Areas.Clear();
        Refresh();
    }
}
