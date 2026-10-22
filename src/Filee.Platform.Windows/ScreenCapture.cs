// A picture of the whole desktop, taken with GDI.
//
// BitBlt from the screen device context into a 32-bit DIB, then GetDIBits to read the pixels out. A top-down DIB is
// asked for (a negative height), so rows come back in the order every image library expects instead of bottom-up.
//
// The virtual screen covers every monitor, including those left of or above the primary one, which is why the picture
// carries where it was taken (ScreenShot.Left / Top can be negative).

using System.Runtime.InteropServices;
using Filee.Core.Platform;

namespace Filee.Platform.Windows;

internal static class ScreenCapture
{
    /// <summary>The whole desktop, or null when any GDI call failed (a locked session, a remote desktop that left).</summary>
    public static ScreenShot? Take()
    {
        var left = NativeMethods.GetSystemMetrics(NativeMethods.SM_XVIRTUALSCREEN);
        var top = NativeMethods.GetSystemMetrics(NativeMethods.SM_YVIRTUALSCREEN);
        var width = NativeMethods.GetSystemMetrics(NativeMethods.SM_CXVIRTUALSCREEN);
        var height = NativeMethods.GetSystemMetrics(NativeMethods.SM_CYVIRTUALSCREEN);
        if (width <= 0 || height <= 0)
            return null;

        var screen = NativeMethods.GetDC(0);
        if (screen == 0)
            return null;
        var memory = nint.Zero;
        var bitmap = nint.Zero;
        try
        {
            memory = NativeMethods.CreateCompatibleDC(screen);
            if (memory == 0)
                return null;
            bitmap = NativeMethods.CreateCompatibleBitmap(screen, width, height);
            if (bitmap == 0)
                return null;
            var previous = NativeMethods.SelectObject(memory, bitmap);
            // CAPTUREBLT so layered windows (tooltips, some overlays) are in the picture too.
            var copied = NativeMethods.BitBlt(memory, 0, 0, width, height, screen, left, top,
                NativeMethods.SRCCOPY | NativeMethods.CAPTUREBLT);
            NativeMethods.SelectObject(memory, previous);
            if (!copied)
                return null;

            var header = new NativeMethods.BITMAPINFOHEADER
            {
                biSize = (uint)Marshal.SizeOf<NativeMethods.BITMAPINFOHEADER>(),
                biWidth = width,
                // Negative: rows top-down. A positive height would hand back the picture upside down.
                biHeight = -height,
                biPlanes = 1,
                biBitCount = 32,
                biCompression = 0, // BI_RGB
            };
            var pixels = new byte[width * height * 4];
            var read = NativeMethods.GetDIBits(memory, bitmap, 0, (uint)height, pixels, ref header, 0);
            return read == height ? new ScreenShot(pixels, width, height, left, top) : null;
        }
        finally
        {
            if (bitmap != 0)
                NativeMethods.DeleteObject(bitmap);
            if (memory != 0)
                NativeMethods.DeleteDC(memory);
            NativeMethods.ReleaseDC(0, screen);
        }
    }
}
