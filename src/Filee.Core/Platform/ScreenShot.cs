namespace Filee.Core.Platform;

/// <summary>
/// A picture taken of the screen: BGRA pixels, row by row, the top row first, with no padding between rows
/// (<see cref="Stride"/> is always <c>Width * 4</c>). <see cref="Left"/> and <see cref="Top"/> say where it was taken,
/// so a rectangle picked on it can be told back in screen coordinates.
/// </summary>
public sealed record ScreenShot(byte[] Pixels, int Width, int Height, int Left, int Top)
{
    public int Stride => Width * 4;
}
