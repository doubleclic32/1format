// The annotation editor's state: what the next mark will be, and the marks placed so far.

using Avalonia.Collections;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Filee.Core.Presets;

namespace Filee.App.ViewModels;

/// <summary>One of the shapes offered in the toolbar.</summary>
public sealed partial class KindChoice(string label, AnnotationKind kind) : ObservableObject
{
    public string Label { get; } = label;
    public AnnotationKind Kind { get; } = kind;

    [ObservableProperty]
    private bool _isSelected;
}

/// <summary>One of the colours offered in the toolbar.</summary>
public sealed partial class ColorChoice(string hex) : ObservableObject
{
    public string Hex { get; } = hex;

    [ObservableProperty]
    private bool _isSelected;
}

/// <summary>Draw boxes, ellipses, arrows and text on a picture.</summary>
public sealed partial class AnnotateToolViewModel : ObservableObject
{
    /// <param name="labels">The four shape names in the user's language, in the order of <see cref="AnnotationKind"/>.</param>
    public AnnotateToolViewModel(Bitmap picture, string fileName, IReadOnlyList<string> labels)
    {
        Picture = picture;
        FileName = fileName;
        Kinds =
        [
            new KindChoice(labels[0], AnnotationKind.Rectangle) { IsSelected = true },
            new KindChoice(labels[1], AnnotationKind.Ellipse),
            new KindChoice(labels[2], AnnotationKind.Arrow),
            new KindChoice(labels[3], AnnotationKind.Text),
        ];
        Colors = [new ColorChoice("#E53935") { IsSelected = true }, new ColorChoice("#2563EB"), new ColorChoice("#0F7A5A"),
                  new ColorChoice("#F9A825"), new ColorChoice("#1F1E1D"), new ColorChoice("#FFFFFF")];
    }

    public Bitmap Picture { get; }
    public string FileName { get; }
    public IReadOnlyList<KindChoice> Kinds { get; }
    public IReadOnlyList<ColorChoice> Colors { get; }

    /// <summary>The marks, in the picture's pixels. The control adds to and removes from this list directly.</summary>
    public AvaloniaList<Annotation> Marks { get; } = [];

    [ObservableProperty]
    private AnnotationKind _kind = AnnotationKind.Rectangle;

    [ObservableProperty]
    private string _color = "#E53935";

    [ObservableProperty]
    private int _thickness = 4;

    /// <summary>What a text mark writes; the text tool does nothing while this is empty.</summary>
    [ObservableProperty]
    private string _text = "";

    public bool IsText => Kind == AnnotationKind.Text;
    public int Count => Marks.Count;
    public bool HasMarks => Marks.Count > 0;

    partial void OnKindChanged(AnnotationKind value)
    {
        _ = value;
        OnPropertyChanged(nameof(IsText));
    }

    /// <summary>Called by the view after the control changed the list.</summary>
    public void Refresh()
    {
        OnPropertyChanged(nameof(Count));
        OnPropertyChanged(nameof(HasMarks));
    }

    /// <summary>Writes the marks into the preset the conversion will run.</summary>
    public void WriteInto(Preset preset) => preset.Image.Annotations = [.. Marks];

    [RelayCommand]
    private void ChooseKind(KindChoice choice)
    {
        foreach (var k in Kinds)
            k.IsSelected = ReferenceEquals(k, choice);
        Kind = choice.Kind;
    }

    [RelayCommand]
    private void ChooseColor(ColorChoice choice)
    {
        foreach (var c in Colors)
            c.IsSelected = ReferenceEquals(c, choice);
        Color = choice.Hex;
    }

    [RelayCommand]
    private void Clear()
    {
        Marks.Clear();
        Refresh();
    }
}
