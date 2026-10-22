// Shared by the tools whose settings change what the picture looks like or what it weighs (compression, retouching):
// it re-encodes the picture off the UI thread as the knobs move and hands back the result to show.
//
// Encoding a large photo takes long enough to be felt, and a slider sends a value per frame, so each request cancels
// the one before it by generation number. Only the newest result is ever published: an older encode that finishes
// late is dropped rather than overwriting a newer one.

using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Filee.Core.Presets;
using Filee.Engines.Magick;

namespace Filee.App.ViewModels;

/// <summary>Re-encodes a picture as a tool's settings change, and says what it would weigh.</summary>
public sealed partial class ToolPreview(ImagePreview source, string targetFormat) : ObservableObject, IDisposable
{
    private int _generation;
    private bool _disposed;

    /// <summary>
    /// Held around every use of the source picture. ImageMagick refuses to be used from two threads at once, and a
    /// window closed mid-encode would otherwise free the picture under a clone that is still running.
    /// </summary>
    private readonly Lock _gate = new();

    /// <summary>The picture as it would be written with the current settings; the source until the first result.</summary>
    [ObservableProperty]
    private Bitmap? _image;

    /// <summary>Length in bytes of the picture as it would be written, or null while it is being worked out.</summary>
    [ObservableProperty]
    private long? _length;

    /// <summary>Size in pixels of the result, as "1600 × 900".</summary>
    [ObservableProperty]
    private string _size = "";

    /// <summary>True while an encode is running, so the window can say so instead of showing a stale number.</summary>
    [ObservableProperty]
    private bool _isWorking;

    public long OriginalLength => source.OriginalLength;

    /// <summary>How much smaller the result is, in percent; negative when the settings made it bigger.</summary>
    public int? Saved => Length is { } length && OriginalLength > 0
        ? (int)Math.Round(100.0 * (OriginalLength - length) / OriginalLength)
        : null;

    partial void OnLengthChanged(long? value)
    {
        _ = value;
        OnPropertyChanged(nameof(Saved));
    }

    /// <summary>
    /// Asks for the picture to be encoded again with <paramref name="options"/>. Returns at once; the result lands on
    /// the UI thread later, and only if no newer request started meanwhile.
    /// </summary>
    public void Refresh(ImageOptions options)
    {
        if (_disposed)
            return;
        var generation = Interlocked.Increment(ref _generation);
        IsWorking = true;
        // A copy: the caller's options keep changing while this runs.
        var frozen = options.Clone();
        Task.Run(() =>
        {
            EncodedImage? encoded;
            lock (_gate)
            {
                // Overtaken while waiting for the picture: ten notches on a slider must not pay for ten encodes.
                if (_disposed || generation != Volatile.Read(ref _generation))
                    return;
                encoded = source.Encode(frozen, targetFormat);
            }
            Dispatcher.UIThread.Post(() => Publish(generation, encoded));
        });
    }

    private void Publish(int generation, EncodedImage? encoded)
    {
        // An older encode that finished late: its picture is not the one the knobs are showing any more.
        if (_disposed || generation != Volatile.Read(ref _generation))
            return;
        IsWorking = false;
        if (encoded is null)
            return;
        var previous = Image;
        using var stream = new MemoryStream(encoded.Bytes);
        try
        {
            Image = new Bitmap(stream);
        }
        catch (ArgumentException)
        {
            // A format Avalonia cannot show (ICO sets, some TIFFs): the weight is still worth showing.
            Image = null;
        }
        previous?.Dispose();
        Length = encoded.Length;
        Size = $"{encoded.Width} × {encoded.Height}";
    }

    public void Dispose()
    {
        _disposed = true;
        Image?.Dispose();
        // Under the gate: an encode may still hold the picture.
        lock (_gate)
            source.Dispose();
    }

    /// <summary>A byte count as the window shows it: "1,4 Mo", "812 ko".</summary>
    public static string Human(long bytes) => bytes switch
    {
        >= 1024 * 1024 => $"{bytes / (1024.0 * 1024):0.#} Mo",
        >= 1024 => $"{bytes / 1024.0:0} ko",
        _ => $"{bytes} o",
    };
}
