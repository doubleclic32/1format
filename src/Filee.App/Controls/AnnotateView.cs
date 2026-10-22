// The marks drawn on a picture: boxes, ellipses, arrows and text.
//
// Drawn by hand like DonutMenu and CropView, and on purpose the same way the engine draws them (ImageEncoder.Annotate):
// what the window shows has to be what the file gets, so the two sets of shapes are kept side by side in the code and
// the arrow head uses the same angle and the same length rule.
//
// Marks live in the picture's own pixels, never in screen pixels: the window can be resized and the file must not
// change because of it.

using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Filee.Core.Presets;

namespace Filee.App.Controls;

/// <summary>Picture with the marks drawn on it, and the mouse to put more there.</summary>
public sealed class AnnotateView : Control
{
    /// <summary>Shortest drag that counts as a mark rather than a click meant to delete one.</summary>
    private const double MinDrag = 6;

    /// <summary>How close to a line counts as pointing at it, in screen pixels.</summary>
    private const double LineReach = 6;

    public static readonly StyledProperty<Bitmap?> SourceProperty =
        AvaloniaProperty.Register<AnnotateView, Bitmap?>(nameof(Source));

    /// <summary>The marks, in the picture's own pixels. The control adds to and removes from this list.</summary>
    public static readonly StyledProperty<IList<Annotation>?> MarksProperty =
        AvaloniaProperty.Register<AnnotateView, IList<Annotation>?>(nameof(Marks));

    /// <summary>What the next drag draws.</summary>
    public static readonly StyledProperty<AnnotationKind> KindProperty =
        AvaloniaProperty.Register<AnnotateView, AnnotationKind>(nameof(Kind));

    /// <summary>Colour of the next mark, as <c>#RRGGBB</c>.</summary>
    public static readonly StyledProperty<string> ColorProperty =
        AvaloniaProperty.Register<AnnotateView, string>(nameof(Color), "#E53935");

    /// <summary>Line width of the next mark, in pixels of the picture.</summary>
    public static readonly StyledProperty<int> ThicknessProperty =
        AvaloniaProperty.Register<AnnotateView, int>(nameof(Thickness), 4);

    /// <summary>What a text mark writes.</summary>
    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<AnnotateView, string?>(nameof(Text));

    static AnnotateView()
    {
        AffectsRender<AnnotateView>(SourceProperty, MarksProperty, KindProperty, ColorProperty, ThicknessProperty);
        FocusableProperty.OverrideDefaultValue<AnnotateView>(true);
    }

    public Bitmap? Source { get => GetValue(SourceProperty); set => SetValue(SourceProperty, value); }
    public IList<Annotation>? Marks { get => GetValue(MarksProperty); set => SetValue(MarksProperty, value); }
    public AnnotationKind Kind { get => GetValue(KindProperty); set => SetValue(KindProperty, value); }
    public string Color { get => GetValue(ColorProperty); set => SetValue(ColorProperty, value); }
    public int Thickness { get => GetValue(ThicknessProperty); set => SetValue(ThicknessProperty, value); }
    public string? Text { get => GetValue(TextProperty); set => SetValue(TextProperty, value); }

    /// <summary>Raised after a mark was added or removed.</summary>
    public event EventHandler? MarksChanged;

    private Point? _from;
    private Point _to;

    private Size? PictureSize => Source is { } b && b.PixelSize.Width > 0 && b.PixelSize.Height > 0
        ? new Size(b.PixelSize.Width, b.PixelSize.Height)
        : null;

    private Rect PictureBounds
    {
        get
        {
            if (PictureSize is not { } size)
                return default;
            var scale = Math.Min(Bounds.Width / size.Width, Bounds.Height / size.Height);
            var width = size.Width * scale;
            var height = size.Height * scale;
            return new Rect((Bounds.Width - width) / 2, (Bounds.Height - height) / 2, width, height);
        }
    }

    /// <summary>Picture pixels per screen pixel.</summary>
    private double Scale => PictureSize is { } size && PictureBounds.Width > 0 ? size.Width / PictureBounds.Width : 1;

    private Point ToScreen(double x, double y)
    {
        var bounds = PictureBounds;
        return new Point(bounds.X + x / Scale, bounds.Y + y / Scale);
    }

    private Point ToPicture(Point at)
    {
        var bounds = PictureBounds;
        return new Point((at.X - bounds.X) * Scale, (at.Y - bounds.Y) * Scale);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property != MarksProperty)
            return;
        if (change.OldValue is INotifyCollectionChanged old)
            old.CollectionChanged -= OnMarksCollectionChanged;
        if (change.NewValue is INotifyCollectionChanged added)
            added.CollectionChanged += OnMarksCollectionChanged;
    }

    private void OnMarksCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => InvalidateVisual();

    public override void Render(DrawingContext context)
    {
        if (Source is not { } bitmap || PictureSize is not { } size)
            return;
        context.DrawImage(bitmap, new Rect(0, 0, size.Width, size.Height), PictureBounds);

        foreach (var mark in Marks ?? [])
            Draw(context, mark);

        // The mark being dragged, drawn like the others so the user sees exactly what they are about to get.
        if (_from is { } from)
            Draw(context, Build(from, _to));
    }

    private void Draw(DrawingContext context, Annotation mark)
    {
        var brush = new SolidColorBrush(ParseColor(mark.Color));
        // The line is scaled to the screen like everything else, so a 4-pixel line on a large photo does not look
        // like a hairline here and a bar once written.
        var pen = new Pen(brush, Math.Max(1, mark.Thickness / Scale));
        var a = ToScreen(mark.X, mark.Y);
        var b = ToScreen(mark.X2, mark.Y2);
        switch (mark.Kind)
        {
            case AnnotationKind.Rectangle:
                context.DrawRectangle(null, pen, new Rect(a, b));
                break;
            case AnnotationKind.Ellipse:
                var rect = new Rect(a, b);
                context.DrawEllipse(null, pen, rect.Center, rect.Width / 2, rect.Height / 2);
                break;
            case AnnotationKind.Arrow:
                context.DrawLine(pen, a, b);
                foreach (var head in ArrowHead(mark))
                    context.DrawLine(pen, b, ToScreen(head.X, head.Y));
                break;
            case AnnotationKind.Text:
                if (string.IsNullOrWhiteSpace(mark.Text))
                    return;
                var text = new FormattedText(mark.Text, System.Globalization.CultureInfo.CurrentUICulture,
                    FlowDirection.LeftToRight, Typeface.Default, Math.Max(6, mark.FontSize / Scale), brush);
                context.DrawText(text, a);
                break;
        }
    }

    /// <summary>The same head as <c>ImageEncoder.ArrowHead</c>, in the picture's pixels.</summary>
    private static IEnumerable<(double X, double Y)> ArrowHead(Annotation mark)
    {
        var angle = Math.Atan2(mark.Y - mark.Y2, mark.X - mark.X2);
        var length = Math.Max(10, Math.Clamp(mark.Thickness, 1, 100) * 4);
        const double Spread = 25 * Math.PI / 180;
        yield return (mark.X2 + length * Math.Cos(angle - Spread), mark.Y2 + length * Math.Sin(angle - Spread));
        yield return (mark.X2 + length * Math.Cos(angle + Spread), mark.Y2 + length * Math.Sin(angle + Spread));
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (PictureSize is null || Marks is null)
            return;
        var at = e.GetPosition(this);
        if (!PictureBounds.Contains(at))
            return;

        // Topmost first: a mark drawn later sits over an earlier one.
        if (PointedAt(at) is { } index)
        {
            Marks.RemoveAt(index);
            MarksChanged?.Invoke(this, EventArgs.Empty);
            InvalidateVisual();
            e.Handled = true;
            return;
        }

        // Text is placed with one click: there is nothing to drag out.
        if (Kind == AnnotationKind.Text)
        {
            if (string.IsNullOrWhiteSpace(Text))
                return;
            var point = ToPicture(at);
            Marks.Add(Build(point, point));
            MarksChanged?.Invoke(this, EventArgs.Empty);
            InvalidateVisual();
            e.Handled = true;
            return;
        }

        _from = ToPicture(at);
        _to = _from.Value;
        Focus();
        e.Pointer.Capture(this);
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_from is null)
            return;
        _to = ToPicture(e.GetPosition(this));
        InvalidateVisual();
        e.Handled = true;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        e.Pointer.Capture(null);
        if (_from is not { } from || Marks is null)
            return;
        _from = null;
        var dragged = Math.Abs(_to.X - from.X) + Math.Abs(_to.Y - from.Y);
        if (dragged * (1 / Scale) >= MinDrag)
        {
            Marks.Add(Build(from, _to));
            MarksChanged?.Invoke(this, EventArgs.Empty);
        }
        InvalidateVisual();
    }

    /// <summary>A mark built from a drag, with the current kind, colour and width.</summary>
    private Annotation Build(Point from, Point to) => new()
    {
        Kind = Kind,
        X = (int)Math.Round(from.X),
        Y = (int)Math.Round(from.Y),
        X2 = (int)Math.Round(to.X),
        Y2 = (int)Math.Round(to.Y),
        Color = Color,
        Thickness = Thickness,
        Text = Text,
        FontSize = Math.Max(8, Thickness * 7),
    };

    /// <summary>Index of the mark under <paramref name="at"/>, topmost first, or null.</summary>
    private int? PointedAt(Point at)
    {
        if (Marks is null)
            return null;
        for (var i = Marks.Count - 1; i >= 0; i--)
        {
            var mark = Marks[i];
            var a = ToScreen(mark.X, mark.Y);
            var b = ToScreen(mark.X2, mark.Y2);
            var hit = mark.Kind switch
            {
                AnnotationKind.Arrow => NearLine(at, a, b),
                // A box or an ellipse is pointed at by its outline, not its inside: the inside is the picture, and a
                // click there is how the user starts a new mark over it.
                AnnotationKind.Rectangle or AnnotationKind.Ellipse => NearOutline(at, new Rect(a, b)),
                AnnotationKind.Text => new Rect(a, new Size(Math.Max(20, (mark.Text?.Length ?? 1) * mark.FontSize / Scale * 0.6),
                    mark.FontSize / Scale)).Contains(at),
                _ => false,
            };
            if (hit)
                return i;
        }
        return null;
    }

    private static bool NearOutline(Point at, Rect rect) =>
        rect.Inflate(LineReach).Contains(at) && !rect.Deflate(LineReach).Contains(at);

    private static bool NearLine(Point at, Point a, Point b)
    {
        var dx = b.X - a.X;
        var dy = b.Y - a.Y;
        var squared = dx * dx + dy * dy;
        if (squared < 0.001)
            return Math.Abs(at.X - a.X) + Math.Abs(at.Y - a.Y) <= LineReach;
        var t = Math.Clamp(((at.X - a.X) * dx + (at.Y - a.Y) * dy) / squared, 0, 1);
        var closest = new Point(a.X + t * dx, a.Y + t * dy);
        return Math.Sqrt(Math.Pow(at.X - closest.X, 2) + Math.Pow(at.Y - closest.Y, 2)) <= LineReach;
    }

    private static Color ParseColor(string hex) =>
        Avalonia.Media.Color.TryParse(hex, out var parsed) ? parsed : Colors.Red;
}
