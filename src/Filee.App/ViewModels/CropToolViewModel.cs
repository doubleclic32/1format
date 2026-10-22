// The crop editor's state: the picture, the rectangle kept, and the shape it is held at.
//
// The rectangle lives in the picture's own pixels. The width and height boxes show the same numbers the file will
// have, so what the user reads is what they get.

using Avalonia;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Filee.Core.Presets;

namespace Filee.App.ViewModels;

/// <summary>One of the shapes offered above the width and height boxes.</summary>
public sealed partial class AspectChoice(string label, double ratio) : ObservableObject
{
    public string Label { get; } = label;

    /// <summary>Width ÷ height, or 0 for "any shape".</summary>
    public double Ratio { get; } = ratio;

    [ObservableProperty]
    private bool _isSelected;
}

/// <summary>Pick the part of a picture to keep.</summary>
public sealed partial class CropToolViewModel : ObservableObject
{
    private bool _updating;

    /// <param name="freeLabel">Name of the "any shape" choice, in the user's language.</param>
    public CropToolViewModel(Bitmap picture, string fileName, string freeLabel)
    {
        Picture = picture;
        FileName = fileName;
        PictureSize = new Size(picture.PixelSize.Width, picture.PixelSize.Height);
        Aspects =
        [
            new AspectChoice(freeLabel, 0) { IsSelected = true },
            new AspectChoice("1:1", 1),
            new AspectChoice("16:9", 16 / 9.0),
            new AspectChoice("9:16", 9 / 16.0),
            new AspectChoice("4:3", 4 / 3.0),
            new AspectChoice("3:4", 3 / 4.0),
        ];
        _selection = new Rect(0, 0, PictureSize.Width, PictureSize.Height);
        ReadBoxes();
    }

    public Bitmap Picture { get; }
    public string FileName { get; }
    public Size PictureSize { get; }
    public IReadOnlyList<AspectChoice> Aspects { get; }

    [ObservableProperty]
    private Rect _selection;

    [ObservableProperty]
    private double _aspectRatio;

    [ObservableProperty]
    private int _cropWidth;

    [ObservableProperty]
    private int _cropHeight;

    /// <summary>The rectangle as the conversion wants it, or null when the whole picture is kept.</summary>
    public CropRect? Result
    {
        get
        {
            var rect = Controls.CropView.Clamp(Selection, PictureSize);
            var crop = new CropRect
            {
                X = (int)rect.X,
                Y = (int)rect.Y,
                Width = (int)rect.Width,
                Height = (int)rect.Height,
            };
            return crop.Covers((int)PictureSize.Width, (int)PictureSize.Height) ? crop : null;
        }
    }

    partial void OnSelectionChanged(Rect value)
    {
        _ = value;
        ReadBoxes();
    }

    /// <summary>Mirrors the rectangle into the width and height boxes without echoing back into the rectangle.</summary>
    private void ReadBoxes()
    {
        _updating = true;
        CropWidth = (int)Math.Round(Selection.Width);
        CropHeight = (int)Math.Round(Selection.Height);
        _updating = false;
    }

    partial void OnCropWidthChanged(int value) => ResizeFromBoxes(value, CropHeight);

    partial void OnCropHeightChanged(int value) => ResizeFromBoxes(CropWidth, value);

    /// <summary>
    /// Resizes the rectangle from the boxes, keeping its top left corner. Ignored while the boxes are being filled
    /// from the rectangle, otherwise each would push the other.
    /// </summary>
    private void ResizeFromBoxes(int width, int height)
    {
        if (_updating || width <= 0 || height <= 0)
            return;
        if (AspectRatio > 0)
            height = (int)Math.Round(width / AspectRatio);
        Selection = Controls.CropView.Clamp(new Rect(Selection.X, Selection.Y, width, height), PictureSize);
    }

    [RelayCommand]
    private void ChooseAspect(AspectChoice choice)
    {
        foreach (var aspect in Aspects)
            aspect.IsSelected = ReferenceEquals(aspect, choice);
        AspectRatio = choice.Ratio;
        // A shape is a decision about the whole frame: the rectangle is redrawn as large as it fits, centred, rather
        // than squeezed out of whatever was selected.
        Selection = Controls.CropView.Largest(PictureSize, choice.Ratio);
    }

    [RelayCommand]
    private void Reset()
    {
        ChooseAspect(Aspects[0]);
        Selection = new Rect(0, 0, PictureSize.Width, PictureSize.Height);
    }
}
