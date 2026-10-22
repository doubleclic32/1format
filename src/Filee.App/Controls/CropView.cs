// The crop rectangle drawn over a picture: the part kept is shown as it is, the rest is dimmed, and eight handles
// plus the inside of the rectangle are dragged with the mouse or the keyboard.
//
// Drawn by hand, like DonutMenu, so the handles can be hit-tested by their own squares rather than by child controls
// stacked over the image.
//
// The rectangle is held in the picture's own pixels (Rect, see CropRect), never in screen pixels: the window can be
// resized, and what is applied to the file must not depend on how large it was shown.

using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace Filee.App.Controls;

/// <summary>Which part of the rectangle the pointer grabbed.</summary>
internal enum CropGrip
{
    None,
    Inside,
    Left,
    Right,
    Top,
    Bottom,
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight,
}

/// <summary>Picture with a draggable crop rectangle over it.</summary>
public sealed class CropView : Control
{
    /// <summary>
    /// True when a press outside the rectangle starts a new one from that point, the way a screen capture is drawn.
    /// False (the crop editor) leaves the rectangle alone, so a stray click never throws the framing away.
    /// </summary>
    public static readonly StyledProperty<bool> DrawsNewProperty =
        AvaloniaProperty.Register<CropView, bool>(nameof(DrawsNew));

    /// <summary>
    /// Set to a list to put the control in redaction mode: each drag adds a box to it, a press inside a box removes
    /// that box, and nothing is dimmed. Null (the default) is the crop editor, with its single rectangle.
    /// </summary>
    public static readonly StyledProperty<IList<Rect>?> RedactionsProperty =
        AvaloniaProperty.Register<CropView, IList<Rect>?>(nameof(Redactions));

    /// <summary>Side of a handle in screen pixels, and the reach around it that still grabs it.</summary>
    private const double HandleSize = 10;
    private const double GrabSlack = 6;

    /// <summary>Smallest rectangle, in picture pixels: below this the handles overlap and nothing can be grabbed.</summary>
    public const int MinSide = 8;

    public static readonly StyledProperty<Bitmap?> SourceProperty =
        AvaloniaProperty.Register<CropView, Bitmap?>(nameof(Source));

    /// <summary>The kept rectangle, in the picture's own pixels.</summary>
    public static readonly StyledProperty<Rect> SelectionProperty =
        AvaloniaProperty.Register<CropView, Rect>(nameof(Selection), defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    /// <summary>Width ÷ height the rectangle is held at; 0 lets it take any shape.</summary>
    public static readonly StyledProperty<double> AspectRatioProperty =
        AvaloniaProperty.Register<CropView, double>(nameof(AspectRatio));

    public static readonly StyledProperty<IBrush?> HandleBrushProperty =
        AvaloniaProperty.Register<CropView, IBrush?>(nameof(HandleBrush), Brushes.White);

    public static readonly StyledProperty<IBrush?> AccentBrushProperty =
        AvaloniaProperty.Register<CropView, IBrush?>(nameof(AccentBrush), Brushes.DodgerBlue);

    static CropView()
    {
        AffectsRender<CropView>(SourceProperty, SelectionProperty, HandleBrushProperty, AccentBrushProperty,
            RedactionsProperty);
        FocusableProperty.OverrideDefaultValue<CropView>(true);
    }

    public Bitmap? Source { get => GetValue(SourceProperty); set => SetValue(SourceProperty, value); }
    public Rect Selection { get => GetValue(SelectionProperty); set => SetValue(SelectionProperty, value); }
    public double AspectRatio { get => GetValue(AspectRatioProperty); set => SetValue(AspectRatioProperty, value); }
    public bool DrawsNew { get => GetValue(DrawsNewProperty); set => SetValue(DrawsNewProperty, value); }
    public IList<Rect>? Redactions { get => GetValue(RedactionsProperty); set => SetValue(RedactionsProperty, value); }

    /// <summary>Redaction mode: boxes are added and removed instead of one rectangle being framed.</summary>
    private bool IsRedacting => Redactions is not null;

    /// <summary>Raised after a box was added or removed, so the window can refresh its count.</summary>
    public event EventHandler? RedactionsChanged;

    /// <summary>
    /// Repaints when the list of boxes changes. AffectsRender only watches the property, so it fires when the list is
    /// swapped and never when a box is added to it, which left boxes invisible until something else redrew.
    /// </summary>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property != RedactionsProperty)
            return;
        if (change.OldValue is INotifyCollectionChanged old)
            old.CollectionChanged -= OnRedactionsCollectionChanged;
        if (change.NewValue is INotifyCollectionChanged added)
            added.CollectionChanged += OnRedactionsCollectionChanged;
    }

    private void OnRedactionsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => InvalidateVisual();
    public IBrush? HandleBrush { get => GetValue(HandleBrushProperty); set => SetValue(HandleBrushProperty, value); }
    public IBrush? AccentBrush { get => GetValue(AccentBrushProperty); set => SetValue(AccentBrushProperty, value); }

    private CropGrip _grip;
    private Point _grabbedAt;
    private Rect _grabbedSelection;

    /// <summary>The picture's size in its own pixels, or null while no picture is loaded.</summary>
    private Size? PictureSize => Source is { } b && b.PixelSize.Width > 0 && b.PixelSize.Height > 0
        ? new Size(b.PixelSize.Width, b.PixelSize.Height)
        : null;

    /// <summary>Where the picture is drawn inside the control, fitted while keeping its shape.</summary>
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

    /// <summary>Picture pixels per screen pixel; 1 while nothing is loaded.</summary>
    private double Scale => PictureSize is { } size && PictureBounds.Width > 0 ? size.Width / PictureBounds.Width : 1;

    /// <summary>A point of the control in the picture's own pixels.</summary>
    private Point ToPicture(Point at)
    {
        var bounds = PictureBounds;
        return new Point((at.X - bounds.X) * Scale, (at.Y - bounds.Y) * Scale);
    }

    private Rect ToScreen(Rect picture)
    {
        var bounds = PictureBounds;
        var s = 1 / Scale;
        return new Rect(bounds.X + picture.X * s, bounds.Y + picture.Y * s, picture.Width * s, picture.Height * s);
    }

    public override void Render(DrawingContext context)
    {
        if (Source is not { } bitmap || PictureSize is not { } size)
            return;
        var bounds = PictureBounds;
        context.DrawImage(bitmap, new Rect(0, 0, size.Width, size.Height), bounds);

        if (IsRedacting)
        {
            // The boxes are painted the way the file will be: opaque black, so what the user sees is the result.
            foreach (var area in Redactions!)
                context.FillRectangle(Brushes.Black, ToScreen(area));
            if (Selection.Width >= MinSide && Selection.Height >= MinSide)
            {
                var drawing = ToScreen(Selection);
                context.FillRectangle(new SolidColorBrush(Colors.Black, 0.6), drawing);
                context.DrawRectangle(null, new Pen(AccentBrush, 1.5), drawing);
            }
            return;
        }

        var selection = ToScreen(Clamp(Selection, size));

        // Everything outside the rectangle is dimmed, in four bands rather than one geometry with a hole: four
        // rectangles are cheaper to fill and leave no seam where the hole's edge meets the band.
        var shade = new SolidColorBrush(Colors.Black, 0.45);
        context.FillRectangle(shade, new Rect(bounds.X, bounds.Y, bounds.Width, selection.Y - bounds.Y));
        context.FillRectangle(shade, new Rect(bounds.X, selection.Bottom, bounds.Width, bounds.Bottom - selection.Bottom));
        context.FillRectangle(shade, new Rect(bounds.X, selection.Y, selection.X - bounds.X, selection.Height));
        context.FillRectangle(shade, new Rect(selection.Right, selection.Y, bounds.Right - selection.Right, selection.Height));

        // Thirds, the usual guide for framing a picture.
        var guide = new Pen(new SolidColorBrush(Colors.White, 0.35), 1);
        for (var i = 1; i < 3; i++)
        {
            var x = selection.X + selection.Width * i / 3;
            var y = selection.Y + selection.Height * i / 3;
            context.DrawLine(guide, new Point(x, selection.Y), new Point(x, selection.Bottom));
            context.DrawLine(guide, new Point(selection.X, y), new Point(selection.Right, y));
        }

        context.DrawRectangle(null, new Pen(AccentBrush, 1.5), selection);
        foreach (var (grip, at) in Handles(selection))
        {
            _ = grip;
            context.DrawRectangle(HandleBrush, new Pen(AccentBrush, 1.5),
                new Rect(at.X - HandleSize / 2, at.Y - HandleSize / 2, HandleSize, HandleSize), 2, 2);
        }
    }

    private static IEnumerable<(CropGrip Grip, Point At)> Handles(Rect s) =>
    [
        (CropGrip.TopLeft, s.TopLeft), (CropGrip.TopRight, s.TopRight),
        (CropGrip.BottomLeft, s.BottomLeft), (CropGrip.BottomRight, s.BottomRight),
        (CropGrip.Left, new Point(s.X, s.Y + s.Height / 2)), (CropGrip.Right, new Point(s.Right, s.Y + s.Height / 2)),
        (CropGrip.Top, new Point(s.X + s.Width / 2, s.Y)), (CropGrip.Bottom, new Point(s.X + s.Width / 2, s.Bottom)),
    ];

    /// <summary>What the pointer is over: a handle first (they sit on the edge), then the inside.</summary>
    internal CropGrip GripAt(Point p)
    {
        if (PictureSize is null || IsRedacting)
            return CropGrip.None;
        var selection = ToScreen(Selection);
        var reach = HandleSize / 2 + GrabSlack;
        foreach (var (grip, at) in Handles(selection))
            if (Math.Abs(p.X - at.X) <= reach && Math.Abs(p.Y - at.Y) <= reach)
                return grip;
        return selection.Contains(p) ? CropGrip.Inside : CropGrip.None;
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        var at = e.GetPosition(this);
        if (IsRedacting && PictureSize is not null)
        {
            // Topmost first: boxes drawn later sit over earlier ones, and that is the one the user points at.
            var hit = Redactions!.Select((area, i) => (area, i)).LastOrDefault(x => ToScreen(x.area).Contains(at));
            if (hit.area.Width > 0)
            {
                Redactions.RemoveAt(hit.i);
                RedactionsChanged?.Invoke(this, EventArgs.Empty);
                InvalidateVisual();
                e.Handled = true;
                return;
            }
        }

        _grip = GripAt(at);
        if (_grip == CropGrip.None && DrawsNew && PictureSize is { } size && PictureBounds.Contains(at))
        {
            // A new rectangle starts empty under the pointer and grows from its top left corner as the drag goes on.
            var start = ToPicture(at);
            Selection = Clamp(new Rect(start.X, start.Y, MinSide, MinSide), size);
            _grip = CropGrip.BottomRight;
        }
        if (_grip == CropGrip.None)
            return;
        _grabbedAt = at;
        _grabbedSelection = Selection;
        Focus();
        e.Pointer.Capture(this);
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        var at = e.GetPosition(this);
        Cursor = new Cursor(CursorFor(_grip == CropGrip.None ? GripAt(at) : _grip));
        if (_grip == CropGrip.None || PictureSize is not { } size)
            return;
        var dx = (at.X - _grabbedAt.X) * Scale;
        var dy = (at.Y - _grabbedAt.Y) * Scale;
        Selection = Clamp(Resize(_grabbedSelection, _grip, dx, dy, size), size);
        e.Handled = true;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (IsRedacting && _grip != CropGrip.None && Selection.Width >= MinSide && Selection.Height >= MinSide)
        {
            Redactions!.Add(Selection);
            Selection = default;
            RedactionsChanged?.Invoke(this, EventArgs.Empty);
        }
        _grip = CropGrip.None;
        e.Pointer.Capture(null);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (PictureSize is not { } size)
            return;
        // Arrows move the rectangle, Shift + arrows resize it from the bottom right corner; 10 px at a time with Ctrl.
        var step = e.KeyModifiers.HasFlag(KeyModifiers.Control) ? 10 : 1;
        var (dx, dy) = e.Key switch
        {
            Key.Left => (-step, 0),
            Key.Right => (step, 0),
            Key.Up => (0, -step),
            Key.Down => (0, step),
            _ => (0, 0),
        };
        if (dx == 0 && dy == 0)
            return;
        var grip = e.KeyModifiers.HasFlag(KeyModifiers.Shift) ? CropGrip.BottomRight : CropGrip.Inside;
        Selection = Clamp(Resize(Selection, grip, dx, dy, size), size);
        e.Handled = true;
    }

    private static StandardCursorType CursorFor(CropGrip grip) => grip switch
    {
        CropGrip.Inside => StandardCursorType.SizeAll,
        CropGrip.Left or CropGrip.Right => StandardCursorType.SizeWestEast,
        CropGrip.Top or CropGrip.Bottom => StandardCursorType.SizeNorthSouth,
        CropGrip.TopLeft or CropGrip.BottomRight => StandardCursorType.TopLeftCorner,
        CropGrip.TopRight or CropGrip.BottomLeft => StandardCursorType.TopRightCorner,
        _ => StandardCursorType.Arrow,
    };

    /// <summary>Moves or resizes <paramref name="from"/> by a drag of (dx, dy) picture pixels on <paramref name="grip"/>.</summary>
    private Rect Resize(Rect from, CropGrip grip, double dx, double dy, Size picture)
    {
        if (grip == CropGrip.Inside)
            return from.WithX(from.X + dx).WithY(from.Y + dy);

        var left = from.X;
        var top = from.Y;
        var right = from.Right;
        var bottom = from.Bottom;
        if (grip is CropGrip.Left or CropGrip.TopLeft or CropGrip.BottomLeft)
            left = Math.Min(right - MinSide, left + dx);
        if (grip is CropGrip.Right or CropGrip.TopRight or CropGrip.BottomRight)
            right = Math.Max(left + MinSide, right + dx);
        if (grip is CropGrip.Top or CropGrip.TopLeft or CropGrip.TopRight)
            top = Math.Min(bottom - MinSide, top + dy);
        if (grip is CropGrip.Bottom or CropGrip.BottomLeft or CropGrip.BottomRight)
            bottom = Math.Max(top + MinSide, bottom + dy);

        var rect = new Rect(left, top, right - left, bottom - top);
        return AspectRatio > 0 ? KeepShape(rect, grip, AspectRatio, picture) : rect;
    }

    /// <summary>
    /// Forces the rectangle back to <paramref name="ratio"/>, keeping the corner opposite the one being dragged still.
    /// The side that changed drives the other, so dragging a vertical edge does not fight the ratio.
    /// </summary>
    private static Rect KeepShape(Rect rect, CropGrip grip, double ratio, Size picture)
    {
        var width = rect.Width;
        var height = rect.Height;
        if (grip is CropGrip.Left or CropGrip.Right)
            height = width / ratio;
        else if (grip is CropGrip.Top or CropGrip.Bottom)
            width = height * ratio;
        else if (width / height > ratio)
            width = height * ratio;
        else
            height = width / ratio;

        // Never larger than the picture, still at the ratio.
        var shrink = Math.Min(1, Math.Min(picture.Width / width, picture.Height / height));
        width *= shrink;
        height *= shrink;

        var x = grip is CropGrip.Left or CropGrip.TopLeft or CropGrip.BottomLeft ? rect.Right - width : rect.X;
        var y = grip is CropGrip.Top or CropGrip.TopLeft or CropGrip.TopRight ? rect.Bottom - height : rect.Y;
        return new Rect(x, y, width, height);
    }

    /// <summary>Keeps the rectangle inside the picture and at least <see cref="MinSide"/> on each side.</summary>
    public static Rect Clamp(Rect rect, Size picture)
    {
        // The floor gives way to the picture: Math.Clamp throws when its minimum is above its maximum, and an icon
        // or a stamp of a few pixels would have closed the tool on the first move of the mouse.
        var width = Math.Clamp(Math.Round(rect.Width), Math.Min(MinSide, picture.Width), picture.Width);
        var height = Math.Clamp(Math.Round(rect.Height), Math.Min(MinSide, picture.Height), picture.Height);
        var x = Math.Clamp(Math.Round(rect.X), 0, picture.Width - width);
        var y = Math.Clamp(Math.Round(rect.Y), 0, picture.Height - height);
        return new Rect(x, y, width, height);
    }

    /// <summary>The largest rectangle of <paramref name="ratio"/> that fits the picture, centred (0 means the whole picture).</summary>
    public static Rect Largest(Size picture, double ratio)
    {
        if (ratio <= 0)
            return new Rect(0, 0, picture.Width, picture.Height);
        var width = Math.Min(picture.Width, picture.Height * ratio);
        var height = width / ratio;
        return Clamp(new Rect((picture.Width - width) / 2, (picture.Height - height) / 2, width, height), picture);
    }
}
