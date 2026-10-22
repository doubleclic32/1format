// Screen capture: a picture of the desktop, an overlay to draw the area on, and a PNG of that area.
//
// The PNG goes to the temp folder: it is an intermediate file. What the user keeps is whatever the donut then writes
// from it, under the rules of the preset they pick (format, folder, file name).

using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Filee.App.ViewModels;
using Filee.App.Views;
using Filee.Core.Platform;
using Microsoft.Extensions.Logging;

namespace Filee.App.Services;

/// <summary>Takes a picture of part of the screen and writes it as a PNG.</summary>
public sealed class ScreenCaptureService(IPlatformServices platform, ILogger<ScreenCaptureService> log)
{
    /// <summary>True where the platform can take a picture of the screen (Windows today).</summary>
    public bool IsSupported => platform.SupportsScreenCapture;

    /// <summary>
    /// Takes a picture of the desktop, lets the user draw the area to keep and writes it as a PNG.
    /// Returns the file, or null when the screen could not be captured or the user cancelled.
    /// </summary>
    /// <param name="hide">
    /// A window to take out of the way first, usually 1format's own: the picture is of the screen as it is, so a
    /// window left open would be captured along with what the user wanted.
    /// </param>
    public async Task<string?> CaptureAsync(Window? hide = null)
    {
        var wasVisible = hide is { IsVisible: true };
        if (wasVisible)
        {
            hide!.Hide();
            // One beat for the compositor to actually paint the desktop without it; without this the window is
            // still in the picture on a slow machine.
            await Task.Delay(TimeSpan.FromMilliseconds(180));
        }
        try
        {
            return await TakeAsync();
        }
        finally
        {
            if (wasVisible)
            {
                hide!.Show();
                hide.Activate();
            }
        }
    }

    private async Task<string?> TakeAsync()
    {
        if (platform.CaptureScreen() is not { } shot || shot.Width <= 0 || shot.Height <= 0)
        {
            log.LogInformation("Screen capture: the platform returned no picture");
            return null;
        }

        using var picture = FromShot(shot);
        var vm = new CaptureViewModel(picture);
        var window = new CaptureWindow { DataContext = vm };
        // Over the whole virtual desktop. Position is in physical pixels, Width and Height are not: they are
        // device-independent, so on a laptop at 150 per cent the window would be half as large again as the screen
        // and the bottom right corner would be out of reach.
        window.Position = new PixelPoint(shot.Left, shot.Top);
        var scaling = window.Screens.ScreenFromPoint(window.Position)?.Scaling
                      ?? window.Screens.Primary?.Scaling ?? 1;
        window.Width = shot.Width / scaling;
        window.Height = shot.Height / scaling;

        var closed = new TaskCompletionSource();
        window.Closed += (_, _) => closed.TrySetResult();
        window.Show();
        window.Activate();
        await closed.Task;

        if (window.Kept is not { } area)
            return null;

        var file = Path.Combine(Path.GetTempPath(), "1format", "captures",
            $"capture-{DateTime.Now:yyyyMMdd-HHmmss}.png");
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        Write(shot, area, file);
        log.LogInformation("Screen capture: {Width}×{Height} written to {File}", area.Width, area.Height, file);
        return file;
    }

    /// <summary>
    /// The whole picture as a bitmap the overlay can show. It is written out and read back rather than handed a
    /// pointer: a bitmap built on a pinned array keeps using that memory, and the array is no longer pinned once
    /// this method returns.
    /// </summary>
    private static Bitmap FromShot(ScreenShot shot)
    {
        using var stream = new MemoryStream();
        Encode(shot.Pixels, shot.Width, shot.Height, stream);
        stream.Position = 0;
        return new Bitmap(stream);
    }

    /// <summary>Writes <paramref name="area"/> of the picture as a PNG, copying it out row by row.</summary>
    internal static void Write(ScreenShot shot, PixelRect area, string file)
    {
        var rows = new byte[area.Width * area.Height * 4];
        for (var y = 0; y < area.Height; y++)
            Array.Copy(shot.Pixels, (area.Y + y) * shot.Stride + area.X * 4, rows, y * area.Width * 4, area.Width * 4);

        using var stream = File.Create(file);
        Encode(rows, area.Width, area.Height, stream);
    }

    /// <summary>Writes BGRA pixels as a PNG, the array held pinned for as long as the bitmap reads from it.</summary>
    private static void Encode(byte[] pixels, int width, int height, Stream into)
    {
        var handle = System.Runtime.InteropServices.GCHandle.Alloc(pixels, System.Runtime.InteropServices.GCHandleType.Pinned);
        try
        {
            using var bitmap = new Bitmap(PixelFormat.Bgra8888, AlphaFormat.Opaque, handle.AddrOfPinnedObject(),
                new PixelSize(width, height), new Vector(96, 96), width * 4);
            bitmap.Save(into);
        }
        finally
        {
            handle.Free();
        }
    }
}
